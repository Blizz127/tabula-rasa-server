using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Game.Server;
    using Packets.LootDispenser.Server;
    using Packets.MapChannel.Client;
    using Packets.Mission.Server;
    using Packets.MapChannel.Server;
    using Repositories.Char;
    using Structures;
    using Structures.Content;
    using Structures.World;

    /// <summary>
    /// Runtime of the reconstructed-content layer: rule matching, action execution and area triggers.
    /// Only live (validated) rows are indexed, so every hook is a no-op for contexts and missions without content.
    /// </summary>
    public partial class MissionContentManager
    {
        private Dictionary<uint, List<ContentRuleEntry>> _rulesByContext = new();
        private Dictionary<(uint MissionId, uint ObjectiveId), List<ContentAreaEntry>> _areaBindings = new();
        private HashSet<uint> _contextsWithAreas = new();

        // Areas an area_entered rule of the context listens on.
        private Dictionary<uint, List<ContentAreaEntry>> _ruleAreasByContext = new();

        private MissionManager _missions;

        // Monotonic milliseconds for restores and fuses; replaceable by tests.
        public Func<long> TickNow { get; set; } = () => Environment.TickCount64;

        // The mission manager that owns offers and objective progress (the server's singleton by default).
        public MissionManager Missions
        {
            get => _missions ?? MissionManager.Instance;
            set => _missions = value;
        }

        private void BuildRuntime(IReadOnlyDictionary<uint, Mission> missions)
        {
            _rulesByContext = Content.LiveRules
                .GroupBy(rule => rule.MapContextId)
                .ToDictionary(group => group.Key, group => group.OrderBy(rule => rule.Id).ToList());

            var liveAreas = Content.LiveAreas.ToDictionary(area => area.Id);
            _areaBindings = new Dictionary<(uint, uint), List<ContentAreaEntry>>();

            foreach (var mission in missions.Values)
                mission.Bindings.Clear();

            foreach (var binding in Content.LiveBindings)
            {
                if (!missions.TryGetValue(binding.MissionId, out var mission))
                    continue;

                mission.Bindings.Add(binding);

                if ((ObjectiveBindingKind)binding.Kind == ObjectiveBindingKind.AreaEntered && liveAreas.TryGetValue(binding.AreaId, out var area))
                {
                    if (!_areaBindings.TryGetValue((binding.MissionId, binding.ObjectiveId), out var areas))
                        _areaBindings[(binding.MissionId, binding.ObjectiveId)] = areas = new List<ContentAreaEntry>();

                    areas.Add(area);
                }
            }

            _ruleAreasByContext = Content.LiveRules
                .Where(rule => (ContentRuleEvent)rule.Event == ContentRuleEvent.AreaEntered && liveAreas.ContainsKey(rule.AreaId))
                .Select(rule => liveAreas[rule.AreaId])
                .Distinct()
                .GroupBy(area => area.MapContextId)
                .ToDictionary(group => group.Key, group => group.ToList());

            _contextsWithAreas = new HashSet<uint>(_areaBindings.Values.SelectMany(areas => areas).Select(area => area.MapContextId)
                .Concat(_ruleAreasByContext.Keys));
        }

        #region Rules

        /// <summary>
        /// The actions of every live rule that matches the event and whose condition holds in the given state,
        /// in rule id order.
        /// </summary>
        public ContentReaction Plan(ContentEvent contentEvent, ContentState state, Func<ContentRuleEntry, bool> include = null)
        {
            var reaction = new ContentReaction();

            if (!_rulesByContext.TryGetValue(contentEvent.MapContextId, out var rules))
                return reaction;

            foreach (var rule in rules)
            {
                if (!contentEvent.Matches(rule) || (include != null && !include(rule)))
                    continue;

                if (rule.ConditionId != 0 && !state.Evaluate(Content.Catalog.Conditions[rule.ConditionId]))
                    continue;

                reaction.Add(Content.Catalog.RuleActions[rule.Id]);
                reaction.MatchedRuleIds.Add(rule.Id);
            }

            return reaction;
        }

        /// <summary>
        /// Stages the persistent actions into the triggering unit of work. Nothing is sent or changed in memory.
        /// </summary>
        public void Stage(ContentReaction reaction, ICharUnitOfWork unitOfWork, Manifestation player, ContentState state, uint accountId = 0)
        {
            foreach (var action in reaction.Persistent)
            {
                switch ((ContentRuleAction)action.Action)
                {
                    case ContentRuleAction.GrantLogos:
                        if (state.HasLogos(action.LogosId))
                            break;

                        unitOfWork.CharacterLogoses.Stage(player.Id, action.LogosId);
                        state.PlanLogos(action.LogosId);
                        reaction.GrantedLogos.Add((action.LogosId, (LogosGrantProtocol)action.LogosProtocol));
                        break;

                    case ContentRuleAction.TransferToLocation:
                    {
                        // The destination commits with the trigger, so a crash after the commit logs in
                        // at the destination and never back in the source (build plan S6 step 1).
                        var location = Content.Catalog.Locations[action.LocationId];
                        unitOfWork.Characters.StagePosition(player.Id, location.PosX, location.PosY, location.PosZ, location.Rotation, location.MapContextId);
                        reaction.Transfer = location;
                        break;
                    }

                    case ContentRuleAction.SetFact:
                        unitOfWork.CharacterContentFacts.Set(player.Id, player.MapContextId, action.FactKey, action.FactValue,
                            (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                        state.PlanFact(player.MapContextId, action.FactKey, action.FactValue);
                        reaction.FactChanges.Add((player.MapContextId, action.FactKey, action.FactValue));
                        break;

                    case ContentRuleAction.ClearFact:
                        unitOfWork.CharacterContentFacts.Clear(player.Id, player.MapContextId, action.FactKey);
                        state.PlanFact(player.MapContextId, action.FactKey, null);
                        reaction.FactChanges.Add((player.MapContextId, action.FactKey, null));
                        break;

                    case ContentRuleAction.SetAccountSkipBootcamp:
                        if (accountId == 0)
                            throw new InvalidOperationException("set_account_skip_bootcamp needs the account of the triggering character");
                        unitOfWork.GameAccounts.StageCanSkipBootcamp(accountId, true);
                        reaction.SkipBootcampGranted = true;
                        break;

                    case ContentRuleAction.GrantRewards:
                    {
                        var experience = player.Level >= ManifestationManager.MaxPlayerLevel ? 0u : action.Experience;
                        unitOfWork.Characters.StageRewardGrant(player.Id, action.Credits, experience);
                        reaction.GrantedExperience += experience;
                        reaction.GrantedCredits += action.Credits;
                        break;
                    }

                    default:
                        // The validator withholds rules with unimplemented actions.
                        throw new InvalidOperationException($"content action {(ContentRuleAction)action.Action} has no handler");
                }
            }
        }

        /// <summary>
        /// Applies committed persistent actions to memory and notifies the client.
        /// </summary>
        public void Apply(Client client, ContentReaction reaction)
        {
            var player = client.Player;
            var tabula = false;

            foreach (var (logosId, protocol) in reaction.GrantedLogos)
            {
                if (!player.Logos.Contains(logosId))
                    player.Logos.Add(logosId);

                if (protocol == LogosGrantProtocol.StoneAdded)
                    client.CallMethod(player.EntityId, new LogosStoneAddedPacket(logosId));
                else
                    tabula = true;
            }

            // LogosStoneTabula replaces the client's whole list and shows nothing in chat.
            if (tabula)
                client.CallMethod(player.EntityId, new LogosStoneTabulaPacket(player.Logos.ToList()));

            if (reaction.GrantedCredits != 0)
            {
                player.Credits[CurencyType.Credits] += reaction.GrantedCredits;
                client.CallMethod(player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, player.Credits[CurencyType.Credits], 0));
            }

            // "You gained 500 experience points." then the level-up lines (A3-066).
            if (reaction.GrantedExperience != 0)
            {
                player.Experience += reaction.GrantedExperience;
                ManifestationManager.Instance.NotifyExperienceGained(client, reaction.GrantedExperience);
            }

            foreach (var (mapContextId, key, value) in reaction.FactChanges)
            {
                if (value is int set)
                    player.ContentFacts[(mapContextId, key)] = set;
                else
                    player.ContentFacts.Remove((mapContextId, key));
            }

            if (reaction.SkipBootcampGranted && client.AccountEntry != null)
                client.AccountEntry.CanSkipBootcamp = true;

            // Every committed change can move a placement's presence condition or a usable's enabled state.
            ContentMaterializer.RefreshPresence(client, Content, TickNow());
            ContentMaterializer.RefreshFor(client, Content);

            // Last: the position is already committed, so the loading screen only follows it.
            if (reaction.Transfer is { } destination)
                Transfer(client, destination);
        }

        /// <summary>Takes the player to a committed content location through the loading screen (MapChannelManager.ChangeMap).</summary>
        public Action<Client, ContentLocationEntry> Transfer { get; set; } = (client, location) =>
        {
            if (!MapChannelManager.Instance.ChangeMap(client, location.MapContextId,
                    new Vector3((float)location.PosX, (float)location.PosY, (float)location.PosZ), (float)location.Rotation))
                Logger.WriteLog(LogType.Error, $"Content transfer of character {client.Player?.Id} to location {location.Id} could not start; it takes effect at the next login");
        };

        /// <summary>
        /// Runs the presentation actions, after the triggering change and its persistent actions were committed.
        /// </summary>
        public void Present(Client client, ContentReaction reaction)
        {
            var player = client.Player;

            foreach (var action in reaction.Presentation)
            {
                switch ((ContentRuleAction)action.Action)
                {
                    case ContentRuleAction.DispenseRadioMission:
                        Missions.DispenseRadioMission(client, action.MissionId, action.Forced);
                        break;

                    case ContentRuleAction.OfferMissionAtNpc:
                        OfferMissionAtNpc(client, action);
                        break;

                    case ContentRuleAction.ForceConverseGreeting:
                        client.CallMethod(player.EntityId, new ForceConversePacket(action.GreetingId, action.NpcNameId));
                        break;

                    case ContentRuleAction.TutorialNotification:
                        client.CallMethod(SysEntity.ClientMethodId, new DisplayPlayerTutorialNotificationPacket((TutorialId)action.TutorialId));
                        break;

                    case ContentRuleAction.SetPlacementState:
                        SetPlacementState(client, action);
                        break;

                    default:
                        throw new InvalidOperationException($"content action {(ContentRuleAction)action.Action} has no handler");
                }
            }
        }

        /// <summary>
        /// Moves a usable of the player's own channel to the action's state along its client state machine
        /// (the wreck opening, which plays the dropship explosion). A later rebuild derives the same state from the
        /// placement's alternate-state condition, so nothing is stored; a missing object or an illegal transition
        /// is skipped.
        /// </summary>
        private void SetPlacementState(Client client, ContentRuleActionEntry action)
        {
            var mapChannel = client.Player?.MapChannel;

            if (mapChannel == null || !Content.Catalog.Placements.TryGetValue(action.PlacementId, out var placement))
                return;

            var obj = mapChannel.DynamicObjects.FirstOrDefault(candidate =>
                mapChannel.ContentUsables.TryGetValue(candidate.EntityId, out var id) && id == placement.Id);

            if (obj == null || (uint)obj.StateId == action.StateId ||
                !MissionContentRules.UsableStateTransitions.TryGetValue((ContentUsableKind)placement.UsableKind, out var transitions) ||
                !transitions.Contains(((uint)obj.StateId, action.StateId)))
                return;

            obj.StateId = (UseObjectState)action.StateId;
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket((UseObjectState)action.StateId, 0));
        }

        /// <summary>
        /// Opens the mission-offer window of the action's NPC placement, as if the
        /// player had initiated the conversation: Converse carries the MissionDispense
        /// topic and forces it open. The catalog already validated that the placement
        /// is the mission's giver creature; here the creature must exist in the
        /// player's map and the player must be within conversation range, otherwise
        /// the offer is skipped (the NPC's own dispense topic still offers it).
        /// </summary>
        private void OfferMissionAtNpc(Client client, ContentRuleActionEntry action)
        {
            var player = client.Player;

            if (!MissionManager.IsInWorld(client) || !Content.Catalog.Placements.TryGetValue(action.PlacementId, out var placement))
                return;

            var creature = EntityManager.Instance.Creatures.Values.FirstOrDefault(c =>
                c.DbId == placement.CreatureId && c.MapContextId == player.MapContextId &&
                (player.MapChannel == null || MapChannelManager.IsOnChannel(c, player.MapChannel)));

            if (creature?.Npc == null || !MissionManager.IsInConversationRange(player, creature))
                return;

            if (!Missions.LoadedMissions.TryGetValue(action.MissionId, out var definition) ||
                !definition.IsDispensable ||
                Missions.HasBlockingProgress(player, definition) ||
                player.PendingRadioOffers.Contains(action.MissionId))
                return;

            var convoData = new Dictionary<ConversationType, object>
            {
                { ConversationType.MissionDispense, new Dictionary<uint, MissionInfo> { { definition.MissionId, definition } } },
                { ConversationType.ForceTopic, new ForceTopic(ConversationType.MissionDispense, (int)definition.MissionId) }
            };

            client.CallMethod(creature.EntityId, new ConversePacket(convoData));
        }

        /// <summary>
        /// Completes equip-bound objectives the player's currently equipped items satisfy.
        /// Level-triggered: called after a successful equip commit, after objective reveals
        /// and on reconnect reconciliation, so a matching item equipped before the objective
        /// was revealed still counts and the check is idempotent.
        /// </summary>
        public void OnEquipCommitted(Client client)
        {
            var player = client.Player;

            if (player == null || !MissionManager.IsInWorld(client))
                return;

            var equipped = EquippedTemplateIds(player);
            if (equipped.Count == 0)
                return;

            foreach (var mission in player.Missions.Values)
            {
                if (mission.State != MissionState.Active || !Missions.LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    continue;

                foreach (var binding in definition.Bindings)
                {
                    if ((ObjectiveBindingKind)binding.Kind != ObjectiveBindingKind.Equip ||
                        !mission.Objectives.TryGetValue(binding.ObjectiveId, out var status) ||
                        status != MissionObjectiveState.Incomplete)
                        continue;

                    if (EquipBindingMatches(binding, equipped))
                        Missions.CompleteBoundObjective(client, mission.MissionId, binding.ObjectiveId, ObjectiveBindingKind.Equip);
                }
            }
        }

        /// <summary>
        /// The item-template ids of everything the player counts as equipped: the
        /// equipment slots and the weapon drawer.
        /// </summary>
        private static HashSet<uint> EquippedTemplateIds(Manifestation player)
        {
            var result = new HashSet<uint>();

            foreach (var entityId in player.Inventory.EquippedInventory.Concat(player.Inventory.WeaponDrawer))
            {
                var item = EntityManager.Instance.GetItem(entityId);
                if (item != null)
                    result.Add(item.ItemTemplateId);
            }

            return result;
        }

        private bool EquipBindingMatches(NpcMissionObjectiveBindingEntry binding, HashSet<uint> equipped)
        {
            switch (binding.EquipMatch)
            {
                case 0:     // any equip
                    return equipped.Count > 0;

                case 1:     // a specific item template
                    return equipped.Contains(binding.ItemTemplateId);

                case 2:     // any template of an item set
                    return Content.Catalog.ItemSets.TryGetValue(binding.ItemSetId, out var entries) &&
                           entries.Any(set => equipped.Contains(set.ItemTemplateId));

                default:
                    return false;
            }
        }

        /// <summary>
        /// A content usable a player is currently looting or has used, per player entity id.
        /// </summary>
        private sealed class PendingUse
        {
            public ulong ObjectId;
            public uint PlacementId;
            public uint WindupMs;
        }

        private readonly Dictionary<ulong, PendingUse> _pendingUses = new();

        /// <summary>
        /// A use request on a content usable. The client has already range-checked
        /// (MAX_CONVERSATION_RANGE does not apply; the client checks interact range itself),
        /// so the server verifies the object is enabled for this client, sends the windup,
        /// and queues the recovery that completes the use.
        /// </summary>
        public void RequestUseContentUsable(Client client, RequestUseObjectPacket packet, DynamicObject obj)
        {
            var player = client.Player;
            var mapChannel = player.MapChannel;

            if (mapChannel == null || !mapChannel.ContentUsables.TryGetValue(packet.EntityId, out var placementId) ||
                !Content.Catalog.Placements.TryGetValue(placementId, out var placement))
                return;

            var state = new ContentState(player);
            var (enabled, missionActivated) = ContentMaterializer.UsableStateFor(Content, state, placement);
            if (!enabled)
                return;

            // Structures (the wreck) are scenery with states, never used by players.
            if ((ContentUsableKind)placement.UsableKind == ContentUsableKind.Structure)
                return;

            // An armed or detonated bomb cannot be planted again (disarming, 114 -> 113, is not implemented).
            if ((ContentUsableKind)placement.UsableKind == ContentUsableKind.Bomb && (uint)obj.StateId != BombDisarmed)
                return;

            obj.ActivateMission = missionActivated;
            obj.WindupTime = placement.WindupMs;

            client.CallMethod(player.EntityId, new PerformWindupPacket(PerformType.TwoArgs, packet.ActionId, packet.ActionArgId));
            client.CallMethod(packet.EntityId, new UsePacket(player.EntityId, obj.StateId, (int)placement.WindupMs));

            var actionData = new ActionData(player, packet.ActionId, packet.ActionArgId, placement.WindupMs) { SourceId = obj.EntityId };
            mapChannel.PerformRecovery.Add(actionData);
            obj.TriggeredByPlayers.Add(client);

            _pendingUses[player.EntityId] = new PendingUse { ObjectId = obj.EntityId, PlacementId = placementId, WindupMs = placement.WindupMs };
        }

        /// <summary>
        /// The windup on a content usable finished: complete loot_all/use_completed
        /// bindings bound to this placement.
        /// </summary>
        public void ContentUsableRecovery(MapChannel mapChannel, ActionData action)
        {
            if (!_pendingUses.TryGetValue(action.Actor.EntityId, out var pending) || pending.ObjectId != action.SourceId)
                return;

            _pendingUses.Remove(action.Actor.EntityId);

            var obj = mapChannel.DynamicObjects.FirstOrDefault(candidate => candidate.EntityId == pending.ObjectId);
            if (obj == null)
                return;

            foreach (var client in obj.TriggeredByPlayers.Where(client => client?.Player == action.Actor).ToList())
                obj.TriggeredByPlayers.Remove(client);

            var player = action.Actor as Manifestation;
            if (player == null)
                return;

            var owner = mapChannel.ClientList.FirstOrDefault(candidate => candidate?.Player == player);
            if (owner == null)
                return;

            var placement = Content.Catalog.Placements[pending.PlacementId];

            // A container's use opens its loot window; the objective completes on Loot All.
            if ((ContentUsableKind)placement.UsableKind == ContentUsableKind.Container)
            {
                OpenContentContainer(owner, placement, obj);
                return;
            }

            if ((ContentUsableKind)placement.UsableKind == ContentUsableKind.Bomb && !PlantBomb(owner, mapChannel, placement, obj))
                return;

            CompleteUseBoundObjectives(owner, placement);
        }

        private const uint BombDisarmed = 113;
        private const uint BombArmed = 114;
        private const uint BombDetonated = 115;

        /// <summary>
        /// The plant (build plan 1.6): the bomb enters state 114 and its fuse starts. In one transaction the
        /// timers of the objectives its detonation completes are disarmed and the placement_state_entered 114
        /// rules commit (the planted-bomb fact). The countdown keeps running on the client (B1-044: 00:02:06 to
        /// 00:02:00 through the plant), but it can no longer fail the objective.
        /// </summary>
        private bool PlantBomb(Client client, MapChannel mapChannel, ContentPlacementEntry placement, DynamicObject obj)
        {
            var player = client.Player;

            if ((uint)obj.StateId != BombDisarmed)
                return false;

            var disarmed = new List<ObjectiveTimer>();
            foreach (var (mission, binding) in PlacementStateBindings(player, placement.Id, BombDetonated))
                if (mission.Timers.TryGetValue(binding.ObjectiveId, out var timer) && !timer.Disarmed && !disarmed.Contains(timer))
                    disarmed.Add(timer);

            var state = new ContentState(player);
            var reaction = Plan(new ContentEvent(ContentRuleEvent.PlacementStateEntered, player.MapContextId, placementId: placement.Id, stateId: BombArmed), state);

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                foreach (var (mission, binding) in PlacementStateBindings(player, placement.Id, BombDetonated))
                    if (mission.Timers.TryGetValue(binding.ObjectiveId, out var timer) && disarmed.Contains(timer))
                        unitOfWork.CharacterMissions.SetObjectiveTimer(player.Id, mission.MissionId, binding.ObjectiveId, timer.RemainingMs, timer.AnchorMs, true);

                Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"PlantBomb: could not save the plant of placement {placement.Id} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }

            foreach (var timer in disarmed)
                timer.Disarmed = true;

            ArmBomb(mapChannel, placement, obj, player.Id);
            Apply(client, reaction);
            Present(client, reaction);
            return true;
        }

        private void ArmBomb(MapChannel mapChannel, ContentPlacementEntry placement, DynamicObject obj, uint armedBy)
        {
            obj.StateId = (UseObjectState)BombArmed;
            obj.FuseAt = TickNow() + placement.FuseMs;
            obj.ArmedByCharacterId = armedBy;
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket((UseObjectState)BombArmed, 0));
        }

        /// <summary>The player's active missions' incomplete objectives bound to this placement reaching the state.</summary>
        private IEnumerable<(PlayerMission Mission, NpcMissionObjectiveBindingEntry Binding)> PlacementStateBindings(Manifestation player, uint placementId, uint stateId)
        {
            foreach (var mission in player.Missions.Values.ToList())
            {
                if (mission.State != MissionState.Active || !Missions.LoadedMissions.TryGetValue(mission.MissionId, out var definition))
                    continue;

                foreach (var binding in definition.Bindings)
                    if ((ObjectiveBindingKind)binding.Kind == ObjectiveBindingKind.PlacementState && binding.PlacementId == placementId &&
                        binding.TargetState == stateId &&
                        mission.Objectives.TryGetValue(binding.ObjectiveId, out var status) && status == MissionObjectiveState.Incomplete)
                        yield return (mission, binding);
            }
        }

        /// <summary>
        /// The fuse step of the map tick: armed bombs whose fuse burned down detonate (state 115, whose client
        /// effects play the explosion). The instance owner is credited in a private instance, the arming
        /// character in a shared world; without that character in the channel only the state changes.
        /// </summary>
        public void DetonateFuses(MapChannel mapChannel, long now = -1)
        {
            if (now < 0)
                now = TickNow();

            foreach (var obj in mapChannel.DynamicObjects.ToList())
            {
                if (obj.DynamicObjectType != DynamicObjectType.ContentUsable || obj.FuseAt == 0 || now < obj.FuseAt)
                    continue;

                obj.FuseAt = 0;

                if (!mapChannel.ContentUsables.TryGetValue(obj.EntityId, out var placementId))
                    continue;

                obj.StateId = (UseObjectState)BombDetonated;
                CellManager.Instance.CellCallMethod(obj, new ForceStatePacket((UseObjectState)BombDetonated, 0));

                var creditedId = mapChannel.IsPrivateInstance ? mapChannel.OwnerCharacterId : obj.ArmedByCharacterId;
                var client = mapChannel.ClientList.FirstOrDefault(candidate => candidate?.Player != null && candidate.Player.Id == creditedId);

                if (client != null && MissionManager.IsInWorld(client))
                    Missions.CommitPlacementState(client, placementId, BombDetonated);
            }
        }

        /// <summary>
        /// Opens a content container's per-owner loot window with its item set as the
        /// lootable items. The objective completes when the player loots all of it.
        /// </summary>
        private void OpenContentContainer(Client client, ContentPlacementEntry placement, DynamicObject obj)
        {
            if (!_openContainers.TryGetValue(obj.EntityId, out var open) || open.OwnerEntityId != client.Player.EntityId)
            {
                open = new ContentContainerLoot
                {
                    OwnerEntityId = client.Player.EntityId,
                    Items = Content.Catalog.ItemSets.TryGetValue(placement.LootItemSetId, out var entries)
                        ? entries.Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToList()
                        : new List<(uint, uint)>()
                };
                _openContainers[obj.EntityId] = open;
            }

            open.Sent = true;
            client.CallMethod(SysEntity.ClientMethodId, new CreatePhysicalEntityPacket(obj.EntityId, obj.EntityClassId));
            client.CallMethod(obj.EntityId, new LootInfoPacket(open.Items.Select(item => new LootItem(item.ItemTemplateId, 0, item.Quantity, client.Player.EntityId, 0)).ToList()));
            client.CallMethod(obj.EntityId, new CanLootItemsPacket(true, open.Items.Select(item => new LootItem(item.ItemTemplateId, 0, item.Quantity, client.Player.EntityId, 0)).ToList()));
        }

        private sealed class ContentContainerLoot
        {
            public ulong OwnerEntityId;
            public List<(uint ItemTemplateId, uint Quantity)> Items = new();
            public bool Sent;
            public bool Granted;
        }

        private readonly Dictionary<ulong, ContentContainerLoot> _openContainers = new();

        /// <summary>
        /// Loot All on a content container: grants every item of the open container's
        /// item set in one all-or-nothing transaction, then completes the bound objective.
        /// </summary>
        public void RequestLootAllFromContentContainer(Client client, ulong entityId)
        {
            var player = client.Player;
            var mapChannel = player.MapChannel;

            if (mapChannel == null || !_openContainers.TryGetValue(entityId, out var open) || open.OwnerEntityId != player.EntityId)
                return;

            if (open.Items.Count == 0 || open.Granted)
                return;

            // All-or-nothing: nothing is granted unless every item fits.
            var planned = new List<Item>();
            foreach (var (templateId, quantity) in open.Items)
            {
                var item = ItemManager.Instance.CreateFromTemplateId(templateId, quantity);
                if (item == null)
                {
                    foreach (var created in planned)
                        EntityManager.Instance.UnregisterItem(created.EntityId);
                    return;
                }
                planned.Add(item);
            }

            foreach (var item in planned)
                if (InventoryManager.Instance.AddItemToInventory(client, item) == null)
                {
                    // The inventory could not take it: roll back what was added.
                    foreach (var created in planned)
                        if (created != item)
                            InventoryManager.Instance.RemoveItemBySlot(client, InventoryType.Personal, created.OwnerSlotId);
                    return;
                }

            open.Granted = true;
            open.Items.Clear();
            client.CallMethod(entityId, new TakenInfoPacket(player.EntityId, new List<LootItem>()));
            client.CallMethod(entityId, new CanLootItemsPacket(false, new List<LootItem>()));

            foreach (var binding in BindingsOfKind(ObjectiveBindingKind.LootAll).Where(binding => binding.PlacementId == ContentUsablesPlacementId(mapChannel, entityId)))
                Missions.CompleteBoundObjective(client, binding.MissionId, binding.ObjectiveId, ObjectiveBindingKind.LootAll);
        }

        private uint ContentUsablesPlacementId(MapChannel mapChannel, ulong entityId) =>
            mapChannel.ContentUsables.TryGetValue(entityId, out var id) ? id : 0;

        /// <summary>
        /// Applies weapon/ability damage to a destroyable content placement: UpdateHitPoints,
        /// ForceState at the InertDestroyable thresholds (185 50%, 186 25%, 2 destroyed),
        /// and after restore_ms back to 110 (intact) with hit points restored. No XP, no loot.
        /// Returns the placement when this hit destroyed it, for the hit bindings.
        /// </summary>
        public uint? DamageContentUsable(MapChannel mapChannel, ulong entityId, int damage, Client sourceClient = null)
        {
            if (!mapChannel.ContentUsables.TryGetValue(entityId, out var placementId) ||
                !Content.Catalog.Placements.TryGetValue(placementId, out var placement) ||
                placement.HitPoints == 0)
                return null;

            var obj = mapChannel.DynamicObjects.FirstOrDefault(candidate => candidate.EntityId == entityId);
            if (obj == null || obj.HitPoints == 0)
                return null;

            obj.HitPoints = (uint)Math.Max(0, (int)obj.HitPoints - damage);
            CellManager.Instance.CellCallMethod(obj, new UpdateHitPointsPacket(obj.HitPoints));

            if (obj.HitPoints > 0)
            {
                var fraction = (double)obj.HitPoints / obj.MaxHitPoints;
                var threshold = fraction <= 0.25 ? 186u : fraction <= 0.5 ? 185u : 0;
                if (threshold != 0 && (uint)obj.StateId != threshold)
                {
                    obj.StateId = (UseObjectState)threshold;
                    CellManager.Instance.CellCallMethod(obj, new ForceStatePacket((UseObjectState)threshold, 0));
                }
                return null;
            }

            // Destroyed: state 2, restore after restore_ms (0 = never).
            obj.StateId = UseObjectState.StateDestroyed;
            CellManager.Instance.CellCallMethod(obj, new ForceStatePacket(UseObjectState.StateDestroyed, 0));
            if (placement.RestoreMs > 0)
                obj.RestoreAt = TickNow() + placement.RestoreMs;

            if (sourceClient != null)
                OnContentUsableHit(sourceClient, placementId, 0, true);

            return placementId;
        }

        /// <summary>
        /// Restores destroyed placements whose restore_ms has elapsed: back to 110 (intact)
        /// with hit points restored.
        /// </summary>
        public void RestoreDestroyedUsables(MapChannel mapChannel, long now = -1)
        {
            if (now < 0)
                now = TickNow();
            foreach (var obj in mapChannel.DynamicObjects)
            {
                if (obj.DynamicObjectType != DynamicObjectType.ContentUsable || obj.RestoreAt == 0 || now < obj.RestoreAt)
                    continue;

                if (!mapChannel.ContentUsables.TryGetValue(obj.EntityId, out var placementId) ||
                    !Content.Catalog.Placements.TryGetValue(placementId, out var placement) || placement.HitPoints == 0)
                {
                    obj.RestoreAt = 0;
                    continue;
                }

                obj.RestoreAt = 0;
                obj.HitPoints = placement.HitPoints;
                obj.StateId = (UseObjectState)110;
                CellManager.Instance.CellCallMethod(obj, new ForceStatePacket((UseObjectState)110, 0));
                CellManager.Instance.CellCallMethod(obj, new UpdateHitPointsPacket(obj.HitPoints));
            }
        }

        /// <summary>
        /// Completes the hit bindings bound to a placement, on the destroying hit when the
        /// binding says so (or on any hit otherwise).
        /// </summary>
        public void OnContentUsableHit(Client sourceClient, uint placementId, uint actionId, bool destroyed)
        {
            foreach (var binding in BindingsOfKind(ObjectiveBindingKind.Hit).Where(binding => binding.PlacementId == placementId))
            {
                if (binding.ActionId != 0 && binding.ActionId != actionId)
                    continue;

                if (binding.DestroyingHitOnly && !destroyed)
                    continue;

                Missions.CompleteBoundObjective(sourceClient, binding.MissionId, binding.ObjectiveId, ObjectiveBindingKind.Hit);
            }
        }

        /// <summary>
        /// Advances a kill-bound objective's counter by one and completes the objective
        /// when the counter reaches its target. Counter rows come from
        /// npc_mission_objective_counter (initial/target); the client sees the same values
        /// through UpdateObjectiveCounter and the mission-log CounterDict.
        /// </summary>
        public void OnKillBinding(Client sourceClient, uint missionId, uint objectiveId, ObjectiveBindingKind kind)
        {
            var player = sourceClient.Player;

            if (!Missions.LoadedMissions.TryGetValue(missionId, out var definition))
                return;

            var counter = Content.Catalog.Counters.FirstOrDefault(entry =>
                entry.MissionId == missionId && entry.ObjectiveId == objectiveId);

            if (counter == null)
            {
                // No counter rows: a single kill completes the objective.
                Missions.CompleteBoundObjective(sourceClient, missionId, objectiveId, kind);
                return;
            }

            var value = player.Missions.TryGetValue(missionId, out var mission) &&
                        mission.Counters.TryGetValue((objectiveId, counter.CounterId), out var current)
                ? current + 1
                : counter.InitialValue + 1;

            var state = new ContentState(player);
            state.PlanCounter(missionId, objectiveId, counter.CounterId, value);

            try
            {
                using var unitOfWork = Missions.GameUnitOfWorkFactoryForContent.CreateChar();
                unitOfWork.CharacterMissions.UpsertCounter(player.Id, missionId, objectiveId, counter.CounterId, value);
                unitOfWork.Complete();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"OnKillBinding: could not save counter {missionId}/{objectiveId}/{counter.CounterId} for character {player.Id}");
                Logger.WriteLog(LogType.Error, e);
                return;
            }

            if (mission != null)
                mission.Counters[(objectiveId, counter.CounterId)] = value;

            sourceClient.CallMethod(player.EntityId, new UpdateObjectiveCounterPacket(
                missionId, objectiveId, counter.CounterId, value, counter.InitialValue, counter.TargetValue));

            if (value >= counter.TargetValue)
                Missions.CompleteBoundObjective(sourceClient, missionId, objectiveId, kind);
        }

        /// <summary>
        /// True when the action's source is a reconstructed-content usable of this channel.
        /// </summary>
        public bool IsContentUsableSource(MapChannel mapChannel, ulong sourceId) =>
            mapChannel != null && mapChannel.ContentUsables.ContainsKey(sourceId);

        private IEnumerable<NpcMissionObjectiveBindingEntry> BindingsOfKind(ObjectiveBindingKind kind) =>
            Content.LiveBindings.Where(binding => (ObjectiveBindingKind)binding.Kind == kind);

        /// <summary>
        /// Completes the use_completed bindings bound to this placement.
        /// </summary>
        private void CompleteUseBoundObjectives(Client client, ContentPlacementEntry placement)
        {
            foreach (var binding in BindingsOfKind(ObjectiveBindingKind.UseCompleted).Where(binding => binding.PlacementId == placement.Id))
                Missions.CompleteBoundObjective(client, binding.MissionId, binding.ObjectiveId, ObjectiveBindingKind.UseCompleted);
        }

        /// <summary>
        /// Reacts to an event that has no transaction of its own, such as entering a map.
        /// </summary>
        /// <returns>The rules that fired and committed; none when the commit failed.</returns>
        public IReadOnlyList<uint> React(Client client, ContentEvent contentEvent, Func<ContentRuleEntry, bool> include = null)
        {
            var player = client.Player;
            var state = new ContentState(player);
            var reaction = Plan(contentEvent, state, include);

            if (reaction.IsEmpty)
                return reaction.MatchedRuleIds;

            if (reaction.Persistent.Count > 0)
            {
                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                    Stage(reaction, unitOfWork, player, state, client.AccountEntry?.Id ?? 0);
                    unitOfWork.Complete();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Content {contentEvent.Kind} in context {contentEvent.MapContextId}: could not save the reaction for character {player.Id}");
                    Logger.WriteLog(LogType.Error, e);
                    return Array.Empty<uint>();
                }

                Apply(client, reaction);
            }

            Present(client, reaction);
            return reaction.MatchedRuleIds;
        }

        public void OnPlayerEnteredMap(Client client)
        {
            if (!MissionManager.IsInWorld(client))
                return;

            client.Player.LastContentSample = null;
            client.Player.FiredAreaRules.Clear();
            ContentMaterializer.RefreshPresence(client, Content, TickNow());
            ContentMaterializer.RefreshFor(client, Content);
            React(client, new ContentEvent(ContentRuleEvent.EnteredMap, client.Player.MapContextId));
        }

        #endregion

        #region Areas

        /// <summary>
        /// Completes area-bound objectives of players in this channel. A player's movement since the last
        /// sample is tested as a segment, so a fast crossing between samples still counts.
        /// </summary>
        public void DoWork(MapChannel mapChannel)
        {
            var contextId = mapChannel.MapInfo?.MapContextId ?? 0;

            if (!_contextsWithAreas.Contains(contextId))
                return;

            foreach (var client in mapChannel.ClientList.ToList())
            {
                if (client?.Player == null || !MissionManager.IsInWorld(client) || client.Player.MapContextId != contextId)
                    continue;

                var player = client.Player;
                var current = player.Position;
                var previous = player.LastContentSample is { } sample && sample.MapContextId == contextId ? sample.Position : current;

                player.LastContentSample = (contextId, current);

                // An area_entered rule fires once per stay in its area: on the crossing, or on the first sample inside
                // the area at which its condition holds (a recruit who talks to Van Valkenberg on the exit pad is
                // already standing in it). Leaving the area re-arms its rules.
                if (_ruleAreasByContext.TryGetValue(contextId, out var ruleAreas))
                    foreach (var area in ruleAreas)
                    {
                        var touched = SegmentEntersArea(previous, current, area);
                        var inside = SegmentEntersArea(current, current, area);

                        if (touched)
                            foreach (var ruleId in React(client, new ContentEvent(ContentRuleEvent.AreaEntered, contextId, areaId: area.Id),
                                         rule => !player.FiredAreaRules.Contains(rule.Id)))
                                player.FiredAreaRules.Add(ruleId);

                        if (!inside)
                            player.FiredAreaRules.RemoveWhere(ruleId => Content.Catalog.Rules.TryGetValue(ruleId, out var rule) && rule.AreaId == area.Id);
                    }

                foreach (var (missionId, objectiveId) in IncompleteAreaObjectives(player))
                {
                    var areas = _areaBindings[(missionId, objectiveId)];

                    foreach (var area in areas)
                    {
                        if (area.MapContextId != contextId || !SegmentEntersArea(previous, current, area))
                            continue;

                        Missions.CompleteBoundObjective(client, missionId, objectiveId, ObjectiveBindingKind.AreaEntered);
                        break;
                    }
                }
            }
        }

        private IEnumerable<(uint MissionId, uint ObjectiveId)> IncompleteAreaObjectives(Manifestation player)
        {
            var result = new List<(uint, uint)>();

            foreach (var mission in player.Missions.Values)
            {
                if (mission.State != MissionState.Active)
                    continue;

                foreach (var (objectiveId, status) in mission.Objectives)
                    if (status == MissionObjectiveState.Incomplete && _areaBindings.ContainsKey((mission.MissionId, objectiveId)))
                        result.Add((mission.MissionId, objectiveId));
            }

            return result;
        }

        public static bool SegmentEntersArea(Vector3 from, Vector3 to, ContentAreaEntry area)
        {
            var center = new Vector3((float)area.PosX, (float)area.PosY, (float)area.PosZ);
            var radius = (float)area.Radius;

            switch ((ContentAreaShape)area.Shape)
            {
                case ContentAreaShape.Sphere:
                    return Vector3.DistanceSquared(ClosestPoint(from, to, center), center) <= radius * radius;

                case ContentAreaShape.VerticalCylinder:
                {
                    // Closest approach in the horizontal plane; the height at that point must lie within the cylinder.
                    var from2 = new Vector2(from.X, from.Z);
                    var to2 = new Vector2(to.X, to.Z);
                    var center2 = new Vector2(center.X, center.Z);
                    var direction = to2 - from2;
                    var lengthSquared = direction.LengthSquared();
                    var t = lengthSquared > 0 ? Math.Clamp(Vector2.Dot(center2 - from2, direction) / lengthSquared, 0f, 1f) : 0f;
                    var closest = from2 + direction * t;
                    var height = from.Y + (to.Y - from.Y) * t;

                    return Vector2.DistanceSquared(closest, center2) <= radius * radius &&
                           Math.Abs(height - center.Y) <= area.HalfHeight;
                }

                default:
                    return false;
            }
        }

        private static Vector3 ClosestPoint(Vector3 from, Vector3 to, Vector3 point)
        {
            var direction = to - from;
            var lengthSquared = direction.LengthSquared();

            if (lengthSquared <= 0)
                return from;

            var t = Math.Clamp(Vector3.Dot(point - from, direction) / lengthSquared, 0f, 1f);
            return from + direction * t;
        }

        #endregion
    }
}
