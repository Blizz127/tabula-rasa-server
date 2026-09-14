using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Packets.Mission.Server;
    using Repositories.Char.CharacterMission;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;
    using Structures.Content;
    using Structures.World;

    /// <summary>
    /// Mission definitions and per-character mission-log state. The wire shapes
    /// follow the recovered 1.16.5.0 client (client/missionlog.pyo,
    /// client/augmentations/npc.pyo, client/ui/conversationwindow.pyo); the
    /// storage layout is this emulator's own, and mission content stays empty
    /// until it is recovered from original evidence.
    /// </summary>
    public class MissionManager
    {
        /*      Mission Packets:
         * - MissionStatusInfo(self, missionStatusDict)
         * - MissionCompleteable(self, missionId, bCompleteable)
         * - MissionCompleted(self, missionId)
         * - MissionRewarded(self, missionId)
         * - MissionFailed(self, missionId)
         * - MissionDiscarded(self, missionId)
         * - MissionCleared(self, missionId)
         * - MissionGained(self, missionId, missionInfo)
         * - ObjectiveRevealed(self, missionId, objectiveId, missionInfo)
         * - ObjectiveActivated(self, missionId, objectiveId)
         * - ObjectiveCompleted(self, missionId, objectiveId)
         * - ObjectiveFailed(self, missionId, objectiveId)
         * - UpdateObjectiveCounter(self, missionId, objectiveId, counterId, counterVal, initialVal, targetVal)
         * - UpdateObjectiveItemCounter(self, missionId, objectiveId, itemClassId, counterVal, targetVal)
         * - DispenseSharedMission(self, actorId, missionId, missionInfo)
         * - DispenseRadioMission(self, missionId, missionInfo, bForceDialog)
         */

        private static MissionManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly Func<uint> _now;

        public readonly Dictionary<uint, Mission> LoadedMissions = new Dictionary<uint, Mission>();

        // Installed by NpcManager so NPC overhead markers follow a player's mission state.
        public Action<Client, Creature> ConversationStatusRefresher { get; set; }

        private MissionContentManager _content;

        // Reconstructed-content rules that react to mission events (the server's singleton by default).
        public MissionContentManager Content
        {
            get => _content ?? MissionContentManager.Instance;
            set => _content = value;
        }

        public static MissionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MissionManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        public MissionManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory, Func<uint> now = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _now = now ?? (() => (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        #region Definitions

        public void LoadMissions()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            LoadedMissions.Clear();

            foreach (var mission in unitOfWork.NpcMissions.Get())
                LoadedMissions.Add(mission.Id, new Mission(mission));

            // The client's missionobjective/objectiveconversation tables are seeded
            // in full at original tier, but npc_mission rows exist only where
            // server-authoritative columns are evidenced. Rows without a parent
            // definition are inert; one summary line keeps that expected state
            // visible without one log line per row.
            var orphanObjectives = 0;
            foreach (var objective in unitOfWork.NpcMissionObjectives.Get())
            {
                if (LoadedMissions.TryGetValue(objective.MissionId, out var mission))
                    mission.Objectives[objective.ObjectiveId] = new MissionObjectiveDefinition(objective);
                else
                    orphanObjectives++;
            }

            var orphanConversations = 0;
            var objectivelessConversations = 0;
            foreach (var conversation in unitOfWork.NpcMissionObjectives.GetConversations())
            {
                if (!LoadedMissions.TryGetValue(conversation.MissionId, out var mission))
                    orphanConversations++;
                else if (!mission.Objectives.ContainsKey(conversation.ObjectiveId))
                    objectivelessConversations++;
                else
                    mission.ObjectiveConversations.Add(new MissionObjectiveConversation
                    {
                        ObjectiveId = conversation.ObjectiveId,
                        NpcPackageId = conversation.NpcPackageId,
                        PlayerFlagId = conversation.PlayerFlagId,
                        ConvoType = conversation.ConvoType
                    });
            }

            if (orphanObjectives > 0)
                Logger.WriteLog(LogType.Initialize, $"LoadMissions: {orphanObjectives} objective rows have no npc_mission row (client skeleton, definition pending)");

            if (orphanConversations > 0 || objectivelessConversations > 0)
                Logger.WriteLog(LogType.Initialize, $"LoadMissions: {orphanConversations} objective conversation rows have no npc_mission row and {objectivelessConversations} reference unknown objectives (client skeleton, definition pending)");

            foreach (var transition in unitOfWork.NpcMissionObjectives.GetTransitions())
            {
                if (LoadedMissions.TryGetValue(transition.MissionId, out var mission) &&
                    mission.Objectives.ContainsKey(transition.CompletedObjectiveId) &&
                    mission.Objectives.ContainsKey(transition.RevealedObjectiveId))
                {
                    if (!mission.Transitions.TryGetValue(transition.CompletedObjectiveId, out var revealed))
                        mission.Transitions[transition.CompletedObjectiveId] = revealed = new List<uint>();

                    revealed.Add(transition.RevealedObjectiveId);
                }
                else
                    Logger.WriteLog(LogType.Error, $"LoadMissions: transition {transition.MissionId}/{transition.CompletedObjectiveId}->{transition.RevealedObjectiveId} references unknown objectives");
            }

            foreach (var mission in LoadedMissions.Values)
            {
                mission.Rewards.AddRange(unitOfWork.NpcMissionRewards.Get(mission.MissionId));
                BuildRewardInfo(mission);
                mission.RefreshDispenseObjectives();
            }
            // Offerability is logged after the content layer attaches completion bindings;
            // a definition complete except for its bindings would be misreported here.
        }

        /// <summary>
        /// Normalizes npc_mission_reward rows into the client-facing reward
        /// description and the payout used at turn-in, so both always agree.
        /// Rows the server cannot represent faithfully (unknown type or template,
        /// non-positive amounts, repeated currency rows) become definition gaps
        /// instead of being shown one way and paid another. Experience is not part
        /// of the client's rewardInfo tuple.
        /// </summary>
        public static void BuildRewardInfo(Mission mission)
        {
            var rewardInfo = new RewardInfo();
            var currencies = new HashSet<NpcMissionRewardType>();
            var itemRewards = false;

            mission.RewardCredits = mission.RewardPrestige = mission.RewardExperience = 0;
            mission.OfferedFixedItems.Clear();
            mission.OfferedSelectableRewards.Clear();
            mission.RewardGaps.Clear();

            foreach (var reward in mission.Rewards)
            {
                var type = (NpcMissionRewardType)reward.Type;

                switch (type)
                {
                    case NpcMissionRewardType.Credits:
                    case NpcMissionRewardType.Prestige:
                    case NpcMissionRewardType.Experience:
                        if (reward.Credits <= 0)
                        {
                            mission.RewardGaps.Add($"{type} reward amount {reward.Credits} is not positive");
                            break;
                        }

                        if (!currencies.Add(type))
                        {
                            mission.RewardGaps.Add($"more than one {type} reward row");
                            break;
                        }

                        if (type == NpcMissionRewardType.Credits)
                        {
                            mission.RewardCredits = reward.Credits;
                            rewardInfo.FixedReward.Credits[CurencyType.Credits] = (uint)reward.Credits;
                        }
                        else if (type == NpcMissionRewardType.Prestige)
                        {
                            mission.RewardPrestige = reward.Credits;
                            rewardInfo.FixedReward.Credits[CurencyType.Prestige] = (uint)reward.Credits;
                        }
                        else
                            mission.RewardExperience = reward.Credits;

                        break;

                    case NpcMissionRewardType.FixedItem:
                    case NpcMissionRewardType.SelectableItem:
                        // Item rewards would be created and placed after the completion
                        // commit, so a full or unsuitable inventory could consume the
                        // mission without the item. Until that is atomic they are withheld.
                        if (!itemRewards)
                        {
                            mission.RewardGaps.Add("item reward delivery is not implemented");
                            itemRewards = true;
                        }

                        var itemTemplate = ItemManager.Instance.GetItemTemplateById(reward.ItemTemplateId);

                        if (itemTemplate == null)
                        {
                            mission.RewardGaps.Add($"reward item template {reward.ItemTemplateId} is unknown");
                            break;
                        }

                        var itemClass = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(itemTemplate.Class, out var entityClass) ? entityClass.ItemClassInfo : null;

                        if (itemClass == null || itemTemplate.InventoryCategory < InventoryCategory.Equipment || itemTemplate.InventoryCategory > InventoryCategory.Misc)
                        {
                            mission.RewardGaps.Add($"reward item template {reward.ItemTemplateId} has no inventory placement");
                            break;
                        }

                        if (reward.Quantity == 0 || reward.Quantity > itemClass.StackSize)
                        {
                            mission.RewardGaps.Add($"reward item template {reward.ItemTemplateId} quantity {reward.Quantity} is outside 1..{itemClass.StackSize}");
                            break;
                        }

                        var rewardItem = new RewardItem
                        {
                            ItemTemplateId = reward.ItemTemplateId,
                            Class = itemTemplate.Class,
                            Quantity = reward.Quantity,
                            QualityId = itemTemplate.QualityId
                        };

                        if (type == NpcMissionRewardType.FixedItem)
                        {
                            mission.OfferedFixedItems.Add(reward);
                            rewardInfo.FixedReward.FixedItems.Add(rewardItem);
                        }
                        else
                        {
                            mission.OfferedSelectableRewards.Add(reward);
                            rewardInfo.SelectableReward.Add(rewardItem);
                        }

                        break;

                    default:
                        mission.RewardGaps.Add($"unknown reward type {reward.Type}");
                        break;
                }
            }

            mission.MissionConstantData.RewardInfo = rewardInfo;
        }

        #endregion

        #region Player state

        public Dictionary<uint, PlayerMission> LoadPlayerMissions(ICharacterMissionRepository repository, uint accountId, byte slot, uint characterId)
        {
            var missions = new Dictionary<uint, PlayerMission>();

            foreach (var entry in repository.Get(accountId, slot))
                missions[entry.MissionId] = new PlayerMission
                {
                    MissionId = entry.MissionId,
                    State = (MissionState)entry.MissionState,
                    ChangeTime = entry.ChangeTime
                };

            foreach (var objective in repository.GetObjectives(characterId))
            {
                if (missions.TryGetValue(objective.MissionId, out var mission))
                    mission.Objectives[objective.ObjectiveId] = (MissionObjectiveState)objective.Status;
                else
                    Logger.WriteLog(LogType.Error, $"Character {characterId}: objective row {objective.MissionId}/{objective.ObjectiveId} has no mission row");
            }

            return missions;
        }

        /// <summary>
        /// Reveals objectives that saved progress lacks because the definition
        /// changed after the mission was accepted: objectives revealed on
        /// acceptance, and transitions from objectives already completed.
        /// Without this an active mission could never become completeable.
        /// </summary>
        public void ReconcilePlayerMissions(Client client)
        {
            var player = client.Player;

            foreach (var mission in player.Missions.Values)
            {
                if (mission.State != MissionState.Active || !LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    continue;

                var known = new HashSet<uint>(mission.Objectives.Keys);
                var added = new List<uint>();

                foreach (var objective in definition.ObjectivesInOrder)
                    if (objective.RevealedOnAccept == true && known.Add(objective.ObjectiveId))
                        added.Add(objective.ObjectiveId);

                var completed = new Queue<uint>(mission.Objectives.Where(entry => entry.Value == MissionObjectiveState.Completed).Select(entry => entry.Key));

                while (completed.Count > 0)
                    if (definition.Transitions.TryGetValue(completed.Dequeue(), out var revealed))
                        foreach (var next in revealed)
                            if (definition.Objectives.ContainsKey(next) && known.Add(next))
                                added.Add(next);

                if (added.Count == 0)
                    continue;

                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                    foreach (var objectiveId in added)
                        unitOfWork.CharacterMissions.AddObjective(new CharacterMissionObjectiveEntry(player.Id, mission.MissionId, objectiveId, (uint)MissionObjectiveState.Incomplete));
                    unitOfWork.Complete();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"ReconcilePlayerMissions: could not reveal objectives of mission {mission.MissionId} for character {player.Id}");
                    Logger.WriteLog(LogType.Error, e);
                    continue;
                }

                foreach (var objectiveId in added)
                    mission.Objectives[objectiveId] = MissionObjectiveState.Incomplete;

                // Nearby NPCs were introduced with the pre-reconciliation log.
                RefreshRelatedNpcStatus(client, definition);

                Logger.WriteLog(LogType.Initialize, $"Character {player.Id}: mission {mission.MissionId} definition changed, revealed objectives {string.Join(", ", added)}");
            }
        }

        public void SendMissionStatusInfo(Client client)
        {
            ReconcilePlayerMissions(client);

            // A player can log in already wearing what an equip-bound objective
            // needs; the level-triggered check covers that without any new equip.
            Content.OnEquipCommitted(client);

            var missionStatus = new Dictionary<uint, MissionInfo>();

            foreach (var mission in client.Player.Missions.Values)
            {
                if (LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    missionStatus[mission.MissionId] = mission.ToMissionInfo(definition);
                else
                    Logger.WriteLog(LogType.Error, $"Character {client.Player.Id}: saved mission {mission.MissionId} has no definition and is not sent");
            }

            client.CallMethod(client.Player.EntityId, new MissionStatusInfoPacket(missionStatus));
        }

        #endregion

        #region NPC requests

        public void AssignNpcMission(Client client, ulong npcEntityId, uint missionId)
        {
            if (!TryResolveNpc(client, npcEntityId, out var creature))
                return;

            if (!LoadedMissions.TryGetValue(missionId, out var definition))
            {
                Logger.WriteLog(LogType.Debug, $"AssignNpcMission: unknown mission {missionId}");
                return;
            }

            var player = client.Player;

            if (!definition.IsDispensable || definition.MissionGiver != creature.DbId || !IsInConversationRange(player, creature))
                return;

            AcceptMission(client, definition);
        }

        /// <summary>
        /// Offers a mission over the radio ("Headquarters"); the offer stays pending for this session until accepted.
        /// </summary>
        public void DispenseRadioMission(Client client, uint missionId, bool forced)
        {
            if (!IsInWorld(client) || !LoadedMissions.TryGetValue(missionId, out var definition) || !definition.IsDispensable)
                return;

            var player = client.Player;

            if (player.Missions.ContainsKey(missionId))
                return;

            player.PendingRadioOffers.Add(missionId);
            client.CallMethod(player.EntityId, new DispenseRadioMissionPacket(missionId, definition, forced));
        }

        public void AssignRadioMission(Client client, uint missionId)
        {
            if (!IsInWorld(client))
                return;

            var player = client.Player;

            // Only a mission this session was offered over the radio can be accepted that way.
            if (!player.PendingRadioOffers.Contains(missionId) || !LoadedMissions.TryGetValue(missionId, out var definition) || !definition.IsDispensable)
                return;

            if (AcceptMission(client, definition))
                player.PendingRadioOffers.Remove(missionId);
        }

        private bool AcceptMission(Client client, Mission definition)
        {
            var player = client.Player;
            var missionId = definition.MissionId;

            // Active, or already completed: the log keeps completed missions so they are not re-offered.
            if (player.Missions.ContainsKey(missionId))
                return false;

            // The client counts the entries it was sent, i.e. missions with definitions.
            // It shows its own ID_MISSION_MAX_COUNT_REACHED warning; the server
            // refuses silently because the original reply is unrecovered.
            if (player.Missions.Values.Count(mission => mission.IsInLog && LoadedMissions.ContainsKey(mission.MissionId)) >= MissionRules.MaxMissionCount)
            {
                Logger.WriteLog(LogType.Debug, $"AcceptMission: mission log of character {player.Id} is full");
                return false;
            }

            var newMission = new PlayerMission { MissionId = missionId, State = MissionState.Active, ChangeTime = _now() };

            foreach (var objective in definition.Objectives.Values.Where(objective => objective.RevealedOnAccept == true))
                newMission.Objectives[objective.ObjectiveId] = MissionObjectiveState.Incomplete;

            var state = new ContentState(player);
            state.PlanMission(missionId, MissionState.Active);
            foreach (var objective in newMission.Objectives)
                state.PlanObjective(missionId, objective.Key, objective.Value);

            var reaction = Content.Plan(new ContentEvent(ContentRuleEvent.MissionAccepted, player.MapContextId, missionId), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                unitOfWork.CharacterMissions.Add(
                    new CharacterMissionEntry(player.Id, missionId, (uint)newMission.State, newMission.ChangeTime),
                    newMission.Objectives.Select(objective => new CharacterMissionObjectiveEntry(player.Id, missionId, objective.Key, (uint)objective.Value)));
                Content.Stage(reaction, unitOfWork, player, state);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"AcceptMission: could not save mission {missionId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }

            player.Missions[missionId] = newMission;
            client.CallMethod(player.EntityId, new MissionGainedPacket(missionId, newMission.ToMissionInfo(definition)));
            Content.Apply(client, reaction);
            Content.Present(client, reaction);
            RefreshRelatedNpcStatus(client, definition);
            return true;
        }

        public void CompleteNpcObjective(Client client, ulong npcEntityId, uint missionId, uint objectiveId, uint playerFlagId)
        {
            if (!TryResolveNpc(client, npcEntityId, out var creature))
                return;

            if (!LoadedMissions.TryGetValue(missionId, out var definition))
                return;

            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var mission) || mission.State != MissionState.Active)
                return;

            if (!mission.Objectives.TryGetValue(objectiveId, out var status) || status != MissionObjectiveState.Incomplete)
                return;

            if (!definition.HasObjectiveConversation(objectiveId, creature.Npc.NpcPackageId, playerFlagId))
                return;

            if (!IsInConversationRange(player, creature))
                return;

            CommitObjectiveProgress(client, definition, mission, objectiveId);
        }

        /// <summary>
        /// Completes an objective through one of its content bindings (area, use, kill...), once.
        /// </summary>
        public void CompleteBoundObjective(Client client, uint missionId, uint objectiveId, ObjectiveBindingKind kind)
        {
            if (!IsInWorld(client) || !LoadedMissions.TryGetValue(missionId, out var definition))
                return;

            if (!client.Player.Missions.TryGetValue(missionId, out var mission) || mission.State != MissionState.Active)
                return;

            if (!mission.Objectives.TryGetValue(objectiveId, out var status) || status != MissionObjectiveState.Incomplete)
                return;

            if (!definition.Bindings.Any(binding => binding.ObjectiveId == objectiveId && binding.Kind == (byte)kind))
                return;

            CommitObjectiveProgress(client, definition, mission, objectiveId);
        }

        /// <summary>
        /// Completes a validated incomplete objective: its reveals and the content reactions commit together,
        /// then memory and the client follow in the client's expected order.
        /// </summary>
        private void CommitObjectiveProgress(Client client, Mission definition, PlayerMission mission, uint objectiveId)
        {
            var player = client.Player;
            var missionId = definition.MissionId;
            var revealed = new List<uint>();

            if (definition.Transitions.TryGetValue(objectiveId, out var next))
                foreach (var revealedId in next)
                    if (!mission.Objectives.ContainsKey(revealedId) && !revealed.Contains(revealedId))
                        revealed.Add(revealedId);

            var changeTime = _now();
            var wasCompleteable = mission.IsCompleteable(definition);

            var state = new ContentState(player);
            state.PlanObjective(missionId, objectiveId, MissionObjectiveState.Completed);
            foreach (var revealedId in revealed)
                state.PlanObjective(missionId, revealedId, MissionObjectiveState.Incomplete);

            var reaction = Content.Plan(new ContentEvent(ContentRuleEvent.ObjectiveCompleted, player.MapContextId, missionId, objectiveId), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                unitOfWork.CharacterMissions.UpdateObjectiveStatus(player.Id, missionId, objectiveId, (uint)MissionObjectiveState.Completed);
                foreach (var revealedId in revealed)
                    unitOfWork.CharacterMissions.AddObjective(new CharacterMissionObjectiveEntry(player.Id, missionId, revealedId, (uint)MissionObjectiveState.Incomplete));
                unitOfWork.CharacterMissions.UpdateState(player.Id, missionId, (uint)mission.State, changeTime);
                Content.Stage(reaction, unitOfWork, player, state);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"CommitObjectiveProgress: could not save objective {missionId}/{objectiveId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            mission.Objectives[objectiveId] = MissionObjectiveState.Completed;
            foreach (var revealedId in revealed)
                mission.Objectives[revealedId] = MissionObjectiveState.Incomplete;
            mission.ChangeTime = changeTime;

            client.CallMethod(player.EntityId, new ObjectiveCompletedPacket(missionId, objectiveId));

            // Recv_MissionCompleteable acts only on a change, and ObjectiveRevealed
            // replaces the stored entry, so the change is announced before any reveal.
            if (!wasCompleteable && mission.IsCompleteable(definition))
                client.CallMethod(player.EntityId, new MissionCompleteablePacket((int)missionId, true));

            foreach (var revealedId in revealed)
                client.CallMethod(player.EntityId, new ObjectiveRevealedPacket(missionId, revealedId, mission.ToMissionInfo(definition)));

            // A just-revealed equip objective can already be satisfied by what the
            // player wears; the check is level-triggered and idempotent.
            Content.OnEquipCommitted(client);

            Content.Apply(client, reaction);
            Content.Present(client, reaction);
            RefreshRelatedNpcStatus(client, definition);
        }

        public void CompleteNpcMission(Client client, ulong npcEntityId, uint missionId, int? selectionIdx)
        {
            if (!TryResolveNpc(client, npcEntityId, out var creature))
                return;

            if (!LoadedMissions.TryGetValue(missionId, out var definition))
                return;

            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var mission) || !mission.IsCompleteable(definition))
                return;

            if (definition.MissionReciver != creature.DbId || !IsInConversationRange(player, creature))
                return;

            // A definition that would not be offered now (for example one changed
            // after acceptance) must not pay out either.
            var gaps = definition.DefinitionGaps();

            if (gaps.Count > 0)
            {
                Logger.WriteLog(LogType.Error, $"CompleteNpcMission: mission {missionId} definition incomplete ({string.Join("; ", gaps)}); turn-in refused for character {player.Id}");
                return;
            }

            // Complete definitions have no selectable rewards, so the client offers no
            // choice and sends None; anything else is not a request it could make.
            if (selectionIdx != null)
                return;

            if (definition.Rewards.Count == 0)
                Logger.WriteLog(LogType.Debug, $"Mission {missionId} has no npc_mission_reward rows; completed without rewards");

            var credits = definition.RewardCredits;
            var prestige = definition.RewardPrestige;
            var experience = player.Level >= ManifestationManager.MaxPlayerLevel ? 0 : definition.RewardExperience;
            var newCredits = (long)player.Credits[CurencyType.Credits] + credits;
            var newPrestige = (long)player.Credits[CurencyType.Prestige] + prestige;
            var newExperience = (long)player.Experience + experience;

            if (newCredits > int.MaxValue || newPrestige > int.MaxValue || newExperience > uint.MaxValue)
            {
                Logger.WriteLog(LogType.Error, $"CompleteNpcMission: rewards of mission {missionId} would overflow character {player.Id}'s balances; turn-in refused");
                return;
            }

            var changeTime = _now();

            var state = new ContentState(player);
            state.PlanMission(missionId, MissionState.Completed);
            var reaction = Content.Plan(new ContentEvent(ContentRuleEvent.MissionTurnedIn, player.MapContextId, missionId), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                // Mission state and currency/experience commit together so a crash
                // can neither pay twice nor consume the mission unpaid.
                unitOfWork.CharacterMissions.UpdateState(player.Id, missionId, (uint)MissionState.Completed, changeTime);
                if (credits != 0 || prestige != 0 || experience != 0)
                    unitOfWork.Characters.UpdateCharacterRewards(player.Id, (int)newCredits, (int)newPrestige, (uint)newExperience);
                Content.Stage(reaction, unitOfWork, player, state);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"CompleteNpcMission: could not save completion of mission {missionId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            mission.State = MissionState.Completed;
            mission.ChangeTime = changeTime;

            if (credits != 0)
            {
                player.Credits[CurencyType.Credits] = (int)newCredits;
                client.CallMethod(player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, (int)newCredits, 0));
            }

            if (prestige != 0)
            {
                player.Credits[CurencyType.Prestige] = (int)newPrestige;
                client.CallMethod(player.EntityId, new UpdateCreditsPacket(CurencyType.Prestige, (int)newPrestige, 0));
            }

            if (experience != 0)
            {
                player.Experience = (uint)newExperience;
                ManifestationManager.Instance.NotifyExperienceGained(client, (uint)experience);
            }

            client.CallMethod(player.EntityId, new MissionCompletedPacket(missionId));
            client.CallMethod(player.EntityId, new MissionRewardedPacket(missionId));

            Content.Apply(client, reaction);
            Content.Present(client, reaction);
            RefreshRelatedNpcStatus(client, definition);
        }

        public void AbandonMission(Client client, uint missionId)
        {
            if (!IsInWorld(client))
                return;

            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var mission) || !mission.IsInLog)
                return;

            // The original client disables Abandon for these missions; refuse the request too.
            if (MissionRules.NonAbandonableMissions.Contains(missionId))
                return;

            var state = new ContentState(player);
            state.PlanMission(missionId, null);
            var reaction = Content.Plan(new ContentEvent(ContentRuleEvent.MissionAbandoned, player.MapContextId, missionId), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                unitOfWork.CharacterMissions.Delete(player.Id, missionId);
                Content.Stage(reaction, unitOfWork, player, state);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"AbandonMission: could not remove mission {missionId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            player.Missions.Remove(missionId);
            client.CallMethod(player.EntityId, new MissionDiscardedPacket(missionId));
            Content.Apply(client, reaction);
            Content.Present(client, reaction);

            if (LoadedMissions.TryGetValue(missionId, out var definition))
                RefreshRelatedNpcStatus(client, definition);
        }

        #endregion

        #region Conversation

        /// <summary>
        /// Adds this player's mission topics for the NPC to a Converse dictionary.
        /// </summary>
        public void AddMissionConversation(Client client, Creature creature, Dictionary<ConversationType, object> convoDataDict)
        {
            var dispensable = new Dictionary<uint, MissionInfo>();
            var completeable = new Dictionary<uint, RewardInfo>();
            var objectives = new List<CompleteableObjectives>();

            foreach (var definition in LoadedMissions.Values)
            {
                client.Player.Missions.TryGetValue(definition.MissionId, out var progress);

                if (progress == null)
                {
                    if (definition.MissionGiver == creature.DbId && definition.IsDispensable)
                        dispensable.Add(definition.MissionId, definition);

                    continue;
                }

                if (progress.State != MissionState.Active)
                    continue;

                if (definition.MissionReciver == creature.DbId && progress.IsCompleteable(definition))
                    completeable.Add(definition.MissionId, definition.MissionConstantData.RewardInfo);

                foreach (var conversation in definition.ObjectiveConversations)
                    if (conversation.NpcPackageId == creature.Npc.NpcPackageId &&
                        progress.Objectives.TryGetValue(conversation.ObjectiveId, out var status) &&
                        status == MissionObjectiveState.Incomplete)
                        objectives.Add(new CompleteableObjectives((int)definition.MissionId, (int)conversation.ObjectiveId, (int)conversation.PlayerFlagId));
            }

            if (dispensable.Count > 0)
                convoDataDict.Add(ConversationType.MissionDispense, dispensable);

            if (completeable.Count > 0)
                convoDataDict.Add(ConversationType.MissionComplete, completeable);

            if (objectives.Count > 0)
                convoDataDict.Add(ConversationType.ObjectiveComplete, objectives);
        }

        /// <summary>
        /// Mission-related NPCConversationStatus for this player, if any.
        /// </summary>
        public bool TryGetConversationStatus(Client client, Creature creature, out ConversationStatus status, out List<uint> data)
        {
            var complete = new List<uint>();
            var objective = new List<uint>();
            var available = new List<uint>();

            foreach (var definition in LoadedMissions.Values)
            {
                client.Player.Missions.TryGetValue(definition.MissionId, out var progress);

                if (progress == null)
                {
                    if (definition.MissionGiver == creature.DbId && definition.IsDispensable)
                        available.Add(definition.MissionId);

                    continue;
                }

                if (progress.State != MissionState.Active)
                    continue;

                if (definition.MissionReciver == creature.DbId && progress.IsCompleteable(definition))
                    complete.Add(definition.MissionId);

                if (definition.ObjectiveConversations.Any(conversation =>
                        conversation.NpcPackageId == creature.Npc.NpcPackageId &&
                        progress.Objectives.TryGetValue(conversation.ObjectiveId, out var objectiveStatus) &&
                        objectiveStatus == MissionObjectiveState.Incomplete))
                    objective.Add(definition.MissionId);
            }

            if (complete.Count > 0)
            {
                status = ConversationStatus.MissionComplete;
                data = complete;
                return true;
            }

            if (objective.Count > 0)
            {
                status = ConversationStatus.ObjectivComplete;
                data = objective;
                return true;
            }

            if (available.Count > 0)
            {
                status = ConversationStatus.Available;
                data = available;
                return true;
            }

            status = ConversationStatus.None;
            data = null;
            return false;
        }

        private void RefreshRelatedNpcStatus(Client client, Mission definition)
        {
            var refresher = ConversationStatusRefresher;
            var mapChannel = client.Player?.MapChannel;

            if (refresher == null || mapChannel == null || client.Player.Cells == null)
                return;

            var visited = new HashSet<Creature>();

            foreach (var cellSeed in client.Player.Cells)
            {
                if (!mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    continue;

                foreach (var creature in cell.CreatureList.ToList())
                    if (creature.Npc != null && visited.Add(creature) && definition.IsRelatedTo(creature))
                        refresher(client, creature);
            }
        }

        #endregion

        public static bool IsInConversationRange(Manifestation player, Creature creature)
        {
            return creature.MapContextId == player.MapContextId &&
                   Vector3.Distance(player.Position, creature.Position) <= MissionRules.ConversationRange + MissionRules.ConversationRangeTolerance;
        }

        /// <summary>
        /// The player is in a map: in game, or arriving by dropship after
        /// MapLoaded placed them in the destination map (the state stays
        /// Teleporting until the arrival sequence ends). A departing player whose
        /// destination is not yet loaded is excluded.
        /// </summary>
        public static bool IsInWorld(Client client)
        {
            var player = client.Player;

            // Logout to selection and socket loss change the client state, so the
            // state alone decides; Disconected is also left set by the GM teleport.
            if (player == null)
                return false;

            if (client.State == ClientState.Ingame)
                return true;

            return client.State == ClientState.Teleporting &&
                   player.MapContextId == client.LoadingMap &&
                   player.MapChannel?.ClientList != null &&
                   player.MapChannel.ClientList.Contains(client);
        }

        private static bool TryResolveNpc(Client client, ulong npcEntityId, out Creature creature)
        {
            creature = null;

            if (!IsInWorld(client))
                return false;

            return EntityManager.Instance.Creatures.TryGetValue(npcEntityId, out creature) && creature.Npc != null;
        }
    }
}
