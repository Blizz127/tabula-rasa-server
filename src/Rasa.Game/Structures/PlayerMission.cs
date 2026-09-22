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
        // objectiveId -> running objective timer; loaded with the mission log.
        public Dictionary<uint, ObjectiveTimer> Timers { get; } = new();

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

        public MissionInfo ToMissionInfo(Mission definition) => ToMissionInfo(definition, System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        /// <param name="nowMs">Unix time in milliseconds, for the remaining time of running objective timers.</param>
        /// <param name="mapContextId">
        /// The map the player is standing on, for the derived objective markers. The client's indicator tuple
        /// carries no map and mapwindow._PlaceWidget draws every marker with the open map's offset and scale, so
        /// a marker for another map would land at a wrong spot on this one; 0 means the map is not known and
        /// filters nothing. The mission log is resent on every map entry (MapChannelManager calls
        /// MissionManager.SendMissionStatusInfo), so the set follows the player.
        /// </param>
        public MissionInfo ToMissionInfo(Mission definition, long nowMs, uint mapContextId = 0)
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
                    IsRequired = objective.IsRequired == true,
                    TimeRemaining = status == MissionObjectiveState.Incomplete && definition.Timers.ContainsKey(objective.ObjectiveId) &&
                                    Timers.TryGetValue(objective.ObjectiveId, out var timer)
                        ? timer.SecondsRemaining(nowMs)
                        : null,
                    CounterDict = CounterDict(definition, objective.ObjectiveId),
                    IndicatorList = IndicatorList(definition, objective.ObjectiveId, mapContextId)
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

        // counterId -> (count, initial, target): the saved value, or the initial value before the first change, so
        // the tracker shows "Boss Eliminated: 0 / 1" as soon as the objective is revealed (A4-21).
        private Dictionary<uint, MissionObjectiveGenericCounter> CounterDict(Mission definition, uint objectiveId)
        {
            var counters = new Dictionary<uint, MissionObjectiveGenericCounter>();

            if (definition.Counters.TryGetValue(objectiveId, out var rows))
                foreach (var row in rows)
                    counters[row.CounterId] = new MissionObjectiveGenericCounter
                    {
                        Count = Counters.TryGetValue((objectiveId, row.CounterId), out var value) ? value : row.InitialValue,
                        InitialCount = row.InitialValue,
                        TargetCount = row.TargetValue
                    };

            return counters;
        }

        // Sent for every listed objective; missionlog.pyo _UpdateIndicators itself clears those of completed and failed ones.
        //
        // A stored npc_mission_objective_indicator row is recovered evidence and always wins. Only an objective
        // with no row of its own falls back to the markers derived from where the world says it is finished
        // (MissionMapIndicators), and those are filtered to the player's map: the client's tuple has no map and
        // mapwindow._PlaceWidget would draw an off-map marker at a wrong spot on the open one.
        private static List<MissionIndicator> IndicatorList(Mission definition, uint objectiveId, uint mapContextId)
        {
            if (definition.Indicators.TryGetValue(objectiveId, out var rows))
                return rows.Select(row => new MissionIndicator
                {
                    Position = new System.Numerics.Vector3((float)row.PosX, (float)row.PosY, (float)row.PosZ),
                    Radius = row.Radius,
                    IndicatorId = row.IndicatorId,
                    Show3DEffect = row.Show3d
                }).ToList();

            if (!definition.DerivedIndicators.TryGetValue(objectiveId, out var derived))
                return new List<MissionIndicator>();

            return derived.Where(indicator => mapContextId == 0 || indicator.MapContextId == mapContextId).ToList();
        }
    }

    /// <summary>
    /// A running objective timer in wall-clock mode (owner decision OD-5): the deadline is
    /// <see cref="AnchorMs"/> + <see cref="RemainingMs"/> in Unix milliseconds and does not stop
    /// while the character is offline. A disarmed timer keeps counting on the client but never
    /// fails the objective (the planted bomb, build plan S5).
    /// </summary>
    public sealed class ObjectiveTimer
    {
        public long RemainingMs { get; set; }
        public long AnchorMs { get; set; }
        public bool Disarmed { get; set; }

        public long DeadlineMs => AnchorMs + RemainingMs;

        public bool HasExpired(long nowMs) => !Disarmed && nowMs >= DeadlineMs;

        /// <summary>
        /// The objective's timeRemaining in whole seconds: missionlog.pyo adds it to gameclient.Time(),
        /// which gameuiutil.FormatTextForTime formats as seconds. Never 0 while the objective is
        /// still incomplete, because the client would show it as already expired.
        /// </summary>
        public uint SecondsRemaining(long nowMs)
        {
            var remainingMs = DeadlineMs - nowMs;
            return remainingMs <= 1000 ? 1u : (uint)System.Math.Min(uint.MaxValue, (remainingMs + 999) / 1000);
        }
    }
}
