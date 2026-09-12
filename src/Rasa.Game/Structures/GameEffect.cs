namespace Rasa.Structures
{
    public class GameEffect
    {
        // owner
        public int TypeId { get; set; } // effect class
        public int EffectId { get; set; } // effect id
        public uint EffectLevel { get; set; }
        // Server timers use milliseconds; client tooltip durations use seconds.
        public int Duration { get; set; }
        public long EffectTime { get; set; }
        public long NextDrainTime { get; set; }
        public int DrainInterval { get; set; }
        public int AdrenalineDrain { get; set; }
        public double MovementBeforeAttach { get; set; }
        public double MovementMultiplier { get; set; } = 1.0;
    }
}
