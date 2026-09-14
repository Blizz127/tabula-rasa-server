using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;

    /// <summary>
    /// One character's persisted progress on a mission. Only revealed
    /// objectives are stored; a required objective that has not been revealed
    /// keeps the mission from becoming completeable.
    /// </summary>
    public class PlayerMission
    {
        public uint MissionId { get; set; }
        public MissionState State { get; set; }
        public uint ChangeTime { get; set; }
        public Dictionary<uint, MissionObjectiveState> Objectives { get; } = new();
        // (objectiveId, counterId) -> current value; loaded with the mission log.
        public Dictionary<(uint ObjectiveId, byte CounterId), int> Counters { get; } = new();

        public bool IsInLog => State == MissionState.Active || State == MissionState.Success;

        public bool IsCompleteable(Mission definition)
        {
            // Fail closed: a definition with nothing required never completes by itself.
            if (State != MissionState.Active || !definition.Objectives.Values.Any(objective => objective.IsRequired == true))
                return false;

            foreach (var objective in definition.Objectives.Values)
            {
                if (objective.IsRequired != true)
                    continue;

                if (!Objectives.TryGetValue(objective.ObjectiveId, out var status) || status != MissionObjectiveState.Completed)
                    return false;
            }

            return true;
        }

        public MissionInfo ToMissionInfo(Mission definition)
        {
            var info = new MissionInfo
            {
                MissionState = State,
                Completeable = IsCompleteable(definition),
                MissionConstantData = definition.MissionConstantData,
                ChangeTime = (int)ChangeTime
            };

            if (!IsInLog)
                return info;

            foreach (var objective in definition.ObjectivesInOrder)
            {
                if (!Objectives.TryGetValue(objective.ObjectiveId, out var status))
                    continue;

                info.ObjectivesList.Add(new MissionObjective
                {
                    ObjectiveId = objective.ObjectiveId,
                    ObjectiveStatus = (uint)status,
                    // Unknown ordinal/required flag mirror the unknown-objective
                    // fallback below; such definitions are unoffered, so a saved
                    // mission only reaches this through a definition change.
                    Ordinal = objective.Ordinal ?? uint.MaxValue,
                    IsRequired = objective.IsRequired == true
                });
            }

            // Objectives the definition no longer knows are still reported so the
            // saved state stays visible instead of silently disappearing.
            foreach (var entry in Objectives.Where(entry => !definition.Objectives.ContainsKey(entry.Key)).OrderBy(entry => entry.Key))
                info.ObjectivesList.Add(new MissionObjective
                {
                    ObjectiveId = entry.Key,
                    ObjectiveStatus = (uint)entry.Value,
                    Ordinal = uint.MaxValue,
                    IsRequired = false
                });

            return info;
        }
    }
}
