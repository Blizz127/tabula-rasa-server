using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets;
    using Packets.Communicator.Server;
    using Packets.Game.Server;
    using Packets.Manifestation.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Content;
    using Structures.Char;
    public class ManifestationManager
    {
        /* Actor: Player "bodies" (ManifestationClass)
         * 
         *    Manifestation Packets:
         *  - CurrentCharacterId                => implemented
         *  - AllCredits                        => implemented
         *  - UpdateCredits                     => implemented
         *  - LockboxFunds                      => implemented
         *  - WonBattleground                   => ToDo
         *  - LostBattleground                  => ToDo
         *  - WeaponDrawerSlot                  => implemented
         *  - AbilityDrawerSlot                 => implemented
         *  - AbilityDrawer                     => implemented
         *  - ArmWeaponFailed                   => ToDo
         *  - ArmAbilityFailed                  => ToDo
         *  - AdvancementStats                  => implemented
         *  - ExperienceChanged                 => ToDo
         *  - LevelChanged                      => ToDo
         *  - CharacterClass                    => implemented
         *  - AvailableAllocationPoints
         *  - AvailableCharacterClasses
         *  - TierAdvancementInfo
         *  - InvitedToJoinFriend
         *  - InvitationDeclined
         *  - InvitationCancelled
         *  - CannotInvite
         *  - InvitedToAddAndJoinFriend
         *  - RequestToJoin
         *  - JoinFriendDeclined
         *  - JoinFriendCancelled
         *  - CannotJoin
         *  - ForceConverse
         *  - LogosStoneTabula
         *  - LogosStoneAdded
         *  - LogosStoneRemoved
         *  - ShowHelmetChanged
         *  - Titles
         *  - TitleChanged
         *  - TitleAdded
         *  - TitleRemoved
         *  - PlayerFlags
         *  - CloneCredits
         *  - WaypointGained
         *  - GraveyardGained
         *  - CharacterName
         *  - RaceId
         *  - PlayerAfk                        => implemented
         *  - PlayerInactiveWarning            => implemented
         *  - ClanId
         *  - IsTrialAccount                   => implemented (always false)
         *  - PlayerEnteredCombat
         *  - PlayerExitedCombat
         *  - MinionAdded
         *  - MinionStayAck
         *  - MinionGoAck
         *  - MinionFollowMeAck
         *  - MinionFollowTargetAck
         *  - MinionTargetMeAck
         *  - MinionTargetAck
         *  - MinionAssistMeAck
         *  - MinionAssistTargetAck
         *  - MinionTemperamentAck
         *  - MinionCommandAck
         *  
         *  Manifestation Handlrs:
         *  - AutoFireKeepAlive         => ToDo
         *  - ChangeShowHelmet          => ToDo
         *  - ChangeTitle               => implemented, but need more work on it
         *  - RespondToAddAndJoinFriend => ToDo
         *  - RespondToJoinFriend       => ToDo
         *  - RespondToRequestToJoin    => ToDo
         *  - RequestArmAbility         => implemented
         *  - RequestArmWeapon          => implemented
         *  - RequestSetAbilitySlot     => implemented
         *  - RequestSwapAbilitySlots   => implemented
         *  - StartAutoFire             => implemented, but need more work on it
         *  - StopAutoFire              => implemented, but need more work on it
         */
        private static ManifestationManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly WeaponAttackManager _weaponAttacks;

        private static List<AutoFireTimer> AutoFire = new List<AutoFireTimer>();
        public static byte MaxPlayerLevel = 50;
        // All eight original signature costs are 1000 CHI and described as 100%.
        // See docs/sprint-client-evidence.md for this inferred normal capacity.
        public const int NormalAdrenalineMaximum = 1000;
        public static ManifestationManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ManifestationManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private ManifestationManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
            : this(gameUnitOfWorkFactory, WeaponAttackManager.Instance)
        {
        }

        public ManifestationManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, WeaponAttackManager weaponAttacks)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _weaponAttacks = weaponAttacks ?? throw new ArgumentNullException(nameof(weaponAttacks));
        }

        #region Handlers
        public void AutoFireKeepAlive(Client client, int keepAliveDelay)
        {
            if (keepAliveDelay <= 0 || !WeaponActionManager.CanAct(client))
                return;
            foreach (var timer in AutoFire)
                if (timer.Client == client)
                    timer.MaxAliveTime = (long)keepAliveDelay * 4; // Existing server grace policy remains unverified.
        }

        public void ChangeShowHelmet(Client client, ChangeShowHelmetPacket packet)
        {
            Logger.WriteLog(LogType.Debug, "ToDo ChangeShowHelmet");
        }

        public void ChangeTitle(Client client, uint titleId)
        {
            //if (titleId != 0)
            //{
            client.Player.CurrentTitle = titleId;
            client.CallMethod(client.Player.EntityId, new TitleChangedPacket(titleId));
            /*}
            else
            {
                client.SendPacket(client.MapClient.Player.Actor.EntityId, new TitleRemovedPacket(client.MapClient.Player.CurrentTitle));
                client.MapClient.Player.CurrentTitle = titleId;
            }

            client.MapClient.Player.CurentTitle = titleId;*/
        }

        public bool PlayerTryFireWeapon(Client client)
            => _weaponAttacks.TryAutoFire(client);

        public void RequestArmAbility(Client client, int abilityDrawerSlot)
        {
            client.Player.CurrentAbilityDrawer = abilityDrawerSlot;
            // ToDo do we need upate Database???
            client.CallMethod(client.Player.EntityId, new AbilityDrawerSlotPacket(abilityDrawerSlot));
        }

        public void RequestArmWeapon(Client client, uint requestedWeaponDrawerSlot)
        {
            if (!WeaponActionManager.CanAct(client) || requestedWeaponDrawerSlot >= client.Player.Inventory.WeaponDrawer.Count)
                return;
            var previousWeaponEntityId = client.Player.Inventory.EquippedInventory[13];
            client.Player.ActiveWeapon = (byte)requestedWeaponDrawerSlot;

            client.CallMethod(client.Player.EntityId, new WeaponDrawerSlotPacket(requestedWeaponDrawerSlot, true));

            RefreshArmedWeapon(client, previousWeaponEntityId);
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.Characters.UpdateCharacterActiveWeapon(client.Player.Id, client.Player.ActiveWeapon);
        }

        public void RefreshArmedWeapon(Client client, ulong previousWeaponEntityId)
        {
            var drawer = client.Player.Inventory.WeaponDrawer;
            var weaponEntityId = client.Player.ActiveWeapon < drawer.Count ? drawer[client.Player.ActiveWeapon] : 0;
            var weapon = EntityManager.Instance.GetItem(weaponEntityId);
            client.Player.Inventory.EquippedInventory[13] = weapon?.EntityId ?? 0;
            if (previousWeaponEntityId != client.Player.Inventory.EquippedInventory[13])
            {
                WeaponActionManager.Instance.Cancel(client, true);
                _weaponAttacks?.Cancel(client, true);
            }

            NotifyEquipmentUpdate(client);
            if (weapon == null)
            {
                if (client.Player.AppearanceData.ContainsKey(EquipmentData.Weapon))
                    RemoveAppearanceItem(client, EquipmentData.Weapon);
                if (client.Player.WeaponReady)
                    WeaponReady(client, false);
            }
            else
            {
                SetAppearanceItem(client, weapon);
                client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));
            }
            UpdateAppearance(client);
        }

        public void RequestSetAbilitySlot(Client client, RequestSetAbilitySlotPacket packet)
        {
            // todo: do we need to check if ability is available ??
            if (packet.AbilityId == 0)
            {
                // remove ability is used
                client.Player.Abilities.Remove(packet.SlotId);
            }
            else
            {
                // added new ability
                client.Player.Abilities.TryGetValue(packet.SlotId, out AbilityDrawerData ability);
                if (ability == null)
                {
                    client.Player.Abilities.Add(packet.SlotId, new AbilityDrawerData(packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel));
                }
                else
                {
                    client.Player.Abilities[packet.SlotId].AbilityId = (int)packet.AbilityId;
                    client.Player.Abilities[packet.SlotId].AbilityLevel = (uint)packet.AbilityLevel;
                    client.Player.Abilities[packet.SlotId].AbilitySlotId = packet.SlotId;
                }
            }
            // update database with new drawer slot ability
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.SlotId, (int)packet.AbilityId, (uint)packet.AbilityLevel);
            // send packet
            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(client.Player.Abilities));
        }

        public void RequestSwapAbilitySlots(Client client, RequestSwapAbilitySlotsPacket packet)
        {
            AbilityDrawerData toSlot;
            var abilities = client.Player.Abilities;
            var fromSlot = abilities[packet.FromSlot];
            abilities.TryGetValue(packet.ToSlot, out toSlot);
            if (toSlot == null)
            {
                abilities.Add(packet.ToSlot, new AbilityDrawerData(packet.ToSlot, fromSlot.AbilityId, fromSlot.AbilityLevel));
                abilities.Remove(packet.FromSlot);
            }
            else
            {
                abilities[packet.ToSlot] = abilities[packet.FromSlot];
                abilities[packet.ToSlot].AbilitySlotId = packet.ToSlot;
                abilities[packet.FromSlot] = toSlot;
                abilities[packet.FromSlot].AbilitySlotId = packet.FromSlot;
            }
            // Do we need to update database here ???
            // update database with new drawer slot ability
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                client.Player.Id,
                abilities[packet.ToSlot].AbilitySlotId,
                abilities[packet.ToSlot].AbilityId,
                abilities[packet.ToSlot].AbilityLevel);
            // check if fromSlot isn't empty now
            abilities.TryGetValue(packet.FromSlot, out AbilityDrawerData tempSlot);
            if (tempSlot != null)
                unitOfWork.CharacterAbilityDrawers.AddOrUpdate(
                    client.Player.Id,
                    abilities[packet.FromSlot].AbilitySlotId,
                    abilities[packet.FromSlot].AbilityId,
                    abilities[packet.FromSlot].AbilityLevel);
            else
                unitOfWork.CharacterAbilityDrawers.AddOrUpdate(client.Player.Id, packet.FromSlot, 0, 0);
            // send packet
            client.CallMethod(client.Player.EntityId, new AbilityDrawerPacket(abilities));
        }

        public void StartAutoFire(Client client, double yaw)
        {
            // ToDo:
            // yaw is probobly used to mach player and target orientation,
            // some creatures recive more damage from back then from front

            if (!WeaponActionManager.CanAct(client) || WeaponActionManager.CurrentWeapon(client) == null ||
                AutoFire.Exists(timer => timer.Client == client))
                return;
            RegisterAutoFire(client);
            ActorManager.Instance.RequestVisualCombatMode(client, true);
            if (PlayerTryFireWeapon(client))
            {
                var timer = AutoFire.Find(entry => entry.Client == client);
                timer.RefireTime = _weaponAttacks.GetNextAttemptDelay(client);
                timer.Delay = timer.RefireTime;
            }
        }

        public void StopAutoFire(Client client)
        {
            ActorManager.Instance.RequestVisualCombatMode(client, false);

            RemoveAutoFire(client);
        }

        #endregion

        #region Helper Functions

        public void AllocateAttributePoints(Client client, AllocateAttributePointsPacket packet)
        {
            if (!AttributePointAllocation.TryAllocate(client.Player, packet.Body, packet.Mind, packet.Spirit))
            {
                SendAvailableAllocationPoints(client);
                return;
            }

            UpdateStatsValues(client, false);

            // update DB
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Attributes, null);

            // Send Data to client
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
            SendAvailableAllocationPoints(client);
        }

        public void AssignPlayer(Client client)
        {
            var player = client.Player;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // get charaterOptions. Cleared first: this runs again on every map change, and the
            // list used to gain another copy of every option each time.
            player.CharacterOptions.Clear();

            var optionsList = unitOfWork.CharacterOptions.Get(player.Id);

            foreach (var characterOption in optionsList)
                player.CharacterOptions.Add(new CharacterOptions((CharacterOption)characterOption.OptionId, characterOption.Value));

            client.CallMethod(SysEntity.ClientMethodId, new CharacterOptionsPacket(player.CharacterOptions));

            client.CallMethod(SysEntity.ClientMethodId, new SetControlledActorIdPacket(player.EntityId));

            client.CallMethod(player.EntityId, new WeaponDrawerSlotPacket(player.ActiveWeapon, false));

            client.CallMethod(SysEntity.ClientGameMapId, new SetSkyTimePacket { RunningTime = 6666666 });   // ToDo add actual time how long map is running

            client.CallMethod(SysEntity.ClientMethodId, new SetCurrentContextIdPacket(client.Player.MapChannel.MapInfo.MapContextId));

            SocialManager.Instance.SetSocialContactList(client);

            client.CallMethod(player.EntityId, new ActorInfoPacket(player));

            // The regions the player is standing in; re-sent by RegionManager.Worker as they move.
            RegionManager.Instance.PlayerEnteredMap(client);

            client.CallMethod(player.EntityId, new AdvancementStatsPacket(
                player.Level,
                player.Experience,
                GetAvailableAttributePoints(player),
                0,       // trainPoints (are not used by the client??)
                GetSkillPointsAvailable(player)
            ));

            SendAdvancementState(client);

            client.CallMethod(player.EntityId, new SkillsPacket(player.Skills));

            client.CallMethod(player.EntityId, new AbilitiesPacket(player.Skills));

            // don't send this packet if abilityDrawer is empty
            if (player.Abilities.Count > 0)
                client.CallMethod(player.EntityId, new AbilityDrawerPacket(player.Abilities));

            client.CallMethod(player.EntityId, new TitlesPacket(player.Titles));

            client.CallMethod(player.EntityId, new UpdateAttributesPacket(player.Attributes, 0));

            client.CallMethod(player.EntityId, new UpdateHealthPacket(player.Attributes[Attributes.Health], 0));

            client.CallMethod(player.EntityId, new LogosStoneTabulaPacket(player.Logos));

            client.CallMethod(player.EntityId, new AllCreditsPacket(player.Credits));

            client.CallMethod(player.EntityId, new LockboxFundsPacket(player.LockboxCredits));
        }

        public void AutoFireTimerDoWork(long delta)
        {
            // go backwards through list
            for (var i = AutoFire.Count - 1; i >= 0; i--)
            {
                var timer = AutoFire[i];
                if (!WeaponActionManager.CanAct(timer.Client))
                {
                    AutoFire.RemoveAt(i);
                    continue;
                }
                // we dont want to server keep fireing if client crash 
                timer.MaxAliveTime -= delta;

                if (timer.MaxAliveTime <= 0)
                {
                    AutoFire.RemoveAt(i);
                    continue;
                }

                timer.Delay -= delta;

                if (timer.Delay <= 0)
                {
                    if (PlayerTryFireWeapon(timer.Client))
                    {
                        timer.RefireTime = _weaponAttacks.GetNextAttemptDelay(timer.Client);
                        timer.Delay = timer.RefireTime;
                    }
                    else
                        timer.Delay = 100; // Original client's retry while drawing/reloading/busy.
                }
            }
        }

        public void CellDiscardClientToPlayers(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(client.Player.EntityId));
            }
        }

        public void CellDiscardPlayersToClient(Client client, List<Client> notifyClients)
        {
            foreach (var tempClient in notifyClients)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(tempClient.Player.EntityId));
            }

        }

        public void CellIntroduceClientToPlayers(Client client, List<Client> clientList)
        {
            var player = client.Player;

            foreach (var tempClient in clientList)
            {
                // don't send data about yourself
                if (tempClient == client)
                    continue;

                tempClient.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(player.EntityId, player.EntityClass, CreatePlayerEntityData(client)));

            }
        }

        public void CellIntroduceClientToSefl(Client client)
        {
            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(client.Player.EntityId, client.Player.EntityClass, CreatePlayerEntityData(client)));
        }

        public void CellIntroducePlayersToClient(Client client, List<Client> clientList)
        {
            foreach (var tempClient in clientList)
            {
                if (tempClient == null)
                    continue;

                if (tempClient == client)
                    continue;

                client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(tempClient.Player.EntityId, tempClient.Player.EntityClass, CreatePlayerEntityData(tempClient)));
            }
        }
		
		public List<PythonPacket> CreatePlayerEntityData(Client client)
        {
            var player = client.Player;

            var entityData = new List<PythonPacket>
            {
                // PhysicalEntity
                new IsTargetablePacket(EntityClassManager.Instance.GetClassInfo(player.EntityClass).TargetFlag),
                new WorldLocationDescriptorPacket(player.Position, player.Rotation),
                // Manifestation
                new CurrentCharacterIdPacket(player.EntityId),
                new CharacterClassPacket(player.Class),
                new RaceIdPacket(player.Race),
                new AttributeInfoPacket(player.Attributes),
                new PreloadDataPacket(client.Player.Inventory.EquippedInventory[13], player.Abilities),
                new AppearanceDataPacket(player.AppearanceData),
                new ResistanceDataPacket(player.ResistanceData),
                new ActorControllerInfoPacket(true),
                new LevelPacket(player.Level),
                new CharacterNamePacket(player.Name),
                new ActorNamePacket(player.FamilyName),
                new IsRunningPacket(player.IsRunning),
                // The player reads as friendly to themselves. This was Factions.AFS, which is also 1,
                // so the value is unchanged - only the type now says what it means.
                new TargetCategoryPacket(TargetCategory.Friendly),
                new PlayerFlagsPacket(player.PlayerFlags),
                new IsTrialAccountPacket(player.IsTrialAccount),
                new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory)
            };

            return entityData;
        }

        internal void GainExperience(Client client, uint experience, uint? baseGained = null, int streakMod = 1)
        {
            if (client.Player.Level >= MaxPlayerLevel)
                return; // cannot gain xp over level 50

            client.Player.Experience += experience;

            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Expirience, client.Player.Experience);

            NotifyExperienceGained(client, experience, baseGained, streakMod);
        }

        /// <summary>
        /// Sends the experience change and applies level-ups for experience that
        /// is already persisted (mission rewards commit it with the mission state).
        /// </summary>
        internal void NotifyExperienceGained(Client client, uint experience, uint? baseGained = null, int streakMod = 1)
        {
            var levelBefore = client.Player.Level;
            // streakMod above 1 makes the client append "[base Base XP] [+N% Kill Streak Bonus]".
            var xpInfo = new XPInfo(client.Player.Experience, experience, baseGained ?? experience) { StreakMod = streakMod };

            client.CallMethod(client.Player.EntityId, new ExperienceChangedPacket(xpInfo));

            ApplyLevelUps(client, experience);

            // Once, after the loop: enough experience for two levels at once is one change as
            // far as the squad window is concerned.
            if (client.Player.Level != levelBefore)
                PartyManager.Instance.MemberInfoChanged(client);
        }

        /// <summary>
        /// Levels the player up while the experience allows. A class stops at its tier gate (Recruit at 4): the
        /// experience is still credited, and the first time it reaches the tier level's threshold the player hears
        /// PM 663, the tier selection becomes pending and a clone credit is granted (class-trainer-spec R1.2-R1.5,
        /// R3.6). <paramref name="gained"/> tells a new arrival at the gate from experience gained while waiting.
        /// </summary>
        private void ApplyLevelUps(Client client, uint gained)
        {
            while (client.Player.Level < MaxPlayerLevel)
            {
                var xpForLevelUp = GetLevelNeededExperience(client.Player.Level);

                if (xpForLevelUp == -1)
                    break;

                if (client.Player.Experience >= xpForLevelUp && ClassAdvancement.IsGatedLevel(client.Player.Class, client.Player.Level + 1))
                {
                    if (client.Player.Experience - gained < xpForLevelUp)
                        ReachTierGate(client);
                    break;
                }

                if (client.Player.Experience >= xpForLevelUp)
                {
                    var previousAttributePoints = GetAvailableAttributePoints(client.Player);
                    var previousSkillPoints = GetSkillPointsAvailable(client.Player);

                    // level up
                    client.Player.Level++;

                    // update database
                    CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Level);

                    // Everyone in range, not just the player: actor.Recv_LevelUp calls
                    // SetExperienceLevel on whichever actor it arrived for, so this is what
                    // moves the level shown over someone's head. It guards the fanfare itself -
                    // the tutorial popup and ACTOR_LEVEL_UP event fire only when the entity id
                    // is the receiver's own manifestation - so onlookers just see the number.
                    client.CellCallMethod(client, client.Player.EntityId, new LevelUpPacket(client.Player.Level));

                    var msgArg = new Dictionary<string, string>
                    {
                        { "level", client.Player.Level.ToString() },
                        { "attributePts", (GetAvailableAttributePoints(client.Player) - previousAttributePoints).ToString() },
                        { "skillPts", (GetSkillPointsAvailable(client.Player) - previousSkillPoints).ToString() }
                    };

                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmLevelIncreased, msgArg, MsgFilterId.LeveledUp));

                    // update stats
                    UpdateStatsValues(client, true);
                    client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
                    SendAvailableAllocationPoints(client);
                }
                else
                    break;
            }
        }

        private void ReachTierGate(Client client)
        {
            var player = client.Player;

            // "You will not advance in level until you visit a trainer..." directly after the level-up line (B2).
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmCharacterClassesAvailable, new Dictionary<string, string>(), MsgFilterId.LeveledUp));
            client.CallMethod(player.EntityId, new AvailableCharacterClassesPacket(ClassAdvancement.ChildrenOf(player.Class)));

            // The trainer window already showed one credit on the Recruit before training (B2).
            player.CloneCredits++;
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.CloneCredits);
            client.CallMethod(player.EntityId, new CloneCreditsPacket(player.CloneCredits));
        }

        /// <summary>The tier state and clone credits a client needs after login (the first clone credit value is silent).</summary>
        public void SendAdvancementState(Client client)
        {
            var player = client.Player;
            var pending = ClassAdvancement.IsAtGate(player.Class, player.Level, player.Experience)
                ? ClassAdvancement.ChildrenOf(player.Class)
                : new List<uint>();

            client.CallMethod(player.EntityId, new CloneCreditsPacket(player.CloneCredits));
            client.CallMethod(player.EntityId, new AvailableCharacterClassesPacket(pending, silent: true));
        }

        /// <summary>
        /// SelectNewCharacterClass(classId) from the Tier Advancement window. The request names no trainer, so the
        /// server checks the living player is at the gate, within the window's range of a class trainer, and picks
        /// an immediate child of the current class. The class changes and the withheld level-ups apply (R3.1-R3.3).
        /// Skill ranks are not granted; the tier-4 signature grant is not implemented (its accounting is open).
        /// </summary>
        public void SelectNewCharacterClass(Client client, uint classId)
        {
            var player = client.Player;

            if (!MissionManager.IsInWorld(client) || player.State == CharacterState.Dead || player.Attributes[Attributes.Health].Current <= 0)
                return;

            if (ClassAdvancement.ParentOf(classId) != player.Class || classId == 0 ||
                !ClassAdvancement.IsAtGate(player.Class, player.Level, player.Experience) || !IsNearClassTrainer(player))
            {
                Logger.WriteLog(LogType.Debug, $"SelectNewCharacterClass: class {classId} refused for character {player.Id} (class {player.Class}, level {player.Level})");
                return;
            }

            player.Class = classId;
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Class, player.Class);
            client.CallMethod(player.EntityId, new CharacterClassPacket(player.Class));
            client.CallMethod(player.EntityId, new AvailableCharacterClassesPacket(new List<uint>()));

            ApplyLevelUps(client, 0);

            // Class (and usually level) are fields of the party member tuple.
            PartyManager.Instance.MemberInfoChanged(client);

            // Content reacting to the choice (the Soldier/Specialist gear missions).
            MissionManager.Instance.Content.React(client, new ContentEvent(ContentRuleEvent.ClassSelected, player.MapContextId));
        }

        private static bool IsNearClassTrainer(Manifestation player) =>
            EntityManager.Instance.Creatures.Values.Any(creature =>
                creature.Npc != null && ClassAdvancement.TrainerNpcPackages.Contains(creature.Npc.NpcPackageId) &&
                creature.State != CharacterState.Dead && creature.MapContextId == player.MapContextId &&
                (player.MapChannel == null || MapChannelManager.IsOnChannel(creature, player.MapChannel)) &&
                System.Numerics.Vector3.Distance(creature.Position, player.Position) <= ClassAdvancement.TrainerRange);

        public void DebugChgPlayerClass(Client client, uint newClassId)
        {
            client.Player.Class = newClassId;
            client.CallMethod(client.Player.EntityId, new CharacterClassPacket(client.Player.Class));
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Class, client.Player.Class);

            // Class is the third field of the same party tuple, so it goes stale the same way.
            PartyManager.Instance.MemberInfoChanged(client);
        }

        public void GainCredits(Client client, int credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, credits);
            // send player message
            client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmGotMoneyLootFromUnknown, new Dictionary<string, string> { { "amount", credits.ToString() } }, MsgFilterId.LootObtained));
        }

        public void LossCredits(Client client, int credits)
        {
            CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, credits);
        }

        public int GetAvailableAttributePoints(Manifestation player)
        {
            return AttributePointAllocation.GetAvailablePoints(player);
        }

        public void GetCustomizationChoices(Client client, GetCustomizationChoicesPacket packet)
        {
            // ToDo
            var test = EntityManager.Instance.GetEntityType(packet.EntityId);
            var testChoices = new Dictionary<int, int>
            {
                { 3663, 36 },
                { 3672, 42 },
                { 3812, 60 }
            };
            client.CallMethod(SysEntity.ClientMethodId, new CustomizationChoicesPacket(packet.EntityId, testChoices));
        }

        private int GetLevelNeededExperience(int level)
        {
            if (level < 1 || level >= 50)
                return -1;

            return ExpPerLevel.ExpRequred[level];
        }

        public int GetSkillPointsAvailable(Manifestation player)
        {
            return SkillTraining.GetAvailablePoints(player);
        }

        public void LevelSkills(Client client, LevelSkillsPacket packet)
        {
            if (packet.SkillIds == null || packet.ListLenght != packet.SkillIds.Length ||
                !SkillTraining.TryPlan(client.Player, packet.SkillIds, packet.SkillLevels, out var changes))
            {
                client.CallMethod(client.Player.EntityId, new SkillsPacket(client.Player.Skills));
                SendAvailableAllocationPoints(client);
                return;
            }

            if (changes.Count > 0)
            {
                // Persist the entire validated request before publishing new ranks.
                var entries = new List<CharacterSkillsEntry>();
                foreach (var skill in changes.Values)
                    entries.Add(new CharacterSkillsEntry(client.Player.Id, (uint)skill.SkillId, skill.AbilityId, skill.SkillLevel));
                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                    unitOfWork.CharacterSkills.AddOrUpdate(entries);
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
                {
                    Logger.WriteLog(LogType.Error, $"Skill training could not be saved: {exception.Message}");
                    client.CallMethod(client.Player.EntityId, new SkillsPacket(client.Player.Skills));
                    SendAvailableAllocationPoints(client);
                    return;
                }

                foreach (var skill in changes)
                    client.Player.Skills[skill.Key] = skill.Value;
            }

            client.CallMethod(client.Player.EntityId, new SkillsPacket(client.Player.Skills));
            client.CallMethod(client.Player.EntityId, new AbilitiesPacket(client.Player.Skills));
            SendAvailableAllocationPoints(client);
        }

        public void NotifyEquipmentUpdate(Client client)
        {
            client.CallMethod(client.Player.EntityId, new EquipmentInfoPacket(client.Player.Inventory.EquippedInventory));
        }

        public void RegisterAutoFire(Client client)
        {
            if (AutoFire.Exists(timer => timer.Client == client))
                return;
            // create timer
            var weapon = WeaponActionManager.CurrentWeapon(client);
            if (weapon == null)
                return;
            var timer = new AutoFireTimer(client, 0, 0);
            // Existing grace policy, pending original-server evidence. The original
            // client sends its first 2500 ms keepalive immediately after StartAutoFire.
            timer.MaxAliveTime = 10000;

            AutoFire.Add(timer);

            // launch missile
            //MissileManager.Instance.PlayerTryFireWeapon(timer.Client);
        }

        private static void RemoveAutoFire(Client client)
        {
            for (var i = AutoFire.Count - 1; i >= 0; i--)
                if (AutoFire[i].Client == client)
                    AutoFire.RemoveAt(i);
        }

        public void RemovePlayerCharacter(Client client)
        {
            // Called from MapChannelManager.RemovePlayer. A client that dropped while holding
            // fire stayed in the auto-fire list; once its items were destroyed CurrentWeapon
            // was null, and the next tick dereferenced it on the main loop.
            RemoveAutoFire(client);
        }

        public void RemoveAppearanceItem(Client client, EquipmentData equipmentSlotId)
        {
            if (equipmentSlotId == 0)
                return;

            client.Player.AppearanceData[equipmentSlotId].Class = 0;
            PersistAppearance(client, new CharacterAppearanceEntry((uint)equipmentSlotId, 0, 0));
        }

        public void RequestCustomization(Client client, RequestCustomizationPacket packet)
        {
            // ToDo
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestCustomization");
        }

        public void RequestPerformAbility(Client client, RequestPerformAbilityPacket packet)
        {
            if (client.State != ClientState.Ingame || client.Player.MapChannel == null ||
                client.Player.RemoveFromMap ||
                !AbilityRequirements.CanUseSkillAbility(client.Player, packet.ActionId, packet.ActionArgId) ||
                !ActorActionManager.Instance.CanBeginAbility(client.Player))
            {
                RejectAbilityRequest(client, packet);
                return;
            }

            if (packet.ActionId == ActionId.AaRecruitLightning)
            {
                if (!ActorActionManager.Instance.TryStartLightning(client, packet))
                    RejectAbilityRequest(client, packet);
                return;
            }

            // The class damage abilities, from the client's action tables.
            if (ActionTableManager.Instance.TryGetResolvable(packet.ActionId, (uint)packet.ActionArgId, out _, out _))
            {
                if (!ActorActionManager.Instance.TryStartDamageAbility(client, packet))
                    RejectAbilityRequest(client, packet);
                return;
            }

            if (packet.ActionId == ActionId.AaRecruitSprint &&
                !GameEffectManager.Instance.CanAttachSprint(client.Player, (uint)packet.ActionArgId))
            {
                RejectAbilityRequest(client, packet);
                return;
            }

            WeaponActionManager.Instance.InterruptForAbility(client);
            client.Player.MapChannel.PerformRecovery.Add(new ActionData(client.Player, packet.ActionId,
                (uint)packet.ActionArgId, packet.Target ?? 0, 0)
            {
                TargetLocation = packet.TargetLocation,
                ItemId = packet.ItemId,
                ClientYaw = packet.ClientYaw
            });
        }

        private static void RejectAbilityRequest(Client client, RequestPerformAbilityPacket packet)
        {
            var accepted = client.Player.CurrentAbility;
            if (accepted != null && accepted.Action.ActionId == packet.ActionId &&
                accepted.Action.ActionArgId == (uint)packet.ActionArgId)
                return; // Same-pair failures cannot distinguish a duplicate from the accepted request.

            // Cancel the client's prediction as well as its unresolved request.
            // UserActionFailed alone leaves the local windup/reuse timers running.
            client.CallMethod(client.Player.EntityId, new ActionFailedPacket(packet.ActionId, (uint)packet.ActionArgId));
            client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(packet.ActionId, packet.ActionArgId));
            if (packet.ActionId == ActionId.AaRecruitLightning)
                client.CallMethod(client.Player.EntityId, new ActionReuseTimesPacket(new[]
                {
                    (packet.ActionId, ActorActionManager.Instance.GetAbilityReuseRemaining(client.Player, packet.ActionId))
                }));
        }

        public void RequestToggleRun(Client client)
        {
            client.Player.IsRunning = !client.Player.IsRunning;

            client.CallMethod(client.Player.EntityId, new IsRunningPacket(client.Player.IsRunning));
        }

        /// <summary>Idle time before PlayerInactiveWarning, which also marks the player AFK.</summary>
        public const long InactiveWarningMs = 5 * 60 * 1000;

        /// <summary>Idle time before the player is sent back to character selection.</summary>
        public const long InactiveLogoutMs = 15 * 60 * 1000;

        /// <summary>
        /// Client traffic that does not mean the player is at the keyboard. It neither resets
        /// the inactivity timer nor clears AFK. Everything else the client sends counts as
        /// activity: movement, abilities, weapons, chat, inventory, targeting and so on.
        /// </summary>
        private static readonly HashSet<GameOpcode> AutomaticOpcodes = new()
        {
            GameOpcode.Ping,                // client resends every 1s from RequestNetworkStats()
            GameOpcode.AutoFireKeepAlive,   // client resends every 2.5s while autofire runs
            GameOpcode.MapLoaded,           // sent automatically once a zone finishes loading
            GameOpcode.TeleportAcknowledge  // automatic reply to a server-initiated teleport
        };

        /// <summary>
        /// Called for every inbound client method call. Resets the inactivity timer and clears
        /// AFK (telling everyone in range) the first time the player actually does something.
        /// </summary>
        public void NotifyPlayerActivity(Client client, GameOpcode opcode)
        {
            if (AutomaticOpcodes.Contains(opcode))
                return;

            ResetInactivity(client);

            // /afk is a deliberate action, so it resets the timer above, but clearing AFK here
            // would immediately undo the flag it is setting.
            if (opcode == GameOpcode.ToggleAfk)
                return;

            SetAfk(client, false);
        }

        /// <summary>Called for movement, which arrives outside CallServerMethod.</summary>
        public void NotifyPlayerActivity(Client client)
        {
            ResetInactivity(client);

            // SetAfk returns immediately when the flag is already clear, so this costs one
            // bool comparison on the movement path and only broadcasts on a real transition.
            SetAfk(client, false);
        }

        /// <summary>
        /// Starts a fresh idle stretch. Also called when a player enters the world or finishes
        /// a teleport, so time spent on a loading screen never counts as idle.
        /// </summary>
        public void ResetInactivity(Client client)
        {
            client.Player.LastActivityTick = Environment.TickCount64;
            client.Player.InactiveWarningSent = false;
        }

        /// <summary>
        /// Run from the MapChannelWorker every tick, ahead of its removal pass. At
        /// InactiveWarningMs the player is marked AFK and warned; at InactiveLogoutMs they are
        /// flagged the same way CharacterLogout flags a /logout, so the removal pass returns
        /// them to character selection on the same tick.
        /// </summary>
        public void CheckInactivity(MapChannel mapChannel)
        {
            var now = Environment.TickCount64;

            foreach (var client in mapChannel.ClientList)
            {
                // Loading, teleporting, already leaving, or a dropped connection awaiting removal.
                if (client == null || client.State != ClientState.Ingame || client.Player.RemoveFromMap || client.Player.Disconected)
                    continue;

                var idle = now - client.Player.LastActivityTick;

                if (idle >= InactiveLogoutMs)
                {
                    Logger.WriteLog(LogType.Network, $"{client.Player.FamilyName} inactive for {idle / 60000} minutes, returning to character selection");

                    // Same effect as MapChannelManager.CharacterLogout, without its LogoutActive
                    // gate: that flag only exists to confirm the client asked to leave.
                    // Recv_BeginCharacterSelection switches the client to character selection
                    // from any input state, so the client does not need to cooperate.
                    client.State = ClientState.LoggedIn;
                    client.Player.RemoveFromMap = true;
                    continue;
                }

                if (idle >= InactiveWarningMs && !client.Player.InactiveWarningSent)
                {
                    client.Player.InactiveWarningSent = true;

                    // AFK first, so the player reads "You are now AFK" followed by the warning,
                    // and everyone in range sees the idle marker.
                    SetAfk(client, true);
                    client.CallMethod(client.Player.EntityId, new PlayerInactiveWarningPacket());
                }
            }
        }

        public void ToggleAfk(Client client)
        {
            SetAfk(client, !client.Player.IsAFK);
        }

        public void SetAfk(Client client, bool isAfk)
        {
            if (client.Player.IsAFK == isAfk)
                return;

            client.Player.IsAFK = isAfk;

            // Broadcast to everyone in visibility range, including the player: the client
            // shows the "you are AFK" system message only for its own manifestation, and an
            // idle indicator over anyone else's head.
            client.CellCallMethod(client, client.Player.EntityId, new PlayerAfkPacket(isAfk));

            // The squad window reads a different source: its own party tuples, not the
            // manifestation. A squadmate on another map is not in visibility range at all, so
            // without this they never learn the member went away.
            PartyManager.Instance.MemberInfoChanged(client);
        }

        public void RequestWeaponDraw(Client client)
            => WeaponActionManager.Instance.Request(client, ActionId.WeaponDraw, true);

        public void RequestWeaponReload(Client client, bool automatic)
            => WeaponActionManager.Instance.Request(client, ActionId.WeaponReload, !automatic);

        public void RequestWeaponStow(Client client)
            => WeaponActionManager.Instance.Request(client, ActionId.WeaponStow, true);

        public void SaveCharacterOptions(Client client, SaveCharacterOptionsPacket packet)
        {
            if (packet.OptionsList.Count == 0)
                return;

            client.Player.CharacterOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.Player.CharacterOptions)
                unitOfWork.CharacterOptions.AddOrUpdate(client.Player.Id, (uint)option.OptionId, option.Value);

            // AddOrUpdate only stages the rows; without this they were thrown away on dispose,
            // and every option the client saved was back to its default at the next login.
            unitOfWork.Complete();
        }

        // maybe move this to other manager becose it's account related
        public void SaveUserOptions(Client client, SaveUserOptionsPacket packet)
        {
            client.UserOptions = packet.OptionsList;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var option in client.UserOptions)
                unitOfWork.UserOptions.AddOrUpdate(client.AccountEntry.Id, (uint)option.OptionId, option.Value);

            unitOfWork.Complete();
        }

        internal void SendAvailableAllocationPoints(Client client)
        {
            // update available allocation points (attributes, trainPts, skillPts)

            var attributePoints = GetAvailableAttributePoints(client.Player);
            var trainPoints = 0;    // not used by te client
            var skillPoints = GetSkillPointsAvailable(client.Player);

            client.CallMethod(client.Player.EntityId, new AvailableAllocationPointsPacket(attributePoints, trainPoints, skillPoints));
        }

        public void SetAppearanceItem(Client client, Item item)
        {
            var equipmentSlotId = EntityClassManager.Instance.GetEquipableClassInfo(item).EquipmentSlotId;

            if (!client.Player.AppearanceData.ContainsKey(equipmentSlotId))
                client.Player.AppearanceData.Add(equipmentSlotId, new AppearanceData { SlotId = equipmentSlotId });

            client.Player.AppearanceData[equipmentSlotId].Class = (uint)item.ItemTemplate.Class;
            client.Player.AppearanceData[equipmentSlotId].Color = new Color(item.Color);
            client.Player.AppearanceData[equipmentSlotId].Hue2 = new Color(item.Color);

            PersistAppearance(client, new CharacterAppearanceEntry((uint)equipmentSlotId, (uint)item.ItemTemplate.Class, item.Color));
        }

        private void PersistAppearance(Client client, CharacterAppearanceEntry appearance)
        {
            try
            {
                using var work = _gameUnitOfWorkFactory.CreateChar();
                work.CharacterAppearances.AddOrUpdate(client.Player.Id, appearance);
                work.Complete();
            }
            catch (Exception exception) when (exception is System.Data.Common.DbException ||
                exception is Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // Equipment locations already committed. A cosmetic save must
                // not prevent authoritative stats/equipment from refreshing.
                Logger.WriteLog(LogType.Error, exception);
            }
        }

        public void SetDesiredCrouchState(Client client, bool crouching)
        {
            client.Player.IsCrouching = crouching;

            client.CallMethod(client.Player.EntityId, new SetDesiredCrouchStatePacket(client.Player.IsCrouching ? CharacterState.Crouched : CharacterState.Standing));
        }

        public void SetTargetId(Client client, ulong entityId)
        {
            client.Player.Target = entityId;
        }

        public void SetTrackingTarget(Client client, ulong entityId)
        {
            client.Player.TrackingTargetEntityId = entityId;
        }

        public void UpdateAppearance(Client client)
        {
            if (client.Player == null)
                return;

            client.CellCallMethod(client, client.Player.EntityId, new AppearanceDataPacket(client.Player.AppearanceData));
        }

        // Health calculation:
        //levelBasedHealth = 20000.0
        //for (int i = level; i< 50; ++i)
        //levelBasedHealth = levelBasedHealth - 0.082995 * levelBasedHealth;

        private readonly float[] HealthBaselinePerLevel =
         {
            286.5784148f,
            312.51565127f,
            340.8003787f,
            371.6450605f,
            405.28138942f,
            441.96202792f,
            481.96250612f,
            525.58329139f,
            573.15204539f,
            625.02608535f,
            681.59506802f,
            743.28391668f,
            810.55601298f,
            883.91667764f,
            963.91696626f,
            1051.15780858f,
            1146.29452247f,
            1250.04173638f,
            1363.17875735f,
            1486.55542483f,
            1621.09849437f,
            1767.818599f,
            1927.81784069f,
            2102.29806892f,
            2292.56990847f,
            2500.06260431f,
            2726.33475751f,
            2973.08603281f,
            3242.1699258f,
            3535.60768567f,
            3855.60349799f,
            4204.56104164f,
            4585.10154431f,
            5000.08347207f,
            5452.62400104f,
            5946.1224323f,
            6484.28572615f,
            7071.15634718f,
            7711.14262974f,
            8409.05189147f,
            9170.12654399f,
            10000.08347172f,
            10905.15697485f,
            11892.14559882f,
            12968.4632023f,
            14142.19464703f,
            15422.15652808f,
            16817.9634005f,
            18340.1f,
            20000f
        };

        /*
         * ToDO (this still need work, this is just copied from c++ projet
         * Updates all attributes depending on level, spent attribute points, etc.
         * Does not send values to clients
         * If fullreset is true, the current values of each attribute are set to the maximum
         */
        /// <summary>
        /// A worn piece's body armour: the client's own armorclass absorption for its class, divided by ten and
        /// truncated.
        ///
        /// The scale is observed: the boot-camp crate's gloves read "Body Armor: 28" (footage A3-034 t=319.6) and
        /// absorb 281. The rounding comes from a transcribed original tooltip, "Pulsar Reflective Armor Gloves, Min
        /// Level 5, Body Armor: 73" (TaRapedia User:Zarevak/Sandbox): the level-5 Reflective gloves absorb 526, 631,
        /// 736 or 841, and only truncating 736 gives 73 - rounding would show 74. (Its "Regen Rate: 7% per sec" does
        /// not match that class's regen of 2, so the match is the armour line's alone.)
        /// itemtemplate_armor's values are not used: the 2026-09-13 source sweep traced them to Infinite Rasa, whose
        /// armorValue has no client counterpart and mixes rounding with truncation. min and max absorption are
        /// equal for all 3,377 classes, so which one is read makes no difference.
        /// </summary>
        public static int BodyArmor(Item item)
        {
            var template = item?.ItemTemplate;
            if (template == null)
                return 0;

            var armorClass = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(template.Class, out var entityClass)
                ? entityClass.ArmorClassInfo
                : null;
            return armorClass == null ? 0 : (int)(armorClass.MaxDamageAbsorbed / 10);
        }

        public void UpdateStatsValues(Client client, bool fullreset)
        {
            var player = client.Player;
            var attribute = player.Attributes;

            int level = player.Level;

            // We don't want things to blow up just in case something wrong happens to level
            if (level < 0) level  = 1;
            if (level > 50) level = 50;

            int levelBasedBody   = 0;
            int levelBasedMind   = 0;
            int levelBasedSpirit = 0;

            switch (player.Race)
            {
                case Race.Human:
                    levelBasedBody = levelBasedMind = levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Forean:
                    levelBasedBody = (level - 1) + 10;
                    levelBasedMind = 3 * (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;

                case Race.Brann:
                    levelBasedBody = levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 4 * (level - 1) + 10;
                    break;

                case Race.Thrax:
                    levelBasedBody = 3 * (level - 1) + 10;
                    levelBasedMind = (level - 1) + 10;
                    levelBasedSpirit = 2 * (level - 1) + 10;
                    break;
            }

            int totalBody   = levelBasedBody   + player.SpentBody;
            int totalMind   = levelBasedMind   + player.SpentMind;
            int totalSpirit = levelBasedSpirit + player.SpentSpirit;

            // Health
            float levelBasedHealth = HealthBaselinePerLevel[level - 1];
            levelBasedHealth = levelBasedHealth / (2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10);
            int totalHealth = (int)(levelBasedHealth * (totalSpirit + 2 * totalBody));

            // The per-point factor shrinks with level; the attribute totals grow. Both sides of
            // these divisions were int, so the factor was truncated: 3 at level 1 instead of
            // 3.43, 1 from level 20 (1.11), and 0 from level 26 for power and 39 for regen - a
            // character that levelled far enough had no chi and no regeneration at all. The
            // health line above already divides as float and was right.
            float attributeDivisor = 2 * (level - 1) + 2 * (2 * (level - 1) + 10) + 10;

            // Regen
            float baseRegen = (2 * level + 100) / attributeDivisor;
            int totalRegen = (int)(baseRegen * (totalMind + 2 * totalSpirit));
          
            // Bonuses. Resuscitation Trauma lowers the three attributes by 20% a stack.
            var bodyBonus = -DeathPenaltyRules.AttributePenalty(totalBody, player.TraumaStacks);
            var mindBonus = -DeathPenaltyRules.AttributePenalty(totalMind, player.TraumaStacks);
            var spiritBonus = -DeathPenaltyRules.AttributePenalty(totalSpirit, player.TraumaStacks);

            var healthBonus = 0;
            var chiBonus    = 0;
            var regenBonus  = 0;

            // Attribute buffs held as game effects (Bio Augmentation).
            foreach (var effect in player.ActiveEffects.Values)
                switch (effect.BonusAttribute)
                {
                    case Attributes.Body: bodyBonus += effect.AttributeBonus; break;
                    case Attributes.Mind: mindBonus += effect.AttributeBonus; break;
                    case Attributes.Spirit: spiritBonus += effect.AttributeBonus; break;
                    case Attributes.Health: healthBonus += effect.AttributeBonus; break;
                }
            foreach (var effect in player.ActiveEffects.Values)
                regenBonus += effect.RegenBonusPercent;

            float armorBonusPercent = (float)Math.Max(0.0, (totalBody - (2 * (level - 1) + 10)) * 0.667);   // every body attribute over the default base attribute gives 0.667% bonus armo;
            float logosBonusPercent = (float)Math.Max(0.0, (totalMind - (2 * (level - 1) + 10)) * 0.375);   // every mind attribute over the default base attribute gives 0.375% bonus logos damage
            float critBonusPercent = (float)Math.Max(0.0, (totalSpirit - (2 * (level - 1) + 10)) * 0.065);  // every spirit attribute over the default base attribute gives 0.065% bonus crit chance;
            player.SpiritCritPercent = critBonusPercent;


            // body
            attribute[Attributes.Body].NormalMax    = totalBody;
            attribute[Attributes.Body].CurrentMax   = attribute[Attributes.Body].NormalMax + bodyBonus;
            attribute[Attributes.Body].Current      = attribute[Attributes.Body].CurrentMax;

            attribute[Attributes.Mind].NormalMax    = totalMind;
            attribute[Attributes.Mind].CurrentMax   = attribute[Attributes.Mind].NormalMax + mindBonus;
            attribute[Attributes.Mind].Current      = attribute[Attributes.Mind].CurrentMax;

            attribute[Attributes.Spirit].NormalMax  = totalSpirit;
            attribute[Attributes.Spirit].CurrentMax = attribute[Attributes.Spirit].NormalMax + spiritBonus;
            attribute[Attributes.Spirit].Current    = attribute[Attributes.Spirit].CurrentMax;

            // health
            attribute[Attributes.Health].NormalMax  = totalHealth;
            attribute[Attributes.Health].CurrentMax = totalHealth + healthBonus;

            // chi/adrenaline: the signature shows a fixed 1000-unit bar (percentage units),
            // not a power-scaled pool. See docs/sprint-client-evidence.md.
            attribute[Attributes.Chi].NormalMax     = NormalAdrenalineMaximum;
            attribute[Attributes.Chi].CurrentMax    = NormalAdrenalineMaximum;

            attribute[Attributes.Regen].NormalMax   = totalRegen; // regenRate in percent
            attribute[Attributes.Regen].CurrentMax  = totalRegen + regenBonus;

            if (fullreset)
            {
                attribute[Attributes.Health].Current = attribute[Attributes.Health].CurrentMax;
                attribute[Attributes.Chi].Current = attribute[Attributes.Chi].CurrentMax;
            }
            else
            {
                attribute[Attributes.Health].Current = Math.Min(attribute[Attributes.Health].Current, attribute[Attributes.Health].CurrentMax);
                attribute[Attributes.Chi].Current = Math.Min(attribute[Attributes.Chi].Current, attribute[Attributes.Chi].CurrentMax);
            }


            // update regen rate: 2.0 per second at 100% regen, scaled by the rate as a
            // percentage. CurrentMax / 100 was int division, so any rate below 200% rounded
            // to the base 2 and the rate only mattered in whole multiples of 100.
            attribute[Attributes.Regen].RefreshAmount = (int)Math.Round(2D * attribute[Attributes.Regen].CurrentMax / 100, 0);
            // 2.0 per second is the base regeneration for health. The Regen attribute is only displayed; health and
            // power regenerate from their own refresh amounts - "Regeneration Rate: Improves natural Health and Power
            // regeneration, and Adrenaline gain" (ID_TOOLTIP_ATTRIBUTES_REGEN) - so both take the same rate. Inferred:
            // the 2-per-second base is this emulator's, and no source gives power a separate one.
            player.HealthRegenRate = 2D * attribute[Attributes.Regen].CurrentMax / 100;
            player.PowerRegenRate = player.HealthRegenRate;
            // calculate armor max
            var armorMax = 0.0d;
            //float armorBonus = 0; // todo! (From item modules)
            var armorBonusPct = player.Attributes[Attributes.Body].CurrentMax * 0.0066666d;
            var armorRegenRate = 0;

            // Resistances come from the equipment's own resist lists, summed per damage type. The client already
            // applies the same conversion to what it is sent, but until now the list was only ever sent: nothing
            // accumulated it and combat never read it, so resistance gear did nothing in a fight.
            var resistances = new Dictionary<DamageType, int>();

            for (var i = 1; i < 21; i++)
            {
                if (client.Player.Inventory.EquippedInventory[i] == 0)
                    continue;

                // skip weapon slot
                if (i == 13)
                    continue;

                var equipmentItem = EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[i]);
                if (equipmentItem == null)
                {
                    // this is very bad, how can the item disappear while it is still linked in the inventory?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Equipment item has no physical copy (item is missing)");
                    continue;
                }
                var classInfo = EntityClassManager.Instance.GetClassInfo(equipmentItem.ItemTemplate.Class);
                if (classInfo?.ArmorClassInfo == null)
                {
                    // how can the player equip non-armor?
                    Logger.WriteLog(LogType.Error, "UpdateStatsValues: Player try to equip non_armor item");
                    continue;
                }
                armorMax += BodyArmor(equipmentItem);
                armorRegenRate += classInfo.ArmorClassInfo.RegenRate;

                foreach (var resistance in equipmentItem.ItemTemplate.EquipableInfo?.ResistList ?? new List<ResistanceData>())
                    resistances[resistance.ResistanceType] =
                        resistances.TryGetValue(resistance.ResistanceType, out var current)
                            ? current + resistance.ResistanceAmmount
                            : resistance.ResistanceAmmount;
                
                // what about damage absorbed? Was it used at all?
            }
            player.ResistanceData.Clear();
            foreach (var (damageType, amount) in resistances)
                player.ResistanceData.Add(new ResistanceData(damageType, amount));

            armorMax = armorMax * (1.0d + armorBonusPct);

            // The worn armour's own recharge ("Regen Rate: 1 per sec" on its tooltip), scaled by any
            // EFFECT_ARMOR_REGEN_MODIFIER in force - Base Wave's 500 is five times the recharge.
            var armorRegenPercent = 100D;
            foreach (var effect in player.ActiveEffects.Values)
                if (effect.ArmorRegenPercent is int percent)
                    armorRegenPercent = armorRegenPercent * percent / 100;
            player.ArmorRegenRate = armorRegenRate * armorRegenPercent / 100;
            attribute[Attributes.Armor].NormalMax = (int)Math.Round(armorMax, 0);
            attribute[Attributes.Armor].CurrentMax = attribute[Attributes.Armor].NormalMax;
            if (fullreset)
                attribute[Attributes.Armor].Current = attribute[Attributes.Armor].CurrentMax;
            else
                attribute[Attributes.Armor].Current = Math.Min(attribute[Attributes.Armor].Current, attribute[Attributes.Armor].CurrentMax);
            // added by krssrb
            // power test
            attribute[Attributes.Power].NormalMax = 100 + (player.Level - 1) * 2 * 4 + player.SpentMind * 3;
            var powerBonus = 0;
            attribute[Attributes.Power].CurrentMax = attribute[Attributes.Power].NormalMax + powerBonus;
            if (fullreset)
                attribute[Attributes.Power].Current = attribute[Attributes.Power].CurrentMax;
            else
                attribute[Attributes.Power].Current = Math.Min(attribute[Attributes.Power].Current, attribute[Attributes.Power].CurrentMax);

            ApplyRegenRates(player);
        }

        // ------------------------------------------------------------------ regeneration and combat

        /// <summary>
        /// Sets the health, power and armour refresh amounts the client predicts from, for the player's combat state:
        /// the out-of-combat rate, times IN_COMBAT_REGEN_MODIFIER in a fight, rounded to the whole amount the wire
        /// carries. The period is the client's 1 s. See <see cref="CombatRegen"/>.
        /// </summary>
        public void ApplyRegenRates(Manifestation player)
        {
            var modifier = player.InCombat ? CombatRegen.InCombatModifier : 1D;
            void Set(Attributes type, double rate)
            {
                if (!player.Attributes.TryGetValue(type, out var attribute))
                    return;
                attribute.RefreshAmount = (int)Math.Round(rate * modifier, MidpointRounding.AwayFromZero);
                attribute.RefreshPeriod = CombatRegen.RegenPeriodSeconds;
            }
            Set(Attributes.Health, player.HealthRegenRate);
            Set(Attributes.Power, player.PowerRegenRate);
            Set(Attributes.Armor, player.ArmorRegenRate);
        }

        /// <summary>Puts a player in combat, or keeps them there: called for both the one dealing damage and the one taking it.</summary>
        public void EnterCombat(Actor actor)
        {
            if (actor is not Manifestation player || player.State == CharacterState.Dead)
                return;
            player.CombatExpiresAt = Environment.TickCount64 + CombatRegen.CombatTimeoutMs;
            if (player.InCombat)
                return;
            player.InCombat = true;
            CombatChanged(player, new PlayerEnteredCombatPacket());
        }

        /// <summary>Takes a player out of combat and restores their full regeneration.</summary>
        public void ExitCombat(Manifestation player)
        {
            if (!player.InCombat)
                return;
            player.InCombat = false;
            player.CombatExpiresAt = 0;
            CombatChanged(player, new PlayerExitedCombatPacket());
        }

        /// <summary>
        /// The new rates go out as UpdateHealth, UpdatePower and UpdateArmor - the packets whose receiver sets the
        /// refresh amount - to everyone who sees the player; the combat indicator only to the player.
        /// </summary>
        private void CombatChanged(Manifestation player, ServerPythonPacket indicator)
        {
            ApplyRegenRates(player);
            var map = player.MapChannel;
            if (map == null)
                return;
            foreach (var client in map.ClientList)
                if (client.Player == player && client.State == ClientState.Ingame)
                    client.CallMethod(player.EntityId, indicator);
            if (player.Attributes.TryGetValue(Attributes.Health, out var health))
                CellManager.Instance.CellCallMethod(map, player, new UpdateHealthPacket(health, player.EntityId));
            if (player.Attributes.TryGetValue(Attributes.Power, out var power))
                CellManager.Instance.CellCallMethod(map, player, new UpdatePowerPacket(power, player.EntityId));
            if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
                CellManager.Instance.CellCallMethod(map, player, new UpdateArmorPacket(armor, player.EntityId));
        }

        /// <summary>
        /// Once a second, adds each living player's refresh amounts to health, power and armour, up to their maxima - the
        /// same whole amounts the client is predicting, so no packet is needed - and drops players whose combat has lapsed.
        /// </summary>
        public void RegenWorker(MapChannel map, long delta)
        {
            var now = Environment.TickCount64;
            foreach (var client in map.ClientList)
            {
                var player = client?.Player;
                if (player == null)
                    continue;
                if (player.InCombat && now >= player.CombatExpiresAt)
                    ExitCombat(player);

                if (player.State == CharacterState.Dead)
                {
                    player.RegenElapsedMs = 0;
                    continue;
                }
                player.RegenElapsedMs += delta;
                var periodMs = CombatRegen.RegenPeriodSeconds * 1000L;
                while (player.RegenElapsedMs >= periodMs)
                {
                    player.RegenElapsedMs -= periodMs;
                    foreach (var type in new[] { Attributes.Health, Attributes.Power, Attributes.Armor })
                    {
                        if (player.Attributes.TryGetValue(type, out var attribute) && attribute.RefreshAmount > 0
                            && attribute.Current < attribute.CurrentMax)
                            attribute.Current = Math.Min(attribute.CurrentMax, attribute.Current + attribute.RefreshAmount);
                    }
                }
            }
        }

        public void WeaponReady(Client client, bool isReady)
        {
            client.Player.WeaponReady = isReady;
            client.CallMethod(client.Player.EntityId, new WeaponReadyPacket(isReady));
        }

        #endregion
    }
}
