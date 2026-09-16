using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Inventory.Server;
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

        // The content runtime stages counter writes through the mission manager's factory
        // so tests can substitute it (the factory is also the transaction owner for the
        // mission-log writes it accompanies).
        public IGameUnitOfWorkFactory GameUnitOfWorkFactoryForContent => _gameUnitOfWorkFactory;
        private readonly Func<uint> _now;

        public readonly Dictionary<uint, Mission> LoadedMissions = new Dictionary<uint, Mission>();

        // Unix time in milliseconds for objective timers (wall clock, OD-5); replaceable by tests.
        public Func<long> NowMs { get; set; } = () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

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
                        // Delivered at turn-in in the completion transaction (CompleteNpcMission).
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
                {
                    mission.Objectives[objective.ObjectiveId] = (MissionObjectiveState)objective.Status;

                    if (objective.TimerRemainingMs is { } remainingMs && objective.TimerAnchorMs is { } anchorMs)
                        mission.Timers[objective.ObjectiveId] = new ObjectiveTimer { RemainingMs = remainingMs, AnchorMs = anchorMs, Disarmed = objective.TimerDisarmed };
                }
                else
                    Logger.WriteLog(LogType.Error, $"Character {characterId}: objective row {objective.MissionId}/{objective.ObjectiveId} has no mission row");
            }

            foreach (var counter in repository.GetCounters(characterId))
            {
                if (missions.TryGetValue(counter.MissionId, out var mission))
                    mission.Counters[(counter.ObjectiveId, counter.CounterId)] = counter.Value;
                else
                    Logger.WriteLog(LogType.Error, $"Character {characterId}: counter row {counter.MissionId}/{counter.ObjectiveId}/{counter.CounterId} has no mission row");
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

                var nowMs = NowMs();
                var timers = StartTimers(definition, added, nowMs);

                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                    foreach (var objectiveId in added)
                        unitOfWork.CharacterMissions.AddObjective(ObjectiveRow(player.Id, mission.MissionId, objectiveId, timers));
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
                foreach (var (objectiveId, timer) in timers)
                    mission.Timers[objectiveId] = timer;

                // Nearby NPCs were introduced with the pre-reconciliation log.
                RefreshRelatedNpcStatus(client, definition);

                Logger.WriteLog(LogType.Initialize, $"Character {player.Id}: mission {mission.MissionId} definition changed, revealed objectives {string.Join(", ", added)}");
            }
        }

        public void SendMissionStatusInfo(Client client)
        {
            ReconcilePlayerMissions(client);

            // Wall-clock timers keep running while the character is offline (OD-5); one that ran
            // out meanwhile fails before the log is sent, so the client never receives an already
            // expired running timer. The client has no log to update yet, so nothing is announced.
            ExpireObjectiveTimers(client, false);

            // A player can log in already wearing what an equip-bound objective
            // needs; the level-triggered check covers that without any new equip.
            Content.OnEquipCommitted(client);

            var missionStatus = new Dictionary<uint, MissionInfo>();
            var nowMs = NowMs();

            foreach (var mission in client.Player.Missions.Values)
            {
                // Recv_MissionFailed pops a failed mission from the client's log, so a later log
                // does not bring it back; the stored row only gates retries.
                if (mission.State == MissionState.Failded)
                    continue;

                if (LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    missionStatus[mission.MissionId] = mission.ToMissionInfo(definition, nowMs);
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

            if (!definition.IsDispensable || definition.MissionGiver != creature.DbId || !IsInConversationRange(player, creature) ||
                !PrerequisitesSatisfied(player, definition))
            {
                Logger.WriteLog(LogType.Debug, $"AssignNpcMission: mission {missionId} refused: dispensable={definition.IsDispensable} giverMatch={definition.MissionGiver == creature.DbId} inRange={IsInConversationRange(player, creature)} prerequisites={PrerequisitesSatisfied(player, definition)}");
                return;
            }

            AcceptMission(client, definition);
        }

        /// <summary>
        /// A creature died. Completes the kill-bound objectives whose creature_id matches
        /// (killer-only credit in shared contexts; the content runtime handles per-character
        /// placement credit and counters).
        /// </summary>
        public void OnCreatureKilled(Client killer, Creature creature)
        {
            if (!IsInWorld(killer))
                return;

            foreach (var definition in LoadedMissions.Values)
            {
                foreach (var binding in definition.Bindings)
                {
                    if ((ObjectiveBindingKind)binding.Kind != ObjectiveBindingKind.Kill || binding.CreatureId != creature.DbId)
                        continue;

                    if (!killer.Player.Missions.TryGetValue(definition.MissionId, out var mission) ||
                        mission.State != MissionState.Active ||
                        !mission.Objectives.TryGetValue(binding.ObjectiveId, out var status) ||
                        status != MissionObjectiveState.Incomplete)
                        continue;

                    Content.OnKillBinding(killer, definition.MissionId, binding.ObjectiveId, ObjectiveBindingKind.Kill);
                }
            }
        }

        /// <summary>
        /// True when this character's mission log satisfies the definition's prerequisite
        /// or-groups: at least one group whose required missions are all in the required state.
        /// A definition without prerequisites is always satisfied.
        /// </summary>
        public bool PrerequisitesSatisfied(Manifestation player, Mission definition)
        {
            if (definition.Prerequisites.Count == 0)
                return true;

            return definition.Prerequisites.GroupBy(prerequisite => prerequisite.OrGroup).Any(group => group.All(prerequisite => Holds(player, prerequisite)));
        }

        // NotAssigned means the character has no row for the required mission at all.
        private static bool Holds(Manifestation player, NpcMissionPrerequisiteEntry prerequisite) =>
            player.Missions.TryGetValue(prerequisite.RequiredMissionId, out var progress)
                ? (uint)progress.State == prerequisite.RequiredState
                : prerequisite.RequiredState == (uint)MissionState.NotAssigned;

        /// <summary>
        /// A failed mission may be taken again only when a satisfied prerequisite group names the mission
        /// itself as failed ("I'll give you another shot", the 2005 retry of build plan S5); the new
        /// acceptance replaces the failed row.
        /// </summary>
        public bool RetryAllowed(Manifestation player, Mission definition) =>
            player.Missions.TryGetValue(definition.MissionId, out var progress) && progress.State == MissionState.Failded &&
            definition.Prerequisites.GroupBy(prerequisite => prerequisite.OrGroup).Any(group =>
                group.Any(prerequisite => prerequisite.RequiredMissionId == definition.MissionId && prerequisite.RequiredState == (uint)MissionState.Failded) &&
                group.All(prerequisite => Holds(player, prerequisite)));

        /// <summary>
        /// True while the character's row for the mission keeps it from being offered or accepted:
        /// active, completed, or failed without an allowed retry.
        /// </summary>
        public bool HasBlockingProgress(Manifestation player, Mission definition) =>
            player.Missions.ContainsKey(definition.MissionId) && !RetryAllowed(player, definition);

        /// <summary>
        /// Offers a mission over the radio ("Headquarters"); the offer stays pending for this session until accepted.
        /// </summary>
        public void DispenseRadioMission(Client client, uint missionId, bool forced)
        {
            if (!IsInWorld(client) || !LoadedMissions.TryGetValue(missionId, out var definition) || !definition.IsDispensable)
                return;

            var player = client.Player;

            if (HasBlockingProgress(player, definition))
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
            // A failed mission is replaced only when its prerequisites allow the retry.
            if (HasBlockingProgress(player, definition))
                return false;

            var replacesFailed = player.Missions.ContainsKey(missionId);

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

            var nowMs = NowMs();
            var timers = StartTimers(definition, newMission.Objectives.Keys, nowMs);

            var state = new ContentState(player);
            state.PlanMission(missionId, MissionState.Active);
            foreach (var objective in newMission.Objectives)
                state.PlanObjective(missionId, objective.Key, objective.Value);

            var reaction = Content.Plan(new ContentEvent(ContentRuleEvent.MissionAccepted, player.MapContextId, missionId), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                if (replacesFailed)
                    unitOfWork.CharacterMissions.Delete(player.Id, missionId);

                unitOfWork.CharacterMissions.Add(
                    new CharacterMissionEntry(player.Id, missionId, (uint)newMission.State, newMission.ChangeTime),
                    newMission.Objectives.Keys.Select(objectiveId => ObjectiveRow(player.Id, missionId, objectiveId, timers)));
                Content.Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"AcceptMission: could not save mission {missionId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }

            foreach (var (objectiveId, timer) in timers)
                newMission.Timers[objectiveId] = timer;

            player.Missions[missionId] = newMission;
            client.CallMethod(player.EntityId, new MissionGainedPacket(missionId, newMission.ToMissionInfo(definition, nowMs)));
            Content.Apply(client, reaction);
            Content.Present(client, reaction);
            RefreshRelatedNpcStatus(client, definition);
            return true;
        }

        /// <summary>
        /// Shares one of the player's active missions with the party. Only a definition whose shareable flag is set can
        /// be shared, and only a character who does not already have it receives one.
        ///
        /// The original's own step order is not recoverable from the sources (GAP-W3-SHARING-OFFER): missionlog.pyo
        /// has the accept and decline requests naming the sharer, but no surviving server data says whether the mission
        /// was put in the target's log at share time or only on accept. This puts it in the log at share time, which is
        /// what makes accept and decline both mean something - accept clears the share, decline drops the mission.
        /// </summary>
        public void ShareMission(Client client, uint missionId)
        {
            if (!IsInWorld(client))
                return;

            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var mission) || mission.State != MissionState.Active)
                return;

            if (!LoadedMissions.TryGetValue(missionId, out var definition) || !definition.MissionConstantData.Shareable)
                return;

            // Party members are found through the party manager: the manifestation carries only its party id.
            if (!PartyManager.Instance.Parties.TryGetValue(player.PartyId, out var party) || party == null)
                return;

            var channel = player.MapChannel;
            if (channel == null)
                return;

            foreach (var member in party.Members.ToList())
            {
                var target = channel.ClientList.FirstOrDefault(other => other?.Player != null && other.Player.Id == member.UserId);
                if (target == null || target.Player.Id == player.Id)
                    continue;

                // Only a character who does not already have the mission, and whose log has room, is offered it.
                if (HasBlockingProgress(target.Player, definition))
                    continue;

                if (!AcceptMission(target, definition))
                    continue;

                target.Player.PendingSharedMissions[missionId] = player.Id;
                Logger.WriteLog(LogType.Debug, $"ShareMission: character {player.Id} shared mission {missionId} with character {member.UserId}");
            }
        }

        /// <summary>
        /// Answers a share: the mission stays, the pending entry goes. The request names the sharing player, so a
        /// player can only clear a share they actually hold from them.
        /// </summary>
        public void AssignSharedMission(Client client, uint sharerId, uint missionId)
        {
            if (!IsInWorld(client))
                return;

            var player = client.Player;

            if (!player.PendingSharedMissions.TryGetValue(missionId, out var from) || from != sharerId)
                return;

            player.PendingSharedMissions.Remove(missionId);
        }

        /// <summary>
        /// Declines a share: the mission leaves the log, exactly as abandoning it would (the client's own decline
        /// removes the shared mission from the log, and there is nothing else the server could do with it).
        /// </summary>
        public void DeclineSharedMission(Client client, uint sharerId, uint missionId)
        {
            if (!IsInWorld(client))
                return;

            var player = client.Player;

            if (!player.PendingSharedMissions.TryGetValue(missionId, out var from) || from != sharerId)
                return;

            player.PendingSharedMissions.Remove(missionId);
            AbandonMission(client, missionId);
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
            {
                return;
            }

            if (!client.Player.Missions.TryGetValue(missionId, out var mission) || mission.State != MissionState.Active)
            {
                return;
            }

            if (!mission.Objectives.TryGetValue(objectiveId, out var status) || status != MissionObjectiveState.Incomplete)
            {
                return;
            }

            if (!definition.Bindings.Any(binding => binding.ObjectiveId == objectiveId && binding.Kind == (byte)kind))
            {
                return;
            }

            CommitObjectiveProgress(client, definition, mission, objectiveId);
        }

        /// <summary>
        /// A content placement entered a state (the bomb detonating): the first objective bound to that state
        /// completes in the same transaction as the placement_state_entered rules, so the objective and the facts
        /// the rules set (the destroyed dropship) can never commit apart. Without a bound objective only the rules run.
        /// </summary>
        public void CommitPlacementState(Client client, uint placementId, uint stateId)
        {
            var player = client.Player;
            var trigger = new ContentEvent(ContentRuleEvent.PlacementStateEntered, player.MapContextId, placementId: placementId, stateId: stateId);
            var bound = new List<(Mission Definition, PlayerMission Mission, uint ObjectiveId)>();

            foreach (var mission in player.Missions.Values)
            {
                if (mission.State != MissionState.Active || !LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    continue;

                foreach (var binding in definition.Bindings)
                    if ((ObjectiveBindingKind)binding.Kind == ObjectiveBindingKind.PlacementState && binding.PlacementId == placementId &&
                        binding.TargetState == stateId &&
                        mission.Objectives.TryGetValue(binding.ObjectiveId, out var status) && status == MissionObjectiveState.Incomplete)
                        bound.Add((definition, mission, binding.ObjectiveId));
            }

            if (bound.Count == 0 || !IsInWorld(client))
            {
                Content.React(client, trigger);
                return;
            }

            var (firstDefinition, firstMission, firstObjective) = bound[0];
            CommitObjectiveProgress(client, firstDefinition, firstMission, firstObjective, trigger);

            foreach (var (definition, mission, objectiveId) in bound.Skip(1))
                CompleteBoundObjective(client, definition.MissionId, objectiveId, ObjectiveBindingKind.PlacementState);
        }

        /// <summary>
        /// Completes a validated incomplete objective: its reveals and the content reactions commit together,
        /// then memory and the client follow in the client's expected order.
        /// </summary>
        private void CommitObjectiveProgress(Client client, Mission definition, PlayerMission mission, uint objectiveId, ContentEvent? trigger = null)
        {
            var player = client.Player;
            var missionId = definition.MissionId;
            var revealed = new List<uint>();
            var nowMs = NowMs();

            // Out of time: the expiry step of this tick fails the objective instead.
            if (mission.Timers.TryGetValue(objectiveId, out var runningTimer) && runningTimer.HasExpired(nowMs))
                return;

            if (definition.Transitions.TryGetValue(objectiveId, out var next))
                foreach (var revealedId in next)
                    if (!mission.Objectives.ContainsKey(revealedId) && !revealed.Contains(revealedId))
                        revealed.Add(revealedId);

            var changeTime = _now();
            var wasCompleteable = mission.IsCompleteable(definition);
            var timers = StartTimers(definition, revealed, nowMs);

            var state = new ContentState(player);
            state.PlanObjective(missionId, objectiveId, MissionObjectiveState.Completed);
            foreach (var revealedId in revealed)
                state.PlanObjective(missionId, revealedId, MissionObjectiveState.Incomplete);

            // The triggering event's rules (placement_state_entered) come first and see the objective completing.
            var triggerReaction = trigger is { } triggerEvent ? Content.Plan(triggerEvent, state) : null;
            ContentReaction reaction;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                if (triggerReaction != null)
                    Content.Stage(triggerReaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);

                // Planned after the trigger's actions are staged, so its conditions see their fact changes.
                reaction = Content.Plan(new ContentEvent(ContentRuleEvent.ObjectiveCompleted, player.MapContextId, missionId, objectiveId), state);

                unitOfWork.CharacterMissions.UpdateObjectiveStatus(player.Id, missionId, objectiveId, (uint)MissionObjectiveState.Completed);
                if (runningTimer != null)
                    unitOfWork.CharacterMissions.SetObjectiveTimer(player.Id, missionId, objectiveId, null, null, false);
                foreach (var revealedId in revealed)
                    unitOfWork.CharacterMissions.AddObjective(ObjectiveRow(player.Id, missionId, revealedId, timers));
                unitOfWork.CharacterMissions.UpdateState(player.Id, missionId, (uint)mission.State, changeTime);
                Content.Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"CommitObjectiveProgress: could not save objective {missionId}/{objectiveId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            mission.Objectives[objectiveId] = MissionObjectiveState.Completed;
            mission.Timers.Remove(objectiveId);
            foreach (var revealedId in revealed)
                mission.Objectives[revealedId] = MissionObjectiveState.Incomplete;
            foreach (var (revealedId, timer) in timers)
                mission.Timers[revealedId] = timer;
            mission.ChangeTime = changeTime;

            client.CallMethod(player.EntityId, new ObjectiveCompletedPacket(missionId, objectiveId));

            // Recv_MissionCompleteable acts only on a change, and ObjectiveRevealed
            // replaces the stored entry, so the change is announced before any reveal.
            if (!wasCompleteable && mission.IsCompleteable(definition))
                client.CallMethod(player.EntityId, new MissionCompleteablePacket((int)missionId, true));

            foreach (var revealedId in revealed)
                client.CallMethod(player.EntityId, new ObjectiveRevealedPacket(missionId, revealedId, mission.ToMissionInfo(definition, nowMs)));

            // A just-revealed equip objective can already be satisfied by what the
            // player wears; the check is level-triggered and idempotent.
            Content.OnEquipCommitted(client);

            if (triggerReaction != null)
            {
                Content.Apply(client, triggerReaction);
                Content.Present(client, triggerReaction);
            }

            Content.Apply(client, reaction);
            Content.Present(client, reaction);
            RefreshRelatedNpcStatus(client, definition);
        }

        /// <summary>
        /// Turns a mission in at its receiver: the NPC has to be the definition's receiver and the player has to be
        /// standing with them. Everything after that - the reward choice, the balances, the item slots, the mission
        /// state and the content reaction - is PayOut, which the radio turn-in uses as well.
        /// </summary>
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

            PayOut(client, definition, mission, selectionIdx, "CompleteNpcMission");
        }

        /// <summary>
        /// Turns a mission in over the radio. The client's mission log offers that for a definition whose
        /// radio_completeable flag is set, and the request carries the same (missionId, selectionIdx, rating) tuple the
        /// NPC turn-in does - without the NPC, which is the whole point of it: the receiver and the conversation range
        /// the NPC path insists on are exactly what this path must not require.
        ///
        /// RewardRadioMission carries the identical tuple, so it is handled here too: which of the two the client sends
        /// to complete and which to collect the reward is not established (GAP-W3-RADIO-REWARD-STEP). Treating both as
        /// "complete and pay" is the interpretation that cannot double-pay, because a completed mission is no longer
        /// completeable.
        /// </summary>
        public void CompleteRadioMission(Client client, uint missionId, int? selectionIdx)
        {
            if (!IsInWorld(client))
                return;

            if (!LoadedMissions.TryGetValue(missionId, out var definition))
                return;

            var player = client.Player;

            if (!player.Missions.TryGetValue(missionId, out var mission) || !mission.IsCompleteable(definition))
                return;

            // Only a definition the client was told is radio completeable may be turned in this way.
            if (!definition.MissionConstantData.RadioCompletable)
                return;

            var gaps = definition.DefinitionGaps();

            if (gaps.Count > 0)
            {
                Logger.WriteLog(LogType.Error, $"CompleteRadioMission: mission {missionId} definition incomplete ({string.Join("; ", gaps)}); turn-in refused for character {player.Id}");
                return;
            }

            PayOut(client, definition, mission, selectionIdx, "CompleteRadioMission");
        }

        /// <summary>
        /// Everything a turn-in does once the request itself has been accepted: the reward choice, the balances with
        /// their overflow guard, the item slots, the mission state, the content reaction and the client's
        /// completed/rewarded notifications. The NPC turn-in and the radio turn-in differ only in how they validate the
        /// request, so they share this.
        /// </summary>
        private void PayOut(Client client, Mission definition, PlayerMission mission, int? selectionIdx, string caller)
        {
            var player = client.Player;
            var missionId = definition.MissionId;

            // The client sends the index of the chosen selectable reward, and None when the
            // mission offers no choice; anything else is not a request it could make.
            var selectable = definition.OfferedSelectableRewards;

            if (selectable.Count == 0 ? selectionIdx != null : selectionIdx is not { } chosen || chosen < 0 || chosen >= selectable.Count)
                return;

            var itemRewards = definition.OfferedFixedItems.ToList();
            if (selectable.Count > 0)
                itemRewards.Add(selectable[selectionIdx.Value]);

            // Every item needs its own free slot in its inventory category before anything commits:
            // a full category refuses the turn-in rather than consuming the mission without the item.
            var placements = PlanRewardSlots(player, itemRewards);

            if (placements == null)
            {
                Logger.WriteLog(LogType.Debug, $"{caller}: no room for the item rewards of mission {missionId}; turn-in refused for character {player.Id}");
                return;
            }

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
                Logger.WriteLog(LogType.Error, $"{caller}: rewards of mission {missionId} would overflow character {player.Id}'s balances; turn-in refused");
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
                using var transaction = placements.Count > 0 ? unitOfWork.BeginTransaction() : null;

                // Item rows get their ids inside the transaction; nothing is visible unless all of it commits.
                foreach (var placement in placements)
                {
                    placement.Item.Id = unitOfWork.Items.CreateItem(placement.Item);
                    if (placement.Item.Id == 0)
                        throw new InvalidOperationException($"reward item {placement.Item.ItemTemplateId} could not be saved");
                    unitOfWork.CharacterInventories.StageInvItem(client.AccountEntry?.Id ?? 0, player.Id, (uint)InventoryType.Personal, placement.Slot, placement.Item.Id);
                }

                unitOfWork.CharacterMissions.UpdateState(player.Id, missionId, (uint)MissionState.Completed, changeTime);
                if (credits != 0 || prestige != 0 || experience != 0)
                    unitOfWork.Characters.UpdateCharacterRewards(player.Id, (int)newCredits, (int)newPrestige, (uint)newExperience);
                Content.Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                unitOfWork.Complete();
                transaction?.Commit();
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

            foreach (var placement in placements)
            {
                var item = placement.Item;
                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);
                item.OwnerId = player.Id;
                item.OwnerSlotId = placement.Slot;
                ItemManager.Instance.SendItemDataToClient(client, item, false);
                player.Inventory.PersonalInventory[(int)placement.Slot] = item.EntityId;
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(InventoryType.Personal, item.EntityId, placement.Slot));
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
                Content.Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
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

        #region Item rewards

        private sealed class RewardPlacement
        {
            public Item Item;
            public uint Slot;
        }

        /// <summary>
        /// A free personal-inventory slot in each reward's category (0-49 equipment ... 200-249 misc), lowest first,
        /// with the item built but not yet saved; null when any reward does not fit.
        /// </summary>
        private static List<RewardPlacement> PlanRewardSlots(Manifestation player, IReadOnlyList<NpcMissionRewardEntry> rewards)
        {
            var placements = new List<RewardPlacement>();
            var taken = new HashSet<uint>();
            var inventory = player.Inventory.PersonalInventory;

            foreach (var reward in rewards)
            {
                var template = ItemManager.Instance.GetItemTemplateById(reward.ItemTemplateId);
                var classInfo = template == null ? null : EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo;

                if (classInfo == null)
                    return null;

                var offset = ((uint)template.InventoryCategory - 1) * 50;
                uint? slot = null;

                for (var candidate = offset; candidate < offset + 50 && candidate < inventory.Count; candidate++)
                    if (inventory[(int)candidate] == 0 && taken.Add(candidate))
                    {
                        slot = candidate;
                        break;
                    }

                if (slot == null)
                    return null;

                placements.Add(new RewardPlacement
                {
                    Slot = slot.Value,
                    Item = new Item
                    {
                        ItemTemplate = template,
                        ItemTemplateId = template.ItemTemplateId,
                        StackSize = reward.Quantity,
                        CurrentHitPoints = classInfo.MaxHitPoints,
                        Crafter = "",
                        Color = 2139062144
                    }
                });
            }

            return placements;
        }

        #endregion

        #region Objective timers

        /// <summary>The running timers of newly revealed objectives that have a timer row, anchored now.</summary>
        private static Dictionary<uint, ObjectiveTimer> StartTimers(Mission definition, IEnumerable<uint> objectiveIds, long nowMs)
        {
            var timers = new Dictionary<uint, ObjectiveTimer>();

            foreach (var objectiveId in objectiveIds)
                if (definition.Timers.TryGetValue(objectiveId, out var row))
                    timers[objectiveId] = new ObjectiveTimer { RemainingMs = row.LimitSeconds * 1000L, AnchorMs = nowMs };

            return timers;
        }

        private static CharacterMissionObjectiveEntry ObjectiveRow(uint characterId, uint missionId, uint objectiveId, IReadOnlyDictionary<uint, ObjectiveTimer> timers)
        {
            var row = new CharacterMissionObjectiveEntry(characterId, missionId, objectiveId, (uint)MissionObjectiveState.Incomplete);

            if (timers.TryGetValue(objectiveId, out var timer))
            {
                row.TimerRemainingMs = timer.RemainingMs;
                row.TimerAnchorMs = timer.AnchorMs;
                row.TimerDisarmed = timer.Disarmed;
            }

            return row;
        }

        /// <summary>
        /// The expiry step of the map tick: fails the timed objectives of this channel's players whose
        /// deadline has passed. It runs after the tick's use recoveries, so an action that completes or
        /// disarms by the deadline tick wins.
        /// </summary>
        public void ExpireObjectiveTimers(MapChannel mapChannel)
        {
            foreach (var client in mapChannel.ClientList.ToList())
                if (client?.Player != null && IsInWorld(client))
                    ExpireObjectiveTimers(client, true);
        }

        /// <summary>
        /// Fails every expired, armed timer of an active mission, earliest deadline first. A timer whose
        /// definition row is gone is left alone rather than failing an objective content no longer times.
        /// </summary>
        public void ExpireObjectiveTimers(Client client, bool announce)
        {
            var player = client.Player;
            var nowMs = NowMs();

            foreach (var mission in player.Missions.Values.Where(mission => mission.Timers.Count > 0).ToList())
            {
                if (!LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    continue;

                foreach (var (objectiveId, timer) in mission.Timers.OrderBy(entry => entry.Value.DeadlineMs).ThenBy(entry => entry.Key).ToList())
                {
                    if (mission.State != MissionState.Active)
                        break;

                    if (!timer.HasExpired(nowMs) || !definition.Timers.TryGetValue(objectiveId, out var row) ||
                        !mission.Objectives.TryGetValue(objectiveId, out var status) || status != MissionObjectiveState.Incomplete)
                        continue;

                    FailTimedObjective(client, definition, mission, objectiveId, (ObjectiveTimerExpiry)row.OnExpire, announce);
                }
            }
        }

        /// <summary>
        /// Timer expiry (build plan 1.6): the objective fails, and with on_expire 2 the mission too; the
        /// timer is cleared and the objective_failed then mission_failed rules commit in the same unit of
        /// work. The client hears ObjectiveFailed before MissionFailed, which drops the mission from its log.
        /// </summary>
        private void FailTimedObjective(Client client, Mission definition, PlayerMission mission, uint objectiveId, ObjectiveTimerExpiry expiry, bool announce)
        {
            var player = client.Player;
            var missionId = definition.MissionId;
            var failsMission = expiry == ObjectiveTimerExpiry.FailObjectiveAndMission;
            var newState = failsMission ? MissionState.Failded : mission.State;
            var changeTime = _now();

            var state = new ContentState(player);
            state.PlanObjective(missionId, objectiveId, MissionObjectiveState.Failed);
            if (failsMission)
                state.PlanMission(missionId, MissionState.Failded);

            var objectiveReaction = Content.Plan(new ContentEvent(ContentRuleEvent.ObjectiveFailed, player.MapContextId, missionId, objectiveId), state);
            ContentReaction missionReaction = null;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                unitOfWork.CharacterMissions.UpdateObjectiveStatus(player.Id, missionId, objectiveId, (uint)MissionObjectiveState.Failed);
                unitOfWork.CharacterMissions.SetObjectiveTimer(player.Id, missionId, objectiveId, null, null, false);
                unitOfWork.CharacterMissions.UpdateState(player.Id, missionId, (uint)newState, changeTime);
                Content.Stage(objectiveReaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);

                if (failsMission)
                {
                    // Planned after the objective's reactions are staged, so its conditions see their fact changes.
                    missionReaction = Content.Plan(new ContentEvent(ContentRuleEvent.MissionFailed, player.MapContextId, missionId), state);
                    Content.Stage(missionReaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                }

                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"FailTimedObjective: could not save the expiry of {missionId}/{objectiveId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            mission.Objectives[objectiveId] = MissionObjectiveState.Failed;
            mission.Timers.Remove(objectiveId);
            mission.State = newState;
            mission.ChangeTime = changeTime;

            if (announce)
            {
                client.CallMethod(player.EntityId, new ObjectiveFailedPacket(missionId, objectiveId));

                if (failsMission)
                    client.CallMethod(player.EntityId, new MissionFailedPacket(missionId));
            }

            Content.Apply(client, objectiveReaction);
            Content.Present(client, objectiveReaction);

            if (missionReaction != null)
            {
                Content.Apply(client, missionReaction);
                Content.Present(client, missionReaction);
            }

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

                if (progress == null || progress.State == MissionState.Failded)
                {
                    if (definition.MissionGiver == creature.DbId && definition.IsDispensable &&
                        !HasBlockingProgress(client.Player, definition) && PrerequisitesSatisfied(client.Player, definition))
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

                if (progress == null || progress.State == MissionState.Failded)
                {
                    if (definition.MissionGiver == creature.DbId && definition.IsDispensable &&
                        !HasBlockingProgress(client.Player, definition) && PrerequisitesSatisfied(client.Player, definition))
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

            // Missions whose prerequisites name this one can become offerable (or stop being so) too.
            var related = LoadedMissions.Values
                .Where(candidate => candidate == definition || candidate.Prerequisites.Any(prerequisite => prerequisite.RequiredMissionId == definition.MissionId))
                .ToList();

            foreach (var cellSeed in client.Player.Cells)
            {
                if (!mapChannel.MapCellInfo.Cells.TryGetValue(cellSeed, out var cell))
                    continue;

                foreach (var creature in cell.CreatureList.ToList())
                    if (creature.Npc != null && visited.Add(creature) && related.Any(candidate => candidate.IsRelatedTo(creature)))
                        refresher(client, creature);
            }
        }

        #endregion

        public static bool IsInConversationRange(Manifestation player, Creature creature)
        {
            return creature.MapContextId == player.MapContextId &&
                   (player.MapChannel == null || MapChannelManager.IsOnChannel(creature, player.MapChannel)) &&
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

            return EntityManager.Instance.Creatures.TryGetValue(npcEntityId, out creature) && creature.Npc != null &&
                   (client.Player.MapChannel == null || MapChannelManager.IsOnChannel(creature, client.Player.MapChannel));
        }
    }
}
