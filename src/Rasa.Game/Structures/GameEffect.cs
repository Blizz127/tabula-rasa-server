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

        /// <summary>A ticking effect (a damage over time, Rage's squad pulse): OnTick runs every TickInterval ms.</summary>
        public int TickInterval { get; set; }
        public long NextTickTime { get; set; }
        public System.Action<GameEffect> OnTick { get; set; }
        /// <summary>Runs once when the effect is detached, for whatever reason.</summary>
        public System.Action<GameEffect> OnDetach { get; set; }
        /// <summary>Rage (235): percent added to the holder's damage, and resistance rating added against every type.</summary>
        public int DamageBonusPercent { get; set; }
        public int ResistRating { get; set; }
        /// <summary>Bio Augmentation (329): a flat amount added to one attribute while the effect lasts.</summary>
        public Data.Attributes? BonusAttribute { get; set; }
        public int AttributeBonus { get; set; }
    }
}
