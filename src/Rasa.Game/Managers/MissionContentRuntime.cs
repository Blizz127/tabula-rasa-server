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

        private MissionManager _missions;

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

            _contextsWithAreas = new HashSet<uint>(_areaBindings.Values.SelectMany(areas => areas).Select(area => area.MapContextId));
        }

        #region Rules

        /// <summary>
        /// The actions of every live rule that matches the event and whose condition holds in the given state,
        /// in rule id order.
        /// </summary>
        public ContentReaction Plan(ContentEvent contentEvent, ContentState state)
        {
            var reaction = new ContentReaction();

            if (!_rulesByContext.TryGetValue(contentEvent.MapContextId, out var rules))
                return reaction;

            foreach (var rule in rules)
            {
                if (!contentEvent.Matches(rule))
                    continue;

                if (rule.ConditionId != 0 && !state.Evaluate(Content.Catalog.Conditions[rule.ConditionId]))
                    continue;

                reaction.Add(Content.Catalog.RuleActions[rule.Id]);
            }

            return reaction;
        }

        /// <summary>
        /// Stages the persistent actions into the triggering unit of work. Nothing is sent or changed in memory.
        /// </summary>
        public void Stage(ContentReaction reaction, ICharUnitOfWork unitOfWork, Manifestation player, ContentState state)
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
        }

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
                        client.CallMethod(SysEntity.ClientMethodId, new DisplayPlayerTutorialNotificationPacket(action.TutorialId));
                        break;

                    default:
                        throw new InvalidOperationException($"content action {(ContentRuleAction)action.Action} has no handler");
                }
            }
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
                c.DbId == placement.CreatureId && c.MapContextId == player.MapContextId);

            if (creature?.Npc == null || !MissionManager.IsInConversationRange(player, creature))
                return;

            if (!Missions.LoadedMissions.TryGetValue(action.MissionId, out var definition) ||
                !definition.IsDispensable ||
                player.Missions.ContainsKey(action.MissionId) ||
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

            CompleteUseBoundObjectives(owner, placement);
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
        public void React(Client client, ContentEvent contentEvent)
        {
            var player = client.Player;
            var state = new ContentState(player);
            var reaction = Plan(contentEvent, state);

            if (reaction.IsEmpty)
                return;

            if (reaction.Persistent.Count > 0)
            {
                try
                {
                    using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

                    Stage(reaction, unitOfWork, player, state);
                    unitOfWork.Complete();
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Content {contentEvent.Kind} in context {contentEvent.MapContextId}: could not save the reaction for character {player.Id}");
                    Logger.WriteLog(LogType.Error, e);
                    return;
                }

                Apply(client, reaction);
            }

            Present(client, reaction);
        }

        public void OnPlayerEnteredMap(Client client)
        {
            if (!MissionManager.IsInWorld(client))
                return;

            client.Player.LastContentSample = null;
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
