using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures.Content
{
    using Data;
    using Structures.World;

    /// <summary>
    /// A character's mission, objective and Logos state as seen by content conditions. Planned changes
    /// of the triggering transaction are overlaid, so rules react to the state being committed.
    /// </summary>
    public sealed class ContentState
    {
        private readonly Manifestation _player;
        private readonly Dictionary<uint, MissionState?> _missions = new();
        private readonly Dictionary<(uint Mission, uint Objective), MissionObjectiveState> _objectives = new();
        private readonly HashSet<uint> _logos = new();

        public ContentState(Manifestation player)
        {
            _player = player;
        }

        public void PlanMission(uint missionId, MissionState? state) => _missions[missionId] = state;

        public void PlanObjective(uint missionId, uint objectiveId, MissionObjectiveState state) => _objectives[(missionId, objectiveId)] = state;

        public void PlanLogos(uint logosId) => _logos.Add(logosId);

        private readonly Dictionary<(uint MapContextId, string Key), int?> _facts = new();

        public void PlanFact(uint mapContextId, string key, int? value) => _facts[(mapContextId, key)] = value;

        /// <summary>A fact of the player's current context; an absent fact reads as 0.</summary>
        public int FactValue(string key)
        {
            var slot = (_player.MapContextId, key);
            if (_facts.TryGetValue(slot, out var planned))
                return planned ?? 0;
            return _player.ContentFacts.TryGetValue(slot, out var value) ? value : 0;
        }

        private readonly Dictionary<(uint Mission, uint Objective, byte Counter), int> _counters = new();

        public void PlanCounter(uint missionId, uint objectiveId, byte counterId, int value)
            => _counters[(missionId, objectiveId, counterId)] = value;

        public MissionState? MissionState(uint missionId)
        {
            if (_missions.TryGetValue(missionId, out var planned))
                return planned;

            return _player.Missions.TryGetValue(missionId, out var mission) ? mission.State : null;
        }

        public MissionObjectiveState? ObjectiveState(uint missionId, uint objectiveId)
        {
            if (_objectives.TryGetValue((missionId, objectiveId), out var planned))
                return planned;

            if (_missions.TryGetValue(missionId, out var plannedMission) && plannedMission == null)
                return null;

            return _player.Missions.TryGetValue(missionId, out var mission) && mission.Objectives.TryGetValue(objectiveId, out var status) ? status : null;
        }

        public bool HasLogos(uint logosId) => _logos.Contains(logosId) || _player.Logos.Contains(logosId);

        public bool Evaluate(IReadOnlyList<ContentConditionEntry> terms)
        {
            // True if any or-group has all of its terms true.
            return terms.GroupBy(term => term.OrGroup).Any(group => group.All(term => Evaluate(term) != term.Negate));
        }

        private bool Evaluate(ContentConditionEntry term)
        {
            switch ((ContentConditionKind)term.Kind)
            {
                case ContentConditionKind.MissionAbsent:
                    return MissionState(term.MissionId) == null;
                case ContentConditionKind.MissionStateIs:
                    return MissionState(term.MissionId) is { } missionState && (uint)missionState == term.State;
                case ContentConditionKind.ObjectiveStateIs:
                    return ObjectiveState(term.MissionId, term.ObjectiveId) is { } objectiveState && (uint)objectiveState == term.State;
                case ContentConditionKind.FactEquals:
                    return FactValue(term.FactKey) == term.Value;
                case ContentConditionKind.HasLogos:
                    return term.Value > 0 && HasLogos((uint)term.Value);
                default:
                    // Unimplemented kinds are withheld at load and never reach evaluation.
                    return false;
            }
        }
    }

    /// <summary>
    /// The matched rules of one committed change, split by when their actions happen: persistent actions are
    /// staged into the triggering transaction and applied after it commits; presentation actions follow.
    /// </summary>
    public sealed class ContentReaction
    {
        public List<ContentRuleActionEntry> Persistent { get; } = new();
        public List<ContentRuleActionEntry> Presentation { get; } = new();

        // The rules that matched, in rule id order.
        public List<uint> MatchedRuleIds { get; } = new();

        // Filled by staging: the Logos actually granted (a character that already owns one is not granted it again).
        public List<(uint LogosId, LogosGrantProtocol Protocol)> GrantedLogos { get; } = new();

        // Filled by staging: the experience and credits of grant_rewards actions.
        public uint GrantedExperience { get; set; }
        public int GrantedCredits { get; set; }

        // Filled by staging: a committed transfer destination and the account's skip-boot-camp flag.
        public ContentLocationEntry Transfer { get; set; }
        public bool SkipBootcampGranted { get; set; }

        // Filled by staging: fact changes in action order (null value = cleared).
        public List<(uint MapContextId, string Key, int? Value)> FactChanges { get; } = new();

        public bool IsEmpty => Persistent.Count == 0 && Presentation.Count == 0;

        public static bool IsPersistent(ContentRuleAction action) => action is ContentRuleAction.GrantLogos or ContentRuleAction.GrantRewards
            or ContentRuleAction.GrantItemSet or ContentRuleAction.SetFact or ContentRuleAction.ClearFact or ContentRuleAction.TransferToLocation
            or ContentRuleAction.SetAccountSkipBootcamp;

        public void Add(IEnumerable<ContentRuleActionEntry> actions)
        {
            foreach (var action in actions)
                (IsPersistent((ContentRuleAction)action.Action) ? Persistent : Presentation).Add(action);
        }
    }

    /// <summary>
    /// The event a rule reacts to, with the values its filters compare against (0 = not applicable).
    /// </summary>
    public readonly struct ContentEvent
    {
        public ContentRuleEvent Kind { get; }
        public uint MapContextId { get; }
        public uint MissionId { get; }
        public uint ObjectiveId { get; }
        public uint AreaId { get; }
        public uint PlacementId { get; }
        public uint StateId { get; }

        public ContentEvent(ContentRuleEvent kind, uint mapContextId, uint missionId = 0, uint objectiveId = 0, uint areaId = 0, uint placementId = 0, uint stateId = 0)
        {
            Kind = kind;
            MapContextId = mapContextId;
            MissionId = missionId;
            ObjectiveId = objectiveId;
            AreaId = areaId;
            PlacementId = placementId;
            StateId = stateId;
        }

        public bool Matches(ContentRuleEntry rule)
        {
            return rule.Event == (byte)Kind && rule.MapContextId == MapContextId &&
                   (rule.MissionId == 0 || rule.MissionId == MissionId) &&
                   (rule.ObjectiveId == 0 || rule.ObjectiveId == ObjectiveId) &&
                   (rule.AreaId == 0 || rule.AreaId == AreaId) &&
                   (rule.PlacementId == 0 || rule.PlacementId == PlacementId) &&
                   (rule.StateId == 0 || rule.StateId == StateId);
        }
    }
}
