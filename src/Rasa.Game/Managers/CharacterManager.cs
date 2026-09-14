using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Game.Client;
    using Packets.Game.Server;
    using Packets.MapChannel.Server;
    using Misc;
    using Packets.ClientMethod.Server;
    using Packets.Communicator.Client;
    using Packets.Communicator.Server;
    using Packets;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Repositories.World;
    using Structures;
    using Structures.Char;

    public class CharacterManager
    {
        private static CharacterManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly object _createLock = new();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public const ulong SelectionPodStartEntityId = 100;
        public const byte MaxSelectionPods = 16;

        public static CharacterManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new CharacterManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        public CharacterManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public void StartCharacterSelection(Client client)
        {
            if (client.State != ClientState.LoggedIn)
                return;

            client.CallMethod(SysEntity.ClientMethodId, new BeginCharacterSelectionPacket(client.AccountEntry.FamilyName, client.AccountEntry.Characters.Any(), client.AccountEntry.Id, client.AccountEntry.CanSkipBootcamp));

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var charactersBySlot = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);

            for (byte i = 1; i <= MaxSelectionPods; ++i)
            {
                CharacterEntry character = null;
                if (charactersBySlot.ContainsKey(i))
                {
                    character = charactersBySlot[i];
                }
                SendCharacterInfoProdCreate(client, i, character);
            }

            client.State = ClientState.CharacterSelection;

            // get userOptions
            var optionsList = unitOfWork.UserOptions.Get(client.AccountEntry.Id);

            foreach (var userOption in optionsList)
                client.UserOptions.Add(new UserOptions((UserOption)userOption.OptionId, userOption.Value));
            
            client.CallMethod(SysEntity.ClientMethodId, new UserOptionsPacket(client.UserOptions));
        }

        public void RequestCharacterName(Client client, int gender)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNames.GetFirstName((Gender)gender);
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedCharacterNamePacket
            {
                Name = name
            });
        }

        public void RequestFamilyName(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var name = unitOfWork.RandomNames.GetLastName();
            client.CallMethod(SysEntity.ClientMethodId, new GeneratedFamilyNamePacket
            {
                Name = name
            });
        }

        /// <summary>
        /// Clones a character into an empty pod for one of its clone credits (research/20260914-class-trainer R5.1).
        /// The clone keeps level, class, experience, Logos, completed missions and discovered waypoints and
        /// hospitals, and stands where the source stands. It starts with the starter gear, no money, Recruit skills
        /// at rank 1 and unspent attribute and skill points: since the live patch of 2008-01-29 cloning resets both.
        /// Carrying completed missions follows the dated 2009-01-25 Beginners Guide; an undated fan guide
        /// disagrees. Name, gender, height, race and appearance come from the creation screen.
        /// </summary>
        public void RequestCloneCharacterToSlot(Client client, RequestCloneCharacterToSlotPacket packet)
        {
            if (client.State != ClientState.CharacterSelection)
            {
                Logger.WriteLog(LogType.Security, $"AccountId = {client.AccountEntry.Id} tried to clone a character while in state {client.State}.");
                return;
            }

            var result = packet.CharacterName == null ? CreateCharacterResult.InvalidEncoding : packet.Validate();
            if (result == CreateCharacterResult.Success && (packet.RaceId < Race.Human || packet.RaceId > Race.Thrax))
                result = CreateCharacterResult.CharacterCreationInvalidRace;
            if (result == CreateCharacterResult.Success && packet.Gender > 1)
                result = CreateCharacterResult.InvalidEncoding;

            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            CharacterEntry clone;
            CharacterEntry source;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            lock (_createLock)
            {
                try
                {
                    using var transaction = unitOfWork.BeginTransaction();
                    var existing = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);

                    if (!existing.TryGetValue(packet.CloneSlotNum, out source))
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterToCloneFrom);
                        return;
                    }

                    if (packet.SlotNum < 1 || packet.SlotNum > MaxSelectionPods || existing.ContainsKey(packet.SlotNum))
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.CharacterSlotInUse);
                        return;
                    }

                    if (source.CloneCredits < 1)
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.NotEnoughCloneCredits);
                        return;
                    }

                    var position = new Structures.World.ContentLocationEntry
                    {
                        MapContextId = source.MapContextId, PosX = source.CoordX, PosY = source.CoordY, PosZ = source.CoordZ, Rotation = source.Rotation
                    };
                    clone = unitOfWork.Characters.Create(client.AccountEntry, packet.SlotNum, packet.CharacterName, (byte)packet.RaceId, packet.Scale, packet.Gender, position);

                    if (clone == null || !unitOfWork.CharacterAppearances.Add(clone, CreateCharacterAppearanceEntries(packet.AppearanceData)))
                        throw new InvalidOperationException("The clone could not be saved.");

                    unitOfWork.Characters.UpdateCharacterClass(clone.Id, source.Class);
                    unitOfWork.Characters.UpdateCharacterLevel(clone.Id, source.Level);
                    unitOfWork.Characters.UpdateCharacterExpirience(clone.Id, source.Experience);
                    // Played before: no first-login boot-camp choice for the clone.
                    unitOfWork.Characters.UpdateCharacterLogin(clone.Id, 0, Math.Max(1u, source.NumLogins));
                    unitOfWork.Characters.UpdateCharacterCloneCredits(source.Id, source.CloneCredits - 1);

                    var skills = SkillTraining.CreateInitialRecruitSkills();
                    unitOfWork.CharacterSkills.AddOrUpdate(skills.Values.Select(skill =>
                        new CharacterSkillsEntry(clone.Id, (uint)skill.SkillId, skill.AbilityId, skill.SkillLevel)).ToArray());

                    foreach (var logosId in unitOfWork.CharacterLogoses.GetLogos(source.Id))
                        unitOfWork.CharacterLogoses.Stage(clone.Id, logosId);

                    foreach (var teleporter in unitOfWork.CharacterTeleporters.Get(source.Id))
                        unitOfWork.CharacterTeleporters.Add(new CharacterTeleporterEntry { CharacterId = clone.Id, WaypointId = teleporter.WaypointId, WaypointType = teleporter.WaypointType });

                    var completedObjectives = unitOfWork.CharacterMissions.GetObjectives(source.Id);
                    foreach (var mission in unitOfWork.CharacterMissions.Get(client.AccountEntry.Id, source.Slot).Where(mission => mission.MissionState == (uint)MissionState.Completed))
                        unitOfWork.CharacterMissions.Add(new CharacterMissionEntry(clone.Id, mission.MissionId, mission.MissionState, mission.ChangeTime),
                            completedObjectives.Where(objective => objective.MissionId == mission.MissionId)
                                .Select(objective => new CharacterMissionObjectiveEntry(clone.Id, objective.MissionId, objective.ObjectiveId, objective.Status)));

                    GiveBasicItems(client, clone.Id, unitOfWork);

                    unitOfWork.Complete();
                    transaction.Commit();
                }
                catch (Exception exception) when (exception is Microsoft.EntityFrameworkCore.DbUpdateException ||
                    exception is InvalidOperationException || exception is KeyNotFoundException)
                {
                    Logger.WriteLog(LogType.Error, $"Clone could not be saved: {exception.Message}");
                    SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                    return;
                }
            }

            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, client.AccountEntry.FamilyName));
            client.ReloadGameAccountEntry();

            SendCharacterInfo(client, packet.SlotNum, unitOfWork.Characters.Get(clone.Id));
            SendCharacterInfo(client, packet.CloneSlotNum, unitOfWork.Characters.Get(source.Id));
        }

        public void RequestCreateCharacterInSlot(Client client, RequestCreateCharacterInSlotPacket packet)
        {
            // The selection screen is the only place the client sends this from. Nothing else here
            // is safe against a create that arrives while a character is loaded: the new row is
            // written, the account entry is reloaded under a live manifestation, and the caller
            // has no reason to be anywhere but the pod screen.
            if (client.State != ClientState.CharacterSelection)
            {
                // Not answered: the pod screen that could show a failure is not up.
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to create a character while in state {client.State}.");
                return;
            }

            var result = packet.Validate();
            if (result != CreateCharacterResult.Success)
            {
                SendCharacterCreateFailed(client, result);
                return;
            }

            // The pods are 1..MaxSelectionPods. Out-of-range and occupied slots are refused inside
            // the creation transaction below with CharacterSlotInUse; this only records the forgery.
            if (packet.SlotNum < 1 || packet.SlotNum > MaxSelectionPods)
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to create a character in slot {packet.SlotNum}.");

            uint characterId;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            // TODO to remove this lock, the family name check and update must be redesigned to be thread safe
            lock (_createLock)
            {
                try
                {
                    // Keep the new character, appearance, starter ranks, items
                    // and first account lockbox tab in one creation transaction.
                    using var transaction = unitOfWork.BeginTransaction();
                    var existing = unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id);
                    if (packet is CreateCharacterPacket &&
                        (existing.Count != 0 || !string.IsNullOrEmpty(unitOfWork.GameAccounts.Get(client.AccountEntry.Id).FamilyName)))
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterName);
                        return;
                    }
                    if (packet.SlotNum < 1 || packet.SlotNum > MaxSelectionPods || existing.ContainsKey(packet.SlotNum))
                    {
                        SendCharacterCreateFailed(client, CreateCharacterResult.CharacterSlotInUse);
                        return;
                    }
                    var createdCharacterId = InternalCreate(client, packet, unitOfWork);
                    if (createdCharacterId == null)
                        return;

                    characterId = createdCharacterId.Value;
                    var skills = SkillTraining.CreateInitialRecruitSkills();
                    unitOfWork.CharacterSkills.AddOrUpdate(skills.Values.Select(skill =>
                        new CharacterSkillsEntry(characterId, (uint)skill.SkillId, skill.AbilityId, skill.SkillLevel)).ToArray());

                    GiveBasicItems(client, characterId, unitOfWork);

                    if (unitOfWork.CharacterLockboxes.Get(client.AccountEntry.Id) == null)
                        unitOfWork.CharacterLockboxes.Add(client.AccountEntry.Id);

                    unitOfWork.Complete();
                    transaction.Commit();
                }
                catch (Exception exception) when (exception is Microsoft.EntityFrameworkCore.DbUpdateException ||
                    exception is InvalidOperationException || exception is KeyNotFoundException)
                {
                    Logger.WriteLog(LogType.Error, $"Character creation could not be saved: {exception.Message}");
                    SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                    return;
                }
            }

            client.CallMethod(SysEntity.ClientMethodId, new CharacterCreateSuccessPacket(packet.SlotNum, packet.FamilyName));

            client.ReloadGameAccountEntry();

            var character = unitOfWork.Characters.Get(characterId);
            SendCharacterInfo(client, packet.SlotNum, character);
        }

        #region Name changes

        /// <summary>
        /// Names the client will accept, from PM_NAME_TOO_SHORT, PM_NAME_TOO_LONG and
        /// PM_NAME_FORMAT_INVALID: "Your name must start with a capital letter, contain only
        /// letters, and must not contain letters repeated more than twice in a row", 3 to 20
        /// characters.
        /// </summary>
        public const int MinNameLength = 3;
        public const int MaxNameLength = 20;

        /// <summary>/changefirstname: renames the character the player is on.</summary>
        internal void ChangeFirstName(Client client, ChangeFirstNamePacket packet)
        {
            if (!IsNameChanger(client))
                return;

            Rename(client, client, packet.Name, false);
        }

        /// <summary>/changelastname: renames the account's family, so every character on it.</summary>
        internal void ChangeLastName(Client client, ChangeLastNamePacket packet)
        {
            if (!IsNameChanger(client))
                return;

            Rename(client, client, packet.Name, true);
        }

        /// <summary>
        /// Renames a character or an account family, telling the player who asked what went
        /// wrong. The target can be another player, for the GM command.
        /// </summary>
        public bool Rename(Client requester, Client target, string newName, bool familyName)
        {
            if (target?.Player == null || target.AccountEntry == null)
                return false;

            var name = newName?.Trim() ?? string.Empty;
            var oldName = familyName ? target.Player.FamilyName : target.Player.Name;

            if (string.Equals(oldName, name, StringComparison.Ordinal))
                return false;

            if (!IsValidName(name, out var formatError))
            {
                NameMessage(requester, formatError);
                return false;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (new Censor(unitOfWork.CensoredWords.GetCensoredWords()).ContainsProfanity(name))
            {
                NameMessage(requester, PlayerMessage.PmNameUnacceptable);
                return false;
            }

            if (familyName)
            {
                if (!unitOfWork.GameAccounts.CanChangeFamilyName(target.AccountEntry.Id, name))
                {
                    NameMessage(requester, PlayerMessage.PmFamilyNameReserved);
                    return false;
                }

                unitOfWork.GameAccounts.UpdateFamilyName(target.AccountEntry.Id, name);
                target.Player.FamilyName = name;
            }
            else
            {
                // Character creation never checked this, so duplicates can exist already; a
                // rename at least does not add more.
                if (unitOfWork.Characters.IsCharacterNameTaken(name, target.Player.Id))
                {
                    NameMessage(requester, PlayerMessage.PmNameInUse);
                    return false;
                }

                unitOfWork.Characters.UpdateCharacterName(target.Player.Id, name);
                target.Player.Name = name;
            }

            // UpdateCharacterName saves as it goes; UpdateFamilyName only changes the tracked
            // row, and the unit of work discards that on dispose unless it is completed. The
            // family name change was lost here, and ReloadGameAccountEntry then read the old
            // name straight back.
            unitOfWork.Complete();

            target.ReloadGameAccountEntry();

            // CharacterName and ActorName are part of the entity data every client gets when it
            // first sees the player (CreatePlayerEntityData); resending them updates the name on
            // screen for everyone nearby without a relog. Characters of this account that are not
            // in the world pick the family name up the next time they log in.
            var mapChannel = target.Player.MapChannel;

            if (mapChannel != null)
                CellManager.Instance.CellCallMethod(mapChannel, target.Player,
                    familyName ? new ActorNamePacket(target.Player.FamilyName) : (PythonPacket)new CharacterNamePacket(target.Player.Name));

            var args = new Dictionary<string, string> { ["oldname"] = oldName ?? string.Empty, ["newname"] = name };
            var changed = familyName ? PlayerMessage.PmLastNameChanged : PlayerMessage.PmFirstNameChanged;

            target.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(changed, args, MsgFilterId.GeneralSystemMessages));

            if (requester != target)
                requester.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(changed, args, MsgFilterId.GeneralSystemMessages));

            Logger.WriteLog(LogType.Command, $"{requester.AccountEntry.FamilyName} changed {(familyName ? "the family name" : "the character name")} of account {target.AccountEntry.Id} from {oldName} to {name}");

            return true;
        }

        public static bool IsValidName(string name, out PlayerMessage error)
        {
            error = PlayerMessage.PmNameFormatInvalid;

            if (string.IsNullOrEmpty(name) || name.Length < MinNameLength)
            {
                error = PlayerMessage.PmNameTooShort;
                return false;
            }

            if (name.Length > MaxNameLength)
            {
                error = PlayerMessage.PmNameTooLong;
                return false;
            }

            if (!char.IsUpper(name[0]))
                return false;

            for (var i = 0; i < name.Length; i++)
            {
                if (!char.IsLetter(name[i]))
                    return false;

                // No letter three times in a row.
                if (i >= 2 && char.ToLowerInvariant(name[i]) == char.ToLowerInvariant(name[i - 1])
                           && char.ToLowerInvariant(name[i]) == char.ToLowerInvariant(name[i - 2]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Name changes are a GM tool here: the slash commands are open to every player, and a
        /// free rename at any moment is a way to be mistaken for someone else.
        /// </summary>
        /// <summary>
        /// Who may use /changefirstname and /changelastname.
        ///
        /// GameMaster, matching the .rename command. It was "any GM level at all", which let an
        /// Observer - the level that exists to read the world without changing it, and the level
        /// every pre-existing account was left on - rename itself and its whole account family.
        /// </summary>
        private static bool IsNameChanger(Client client)
        {
            if (client?.AccountEntry == null || client.Player == null)
                return false;

            if (client.AccountEntry.Level >= (byte)GmLevel.GameMaster)
                return true;

            Logger.WriteLog(LogType.Security, $"AccountId = {client.AccountEntry.Id} tried to change a name without being a GM");
            CommunicatorManager.Instance.SystemMessage(client, "Name changes are done by a GM.");

            return false;
        }

        private static void NameMessage(Client client, PlayerMessage message)
        {
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        #endregion

        private uint? InternalCreate(Client client, RequestCreateCharacterInSlotPacket packet, ICharUnitOfWork unitOfWork)
        {
            var changeFamilyName = false;
            if (!string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) && packet.FamilyName != client.AccountEntry.FamilyName)
            {
                if (!client.AccountEntry.Characters.Any())
                {
                    changeFamilyName = true;
                }
                else
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.InvalidCharacterName);
                    return null;
                }
            }

            if ((string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || packet.FamilyName != client.AccountEntry.FamilyName))
            {
                if (!unitOfWork.GameAccounts.CanChangeFamilyName(client.AccountEntry.Id, packet.FamilyName))
                {
                    SendCharacterCreateFailed(client, CreateCharacterResult.FamilyNameReserved);
                    return null;
                }
            }

            var characterEntry = unitOfWork.Characters.Create(client.AccountEntry, packet.SlotNum,
                packet.CharacterName,
                (byte)packet.RaceId,
                packet.Scale,
                packet.Gender,
                MissionContentManager.Instance.NewCharacterStart(client.AccountEntry.Id));
            if (characterEntry == null)
            {
                SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                return null;
            }

            var appearances = CreateCharacterAppearanceEntries(packet);
            if (!unitOfWork.CharacterAppearances.Add(characterEntry, appearances))
            {
                SendCharacterCreateFailed(client, CreateCharacterResult.TechnicalDifficulty);
                return null;
            }

            if (string.IsNullOrWhiteSpace(client.AccountEntry.FamilyName) || changeFamilyName)
            {
                unitOfWork.GameAccounts.UpdateFamilyName(client.AccountEntry.Id, packet.FamilyName);
            }

            return characterEntry.Id;
        }

        private IEnumerable<CharacterAppearanceEntry> CreateCharacterAppearanceEntries(RequestCreateCharacterInSlotPacket packet)
            => CreateCharacterAppearanceEntries(packet.AppearanceData);

        private IEnumerable<CharacterAppearanceEntry> CreateCharacterAppearanceEntries(IReadOnlyDictionary<EquipmentData, AppearanceData> appearanceData)
        {
            // Original charactercreationwindow uses (255, 255, 255, 255)
            // for these fixed outfit pieces. See new-character-client-evidence.md.
            const uint recruitOutfitColor = 0xffffffff;
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Shoes, (uint)EntityClasses.ArmorRecruitV01CMNBoots, recruitOutfitColor);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Torso, (uint)EntityClasses.ArmorRecruitV01CMNVest, recruitOutfitColor);
            yield return new CharacterAppearanceEntry((uint)EquipmentData.Legs, (uint)EntityClasses.ArmorRecruitV01CMNLegs, recruitOutfitColor);

            using var worldUnitOfWork = _gameUnitOfWorkFactory.CreateWorld();
            var appearancesFromPacket = appearanceData
                .Select(appearanceData => CreateCharacterAppearanceEntry(appearanceData.Value, worldUnitOfWork))
                .ToList();

            foreach (var characterAppearanceEntry in appearancesFromPacket)
            {
                yield return characterAppearanceEntry;
            }
        }

        private static CharacterAppearanceEntry CreateCharacterAppearanceEntry(AppearanceData appearanceData, IWorldUnitOfWork unitOfWork)
        {
            var databaseEntry = appearanceData.GetDatabaseEntry();
            databaseEntry.Class = unitOfWork.Equipment.GetItemClass(appearanceData.Class);
            return databaseEntry;
        }

        private void GiveBasicItems(Client client, uint characterId, ICharUnitOfWork unitOfWork)
        {
            // Preserve the existing item selection pending the original complete
            // starter-loadout audit. Durability must come from the granted item.
            var items = new (uint Template, uint Slot, uint Quantity)[]
            {
                (145, 0, 1), (28, 50, 100), (13126, 1, 1), (13186, 2, 1), (13156, 3, 1)
            };
            foreach (var entry in items)
            {
                var itemClass = ItemManager.Instance.ItemTemplateItemClass[entry.Template];
                var maximumHitPoints = EntityClassManager.Instance.LoadedEntityClasses[itemClass].ItemClassInfo.MaxHitPoints;
                var itemId = unitOfWork.Items.CreateItem(new Item(entry.Template, entry.Quantity, maximumHitPoints, 2139062144));
                if (itemId == 0)
                    throw new InvalidOperationException("A starter item could not be saved.");
                unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, characterId,
                    (uint)InventoryType.Personal, entry.Slot, itemId);
            }
        }

        /// <summary>
        /// Deleting is something the character selection screen asks for, and the shipped client
        /// only offers it there. Nothing refused the packet from a client that was in the world,
        /// though, so a modified one could delete the character its own player was standing in -
        /// leaving the session running against a row that no longer exists.
        ///
        /// The test is the connection's state rather than Player.MapChannel: RemovePlayer takes
        /// the client out of the map's client list but leaves the channel reference on the
        /// Manifestation, so a player who reached selection through /logout still has one, and
        /// gating on it would refuse a delete that is perfectly legitimate.
        ///
        /// Any delete from in the world is refused, not just of the character being played. A
        /// real client cannot ask for either, and deleting one of your other characters
        /// mid-session is no more a thing the selection screen can do.
        /// </summary>
        public void RequestDeleteCharacterInSlot(Client client, RequestDeleteCharacterInSlotPacket packet)
        {
            if (client.State == ClientState.Ingame
                || client.State == ClientState.Loading
                || client.State == ClientState.Teleporting)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to delete the character in slot {packet.Slot} "
                    + $"while in the world (state {client.State}).");

                client.CallMethod(SysEntity.ClientMethodId, new DeleteCharacterFailedPacket());
                return;
            }

            try
            {
                var charactersBySlot = client.AccountEntry.GetCharacterBySlot(packet.Slot);
                if (charactersBySlot == null)
                {
                    return;
                }

                using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                {
                    unitOfWork.CharacterAppearances.DeleteForChar(charactersBySlot.Id);
                    // TODO delete ClanMember entry
                    unitOfWork.Characters.Delete(charactersBySlot.Id);
                    unitOfWork.Complete();
                }

                // Client.Player still points at the character that was just deleted - it is left
                // loaded when the player returns to character selection. Client.SaveCharacter
                // skips a player whose Id is 0 and otherwise looks the row up with
                // GetWritableEnsuring, so dropping the connection from here (Alt+F4 at the
                // selection screen) would go looking for a row that no longer exists and throw.
                // Close() catches that, so it only ever cost a misleading "Failed to save
                // character on disconnect" in the log - but there is genuinely nothing left to
                // save, and the log should not say otherwise.
                if (client.Player != null && client.Player.Id == charactersBySlot.Id)
                    client.Player.Id = 0;

                client.ReloadGameAccountEntry();

                client.CallMethod(SysEntity.ClientMethodId, new CharacterDeleteSuccessPacket(client.AccountEntry.Characters.Any()));

                SendCharacterInfo(client, packet.Slot, null);
            }
            catch
            {
                client.CallMethod(SysEntity.ClientMethodId, new DeleteCharacterFailedPacket());
            }
        }

        public void RequestSwitchToCharacterInSlot(Client client, RequestSwitchToCharacterInSlotPacket packet)
        {
            // Only from the pod screen. From the world this replaced the manifestation while
            // the old one was still in its map's cells and every manager's tables - never
            // removed, a frozen copy for everyone else, and the client in two maps at once.
            if (client.State != ClientState.CharacterSelection)
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to switch to the character in slot {packet.SlotNum} while in state {client.State}.");
                return;
            }

            if (packet.SlotNum < 1 || packet.SlotNum > MaxSelectionPods)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // Resolve persisted ownership before changing either saved or session state.
            if (!unitOfWork.Characters.GetByAccountId(client.AccountEntry.Id).TryGetValue(packet.SlotNum, out var character))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} tried to switch to slot {packet.SlotNum}, which is empty.");
                return;
            }
            // S7: "Would you like to skip Bootcamp and go straight to Wilderness?" answered Yes. The
            // client asks only for an unplayed character of an account that may skip; the server
            // checks the same and moves the character to the boot camp's exit destination in the
            // same commit as the login. No grants (GAP-SKIP-GRANTS).
            if (packet.SkipBootcamp && SkipBootcampDestination(client, character) is { } destination)
            {
                unitOfWork.Characters.StagePosition(character.Id, destination.PosX, destination.PosY, destination.PosZ, destination.Rotation, destination.MapContextId);
                character.MapContextId = destination.MapContextId;
                character.CoordX = destination.PosX;
                character.CoordY = destination.PosY;
                character.CoordZ = destination.PosZ;
                character.Rotation = destination.Rotation;
            }

            unitOfWork.GameAccounts.UpdateSelectedSlot(client.AccountEntry.Id, packet.SlotNum);
            unitOfWork.Characters.UpdateLoginData(character.Id);
            unitOfWork.Complete();
            client.AccountEntry.SelectedSlot = packet.SlotNum;

            client.Player = CreateCharacterManifestation(client, character);
            // The shared channel, or this character's own instance of a per-character context.
            client.Player.MapChannel = MapChannelManager.Instance.ChannelForEntry(character.Id, client.Player.MapContextId)
                ?? MapChannelManager.Instance.FindByContextId(client.Player.MapContextId);
            client.LoadingMap = client.Player.MapContextId;
            MapChannelManager.Instance.PassClientToMapInstance(client);
        }

        /// <summary>
        /// Where an unplayed character in a per-character boot camp goes when its account may skip:
        /// the destination the boot camp's exit transfers to. Null (enter the boot camp) otherwise.
        /// </summary>
        public Func<Client, CharacterEntry, Structures.World.ContentLocationEntry> SkipBootcampDestination { get; set; } = (client, character) =>
        {
            if (client.AccountEntry?.CanSkipBootcamp != true || character.NumLogins != 0 ||
                !MapChannelManager.Instance.IsPerCharacterContext(character.MapContextId))
                return null;

            var content = MissionContentManager.Instance.Content;
            return content.LiveRules
                .Where(rule => rule.MapContextId == character.MapContextId)
                .SelectMany(rule => content.Catalog.RuleActions.TryGetValue(rule.Id, out var actions) ? actions : Enumerable.Empty<Structures.World.ContentRuleActionEntry>())
                .Where(action => (ContentRuleAction)action.Action == ContentRuleAction.TransferToLocation)
                .Select(action => content.Catalog.Locations.TryGetValue(action.LocationId, out var location) ? location : null)
                .FirstOrDefault(location => location != null);
        };

        private void SendCharacterCreateFailed(Client client, CreateCharacterResult result)
        {
            client.CallMethod(SysEntity.ClientMethodId, new UserCreationFailedPacket(result));
        }

        private void SendCharacterInfoProdCreate(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var newEntityPacket = new CreatePhysicalEntityPacket(SelectionPodStartEntityId + slot, EntityClasses.CharacterSelectionPod);

            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            newEntityPacket.EntityData.Add(characterInfo);

            client.CallMethod(SysEntity.ClientMethodId, newEntityPacket);
        }

        private void SendCharacterInfo(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = CreateCharacterInfoPacket(client, slot, data);

            client.CallMethod(SelectionPodStartEntityId + slot, characterInfo);
        }

        private CharacterInfoPacket CreateCharacterInfoPacket(Client client, byte slot, [CanBeNull] CharacterEntry data)
        {
            var characterInfo = data == null
                ? new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName)
                : new CharacterInfoPacket(slot, slot == client.AccountEntry.SelectedSlot, client.AccountEntry.FamilyName, data);
            return characterInfo;
        }

        private Manifestation CreateCharacterManifestation(Client client, CharacterEntry character)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var characterAppearances = unitOfWork.CharacterAppearances.GetByCharacterId(character.Id);
            var appearanceData = new Dictionary<EquipmentData, AppearanceData>();
            var lockboxInfo = unitOfWork.CharacterLockboxes.Get(client.AccountEntry.Id);
            var missionData = MissionManager.Instance.LoadPlayerMissions(unitOfWork.CharacterMissions, client.AccountEntry.Id, client.AccountEntry.SelectedSlot, character.Id);
            var clan = unitOfWork.Clans.GetClanByCharacterId(character.Id);
            var logos = unitOfWork.CharacterLogoses.GetLogos(character.Id);
            var facts = unitOfWork.CharacterContentFacts.Get(character.Id);

            foreach (var appearance in characterAppearances)
                appearanceData.Add((EquipmentData)appearance.Slot, new AppearanceData(appearance));

            var newCharacter = new Manifestation(character, appearanceData)
            {
                ClanId = clan?.Id ?? 0,
                ClanName = clan?.Name,
                GainedWaypoints = unitOfWork.CharacterTeleporters.Get(character.Id),
                LockboxCredits = lockboxInfo?.Credits ?? 0,
                LockboxTabs = lockboxInfo?.PurashedTabs ?? 0,
                Skills = MapChannelManager.Instance.GetPlayerSkills(character.Id),
                Titles = unitOfWork.CharacterTitles.Get(character.Id),
                Abilities = MapChannelManager.Instance.GetPlayerAbilities(character.Id),
                Missions = missionData,
                LoginTime = DateTime.Now,
                Logos = logos
            };

            foreach (var fact in facts)
                newCharacter.ContentFacts[(fact.MapContextId, fact.FactKey)] = fact.Value;

            return newCharacter;
        }

        public void UpdateCharacter(Client client, CharacterUpdate job, object value = null)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            switch (job)
            {
                case CharacterUpdate.Attributes:
                    unitOfWork.Characters.UpdateCharacterAttributes(client.Player.Id, client.Player.SpentBody, client.Player.SpentMind, client.Player.SpentSpirit);
                    break;

                case CharacterUpdate.Class:
                    unitOfWork.Characters.UpdateCharacterClass(client.Player.Id, client.Player.Class);
                    break;

                case CharacterUpdate.CloneCredits:
                    unitOfWork.Characters.UpdateCharacterCloneCredits(client.Player.Id, client.Player.CloneCredits);
                    break;

                case CharacterUpdate.Credits:
                    var ammount = (int)value;

                    if (ammount < 0)
                        client.Player.Credits[CurencyType.Credits] -= Math.Abs(ammount);
                    else
                        client.Player.Credits[CurencyType.Credits] += ammount;

                    // inform owner
                    client.CallMethod(client.Player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, client.Player.Credits[CurencyType.Credits], 0));
                    // update db
                    unitOfWork.Characters.UpdateCharacterCredits(client.Player.Id, client.Player.Credits[CurencyType.Credits]);
                    break;

                case CharacterUpdate.Expirience:
                    unitOfWork.Characters.UpdateCharacterExpirience(client.Player.Id, client.Player.Experience);
                    break;

                case CharacterUpdate.Level:
                    unitOfWork.Characters.UpdateCharacterLevel(client.Player.Id, client.Player.Level);
                    break;

                case CharacterUpdate.Login:
                    // TotalMinutes, not Minutes: Minutes is the minute hand (0..59), so a
                    // session of an hour and ten minutes used to count as ten. TotalTimePlayed
                    // on the manifestation is the value loaded at login and LoginTime is set
                    // once, so the sum is right however many times this runs in one session.
                    var sessionMinutes = (long)(DateTime.Now - client.Player.LoginTime).TotalMinutes;
                    var totalTimePlayed = (uint)Math.Max(0, sessionMinutes) + client.Player.TotalTimePlayed;

                    unitOfWork.Characters.UpdateCharacterLogin(client.Player.Id, totalTimePlayed, client.Player.NumLogins);
                    break;

                case CharacterUpdate.Logos:
                    client.Player.Logos.Add((uint)value);
                    unitOfWork.CharacterLogoses.SetLogos(client.Player.Id, (uint)value);
                    client.CallMethod(client.Player.EntityId, new LogosStoneAddedPacket((uint)value));
                    break;

                case CharacterUpdate.Position:
                    var data = value as WonkavatePacket;

                    if (data != null)
                    {
                        // The character being moved is the one in the world; no need to go by
                        // the selected slot, which can name an empty pod.
                        unitOfWork.Characters.UpdateCharacterPosition(client.Player.Id, data.Position.X, data.Position.Y, data.Position.Z, data.Orientation, data.MapContextId);
                    }
                    else
                        unitOfWork.Characters.UpdateCharacterPosition(
                            client.Player.Id,
                            client.Player.Position.X,
                            client.Player.Position.Y,
                            client.Player.Position.Z,
                            client.Player.Rotation,
                            client.Player.MapContextId
                            );

                    break;

                case CharacterUpdate.Prestige:
                    // Same shape as Credits: value is the signed change. Prestige was loaded
                    // into Player.Credits at login but never written back.
                    var prestigeChange = (int)value;

                    client.Player.Credits[CurencyType.Prestige] += prestigeChange;

                    client.CallMethod(client.Player.EntityId, new UpdateCreditsPacket(CurencyType.Prestige, client.Player.Credits[CurencyType.Prestige], 0));
                    unitOfWork.Characters.UpdateCharacterPrestige(client.Player.Id, client.Player.Credits[CurencyType.Prestige]);
                    break;

                case CharacterUpdate.Stats:
                    break;

                case CharacterUpdate.ActiveWeapon:
                    client.Player.ActiveWeapon = (byte)value;
                    unitOfWork.Characters.UpdateCharacterActiveWeapon(client.Player.Id, client.Player.ActiveWeapon);
                    break;
                case CharacterUpdate.Teleporter:
                    var teleporter = (CharacterTeleporterEntry)value;

                    unitOfWork.CharacterTeleporters.Add(teleporter);
                    break;
                default:
                    break;
            }
        }
    }
}
