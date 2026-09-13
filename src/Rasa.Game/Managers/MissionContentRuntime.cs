using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.ClientMethod.Server;
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
