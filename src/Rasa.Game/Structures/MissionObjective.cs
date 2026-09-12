using System.Collections.Generic;

namespace Rasa.Structures
{
    public class MissionObjective
    {
        public uint ObjectiveId { get; set; }
        public uint ObjectiveStatus { get; set; }
        public uint Ordinal { get; set; }
        // The client treats None as untimed, while zero expires immediately.
        public uint? TimeRemaining { get; set; }
        public Dictionary<uint, MissionObjectiveGenericCounter> CounterDict { get; set; } = new();
        public Dictionary<uint, MissionObjectiveCounter> ItemCounters = new();  // itemClassId = (count, countMax)
        public bool IsRequired { get; set; }
        public List<MissionIndicator> IndicatorList { get; set; } = new();
    }

    public class MissionObjectiveGenericCounter
    {
        public int Count { get; set; }
        public int InitialCount { get; set; }
        public int TargetCount { get; set; }
    }
}
