using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
    using Packets.Communicator.Server;
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
        /// <summary>How close an escort has to stay to its player before it is sent after them.</summary>
        public const float EscortFollowDistance = 6.0f;

        /// <summary>How often an escort may be re-pathed (milliseconds).</summary>
        public const long EscortRepathMs = 2000;

        private Dictionary<uint, List<ContentRuleEntry>> _rulesByContext = new();
        private Dictionary<(uint MissionId, uint ObjectiveId), List<ContentAreaEntry>> _areaBindings = new();
        private HashSet<uint> _contextsWithAreas = new();

        // Area probe: entity id -> the tick it was last logged, so a player standing near a trigger leaves one line
        // a second rather than one a tick. A trigger that does not fire while the player is clearly in the place is
        // otherwise invisible: neither the objective nor the area reports anything.
        private readonly Dictionary<ulong, long> _lastAreaProbe = new();

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

                    case ContentRuleAction.DamagePlayer:
                        reaction.PlayerDamage += action.Damage;
                        break;

                    case ContentRuleAction.MoveCreatureToLocation:
                        // A world effect, applied after the commit with the map channel in hand.
                        reaction.CreatureMoves.Add((action.PlacementId, action.LocationId));
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

            if (reaction.PlayerDamage > 0)
                DamagePlayer(client, reaction.PlayerDamage);

            foreach (var (placementId, locationId) in reaction.CreatureMoves)
                SendCreatureToLocation(client, placementId, locationId);

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
        /// Opens a content container's per-owner loot window. Its rows are real items, created once
        /// per owner from the placement's item set and introduced to the client before the window
        /// opens: corpselootwindow resolves every row with GetEntity(itemId) and skips one it cannot
        /// resolve, so a row built from a bare template id is an empty window - which is what the
        /// boot camp's supply crate showed the live player on 2026-09-16 (GAP-S2-GEAR-OBJECTIVES).
        /// The loot_all binding completes when the container is empty, however it was emptied.
        /// </summary>
        private void OpenContentContainer(Client client, ContentPlacementEntry placement, DynamicObject obj)
        {
            if (!_openContainers.TryGetValue(obj.EntityId, out var open) || open.OwnerEntityId != client.Player.EntityId)
            {
                open = new ContentContainerLoot { OwnerEntityId = client.Player.EntityId };

                if (Content.Catalog.ItemSets.TryGetValue(placement.LootItemSetId, out var entries))
                    foreach (var entry in entries)
                    {
                        var item = ItemManager.Instance.CreateFromTemplateId(entry.ItemTemplateId, entry.Quantity);

                        if (item != null)
                            open.Items.Add(new LootItem(item, client.Player.EntityId, 0));
                        else
                            // A row whose item cannot be built is not added at all. If it were, the window
                            // would list an item the player can never take, and the container would never
                            // read as empty.
                            Logger.WriteLog(LogType.Error,
                                $"Content container {placement.Id}: item template {entry.ItemTemplateId} could not be built, so its row is omitted.");
                    }

                Logger.WriteLog(LogType.Debug,
                    $"{client.Player.Name} opened content container {placement.Id} ({open.Items.Count} row(s)).");

                _openContainers[obj.EntityId] = open;
            }

            var remaining = open.Remaining();

            // No CreatePhysicalEntity here. The corpse path sends one because it invents a loot
            // dispenser entity on the spot; this container is a content usable that materialized
            // into the world with the map and that every client in range already has, with its
            // position, rotation and use state. Re-creating it from an entity id and a class alone
            // replaced all of that with a bare entity - the live report of 2026-09-18 read it as
            // "the crate looked a little translucent".

            // The items have to exist on the client before the window lists them (the corpse
            // path does the same in RequestCorpseLooting); re-sending one it has is an update.
            foreach (var row in remaining)
                ItemManager.Instance.SendItemDataToClient(client, row.Item, false);

            client.CallMethod(obj.EntityId, new LootInfoPacket(remaining));
            client.CallMethod(obj.EntityId, new CanLootItemsPacket(remaining.Count > 0, remaining));

            // And this is what actually opens the window. lootdispenser.Recv_LootCorpse is the only
            // place the client posts UI_SHOW_CORPSELOOT; Recv_LootInfo and Recv_CanLootItems just
            // update state it is already showing. Without it the player used the crate, the server
            // built all five items and sent them, and no window ever appeared - so no take could be
            // requested and the objective bound to the container could never complete
            // (live report 2026-09-18, and the character database showed the five items created and
            // owned by nobody).
            client.CallMethod(obj.EntityId, new LootCorpsePacket(client.Player.EntityId, remaining));
        }

        /// <summary>
        /// The client asking to open a container's window again (lootdispenser.Recv_Use sends
        /// RequestCorpseLooting on every use). Only a container already opened by this player is
        /// re-shown; nothing is created, so a player cannot use this to refill one.
        /// </summary>
        public void ReopenContentContainer(Client client, ulong entityId)
        {
            var open = OpenContainerFor(client, entityId);
            var mapChannel = client?.Player?.MapChannel;
            if (open == null || mapChannel == null)
                return;

            var remaining = open.Remaining();

            foreach (var row in remaining)
                ItemManager.Instance.SendItemDataToClient(client, row.Item, false);

            client.CallMethod(entityId, new LootInfoPacket(remaining));
            client.CallMethod(entityId, new CanLootItemsPacket(remaining.Count > 0, remaining));
            client.CallMethod(entityId, new TakenInfoPacket(client.Player.EntityId, open.Taken()));
            client.CallMethod(entityId, new LootCorpsePacket(client.Player.EntityId, remaining));
        }

        private sealed class ContentContainerLoot
        {
            public ulong OwnerEntityId;
            public readonly List<LootItem> Items = new();

            public List<LootItem> Remaining() => Items.FindAll(row => !row.Taken);
            public List<LootItem> Taken() => Items.FindAll(row => row.Taken);
        }

        private readonly Dictionary<ulong, ContentContainerLoot> _openContainers = new();

        private ContentContainerLoot OpenContainerFor(Client client, ulong entityId) =>
            client.Player?.MapChannel != null && _openContainers.TryGetValue(entityId, out var open) && open.OwnerEntityId == client.Player.EntityId
                ? open
                : null;

        /// <summary>
        /// Loot All on a content container: takes every row that still fits, as the corpse
        /// dispenser does, then settles the container.
        /// </summary>
        public void RequestLootAllFromContentContainer(Client client, ulong entityId)
        {
            var open = OpenContainerFor(client, entityId);
            if (open == null)
                return;

            // A container with nothing left to hand over still settles: the objective bound to it is
            // satisfied by the container being empty, and returning early here meant a crate whose rows
            // had already been emptied - by the player, by an earlier session, or by a row that could not
            // be built - could never complete the objective waiting on it (live report 2026-09-17).
            foreach (var row in open.Remaining())
                TakeContainerRow(client, row, null);

            SettleContainer(client, entityId, open);
        }

        /// <summary>
        /// One row off a content container - the window's right-click path
        /// (lootdispenser.RequestLootItemFromCorpse(self.entityId, itemId, destSlot)). A row that is
        /// gone or was never there is answered with TakenInfo, since it is still on the asker's screen.
        /// </summary>
        public void RequestLootItemFromContentContainer(Client client, ulong entityId, ulong itemId, uint? destSlot)
        {
            var open = OpenContainerFor(client, entityId);
            if (open == null)
                return;

            var row = open.Items.Find(candidate => candidate.EntityId == itemId);
            if (row == null || row.Taken)
            {
                client.CallMethod(entityId, new TakenInfoPacket(client.Player.EntityId, open.Taken()));
                return;
            }

            if (!TakeContainerRow(client, row, destSlot))
                return;

            SettleContainer(client, entityId, open);
        }

        private static bool TakeContainerRow(Client client, LootItem row, uint? destSlot)
        {
            if (row.Taken || row.Item == null)
                return false;

            var placed = destSlot.HasValue
                ? InventoryManager.Instance.AddItemToInventory(client, row.Item, destSlot.Value)
                : InventoryManager.Instance.AddItemToInventory(client, row.Item);

            if (placed == null)
            {
                client.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return false;
            }

            row.Taken = true;
            return true;
        }

        /// <summary>
        /// Tells the window what is gone and what is left; an emptied container completes the
        /// loot_all bindings bound to its placement.
        /// </summary>
        private void SettleContainer(Client client, ulong entityId, ContentContainerLoot open)
        {
            var mapChannel = client.Player.MapChannel;
            var remaining = open.Remaining();

            client.CallMethod(entityId, new TakenInfoPacket(client.Player.EntityId, open.Taken()));
            client.CallMethod(entityId, new CanLootItemsPacket(remaining.Count > 0, remaining));

            // "Get your gear from the crate" is satisfied when the player has the gear, however they came by
            // it: rows they took are gone from here, and rows they are already wearing or carrying count too,
            // or a player who picked the gear up before the objective was revealed would be stuck short of it
            // (live report 2026-09-17).
            var outstanding = remaining.Count(row => !PlayerHolds(client.Player, row.Item?.ItemTemplateId ?? 0));

            Logger.WriteLog(LogType.Debug,
                $"Content container {entityId} settled for {client.Player.Name}: {open.Taken().Count} taken, {remaining.Count} left, {outstanding} still missing.");

            if (outstanding > 0)
                return;

            foreach (var binding in BindingsOfKind(ObjectiveBindingKind.LootAll).Where(binding => binding.PlacementId == ContentUsablesPlacementId(mapChannel, entityId)))
                Missions.CompleteBoundObjective(client, binding.MissionId, binding.ObjectiveId, ObjectiveBindingKind.LootAll);
        }

        /// <summary>
        /// Whether the player is already carrying or wearing an item of that template, in any inventory.
        /// </summary>
        private static bool PlayerHolds(Manifestation player, uint itemTemplateId)
        {
            if (player == null || itemTemplateId == 0)
                return false;

            foreach (var inventory in new[] { player.Inventory.PersonalInventory, player.Inventory.EquippedInventory })
                foreach (var entityId in inventory ?? new List<ulong>())
                {
                    var item = EntityManager.Instance.GetItem(entityId);

                    if (item?.ItemTemplate != null && item.ItemTemplate.ItemTemplateId == itemTemplateId)
                        return true;
                }

            return false;
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
        /// Takes a fixed amount off the player: armour absorbs first, health takes the rest, and zero health kills
        /// with the same announcement a missile kill gets. The content layer's detonation self-damage uses it.
        /// </summary>
        private void DamagePlayer(Client client, int amount)
        {
            var player = client.Player;
            if (player.State == CharacterState.Dead)
                return;

            // Armour absorbs first, exactly as a missile hit does; a character without the attribute simply
            // takes the whole amount on health.
            var absorbed = 0;
            if (player.Attributes.TryGetValue(Attributes.Armor, out var armor))
            {
                absorbed = Math.Min(amount, armor.Current);
                armor.Current -= absorbed;
                client.CallMethod(player.EntityId, new UpdateArmorPacket(armor, 0));
            }

            if (!player.Attributes.TryGetValue(Attributes.Health, out var health))
                return;

            health.Current -= Math.Min(amount - absorbed, health.Current);
            client.CallMethod(player.EntityId, new UpdateHealthPacket(health, 0));

            Logger.WriteLog(LogType.Debug,
                $"{player.Name} takes {amount} from a content action ({absorbed} on armour, {health.Current} health left).");

            if (health.Current == 0)
            {
                player.State = CharacterState.Dead;
                PlayerDeathManager.Instance.AnnounceDeath(player.MapChannel, player, null);
            }
        }

        /// <summary>
        /// Sends the creature of a placement to a location: the content layer's scripted walk. The creature is
        /// the one materialized for that placement in the map the player is on, so a rule that fires for a
        /// player moves the NPCs of that player's own instance.
        /// </summary>
        private void SendCreatureToLocation(Client client, uint placementId, uint locationId)
        {
            if (!Content.Catalog.Locations.TryGetValue(locationId, out var location))
            {
                Logger.WriteLog(LogType.Error, $"creature move to unknown location {locationId} ignored.");
                return;
            }

            var mapChannel = client?.Player?.MapChannel;
            var creature = mapChannel?.MapCellInfo?.Cells.Values
                .SelectMany(cell => cell.CreatureList)
                .FirstOrDefault(candidate => candidate.ContentPlacementId == placementId);

            if (creature == null)
            {
                Logger.WriteLog(LogType.Error,
                    $"creature move: placement {placementId} has no creature in context {mapChannel?.MapInfo?.MapContextId}; ignored.");
                return;
            }

            var destination = new Vector3((float)location.PosX, (float)location.PosY, (float)location.PosZ);
            if (BehaviorManager.Instance.WalkTo(creature, destination))
                Logger.WriteLog(LogType.Debug,
                    $"{creature.Name} (placement {placementId}) walks to location {locationId} ({destination.X:0.#}, {destination.Y:0.#}, {destination.Z:0.#}).");
        }

        /// <summary>
        /// Completes the logos_recovered bindings for the Logos shrine the player just activated. The binding's
        /// placement_id carries the <c>logos</c> row id, because shrines are Logos dynamic objects and not
        /// content placements; DynamicObjectManager.LogosRecovery passes the id it already resolved.
        /// </summary>
        public void CompleteLogosBoundObjectives(Client client, uint logosId)
        {
            if (client == null || logosId == 0)
                return;

            foreach (var binding in BindingsOfKind(ObjectiveBindingKind.LogosRecovered)
                         .Where(binding => binding.PlacementId == logosId).ToList())
                Missions.CompleteBoundObjective(client, binding.MissionId, binding.ObjectiveId, ObjectiveBindingKind.LogosRecovered);
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

                ProbeAreas(player, contextId, current);

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

                        // An escort objective ("Take Milpas to Apirka") is not met by the player arriving alone: the
                        // creature being escorted has to be there too. Without this the objective would complete from
                        // across the zone, and the escort would be decoration.
                        if (!EscortInside(mapChannel, missionId, area, client) )
                            continue;

                        Missions.CompleteBoundObjective(client, missionId, objectiveId, ObjectiveBindingKind.AreaEntered);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// True when the mission's escort creatures are inside the area. A mission with no escort placement is
        /// unaffected, so this only touches the escort objectives.
        /// </summary>
        private bool EscortInside(MapChannel mapChannel, uint missionId, ContentAreaEntry area, Client client)
        {
            var escorts = EscortPositions(mapChannel, missionId);
            if (escorts.Count == 0)
                return true;

            var inside = ContentEscort.AllInside(escorts, area);
            if (!inside)
            {
                // Keep the escort walking after its player while the objective waits on it.
                WorkEscorts(mapChannel, missionId, client);
            }

            return inside;
        }

        /// <summary>The positions of the creatures escorting a mission, on this channel.</summary>
        private List<Vector3> EscortPositions(MapChannel mapChannel, uint missionId)
        {
            var result = new List<Vector3>();
            if (mapChannel == null || Content?.Catalog == null)
                return result;

            foreach (var placement in Content.Catalog.Placements.Values)
            {
                if (placement.Behavior != (byte)ContentPlacementBehavior.Escort || placement.EscortMissionId != missionId)
                    continue;

                var creature = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .FirstOrDefault(c => c.ContentPlacementId == placement.Id && c.State != CharacterState.Dead);

                if (creature != null)
                    result.Add(creature.Position);
            }

            return result;
        }

        /// <summary>
        /// Sends the mission's escorts after the player when they fall behind, at most every EscortRepathMs.
        /// </summary>
        private void WorkEscorts(MapChannel mapChannel, uint missionId, Client client)
        {
            var player = client?.Player;
            if (player == null || mapChannel == null || Content?.Catalog == null)
                return;

            var now = Environment.TickCount64;

            foreach (var placement in Content.Catalog.Placements.Values)
            {
                if (placement.Behavior != (byte)ContentPlacementBehavior.Escort || placement.EscortMissionId != missionId)
                    continue;

                var creature = mapChannel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                    .FirstOrDefault(c => c.ContentPlacementId == placement.Id && c.State != CharacterState.Dead);

                if (creature == null || creature.EscortRepathAt > now)
                    continue;

                var delta = creature.Position - player.Position;
                var distanceSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                if (distanceSquared <= ContentEscort.EscortFollowDistanceSquared)
                    continue;

                creature.EscortRepathAt = now + MissionContentManager.EscortRepathMs;
                BehaviorManager.Instance.WalkTo(creature, player.Position);
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

        /// <summary>
        /// Logs, once a second, how far a player is from the objective areas of their map when they are near one:
        /// horizontal distance against the radius, vertical against the half-height, and whether the area tests as
        /// entered. This answers "the trigger did not fire although the player was standing there".
        /// </summary>
        private void ProbeAreas(Manifestation player, uint contextId, Vector3 position)
        {
            var now = Environment.TickCount64;
            if (_lastAreaProbe.TryGetValue(player.EntityId, out var last) && now - last < 1000)
                return;

            foreach (var areas in _areaBindings.Values)
            {
                foreach (var area in areas)
                {
                    if (area.MapContextId != contextId)
                        continue;

                    var dx = position.X - (float)area.PosX;
                    var dz = position.Z - (float)area.PosZ;
                    var horizontal = MathF.Sqrt(dx * dx + dz * dz);
                    if (horizontal > 60f)
                        continue;

                    _lastAreaProbe[player.EntityId] = now;
                    var vertical = MathF.Abs(position.Y - (float)area.PosY);
                    Logger.WriteLog(LogType.Debug,
                        $"area probe: {player.Name} at ({position.X:0.#}, {position.Y:0.#}, {position.Z:0.#}) vs area {area.Id} " +
                        $"(map {area.MapContextId}): horizontal {horizontal:0.#} m of {area.Radius:0.#}, vertical {vertical:0.#} m " +
                        $"of {area.HalfHeight:0.#}, entered {SegmentEntersArea(position, position, area)}");
                    return;
                }
            }
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
