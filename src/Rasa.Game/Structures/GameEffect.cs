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
        /// <summary>Shield Extender (SHIELDED): the shield pool this holder draws on, shared with the squad under it.</summary>
        public ShieldPool Shield { get; set; }
        /// <summary>Tactical Evasion's smoke screen: percent taken off incoming ranged damage.</summary>
        public int RangedReductionPercent { get; set; }
        /// <summary>A harmful effect: what Cure removes and its debuff guard keeps off.</summary>
        public bool IsDebuff { get; set; }
        /// <summary>Reflection: the damage types sent back, and the percent of each hit.</summary>
        public System.Collections.Generic.HashSet<Data.DamageType> ReflectTypes { get; set; }
        public int ReflectPercent { get; set; }
        /// <summary>Conversion: percent more damage taken, and what part of it heals the squad within ConversionRadius.</summary>
        public int DamageTakenPercent { get; set; }
        public int ConversionHealPercent { get; set; }
        public float ConversionRadius { get; set; }
        public Game.Client ConversionClient { get; set; }
        /// <summary>Regeneration Wave: percent added to the holder's regeneration rate.</summary>
        public int RegenBonusPercent { get; set; }
        /// <summary>Viral Conversion: virulent damage the holder deals becomes this type.</summary>
        public Data.DamageType? ConvertVirulentTo { get; set; }
        /// <summary>Disease (pumps 4-5): the holder does not regenerate, or cannot be healed.</summary>
        public bool StopsRegeneration { get; set; }
        public bool PreventsHealing { get; set; }
        /// <summary>Sacrifice: a creature this holder damages turns on it.</summary>
        public bool DrawsThreat { get; set; }
        /// <summary>Called Shot (arm): percent added to the holder's attack cooldowns.</summary>
        public int AttackDelayPercent { get; set; }
        /// <summary>Called Shot: the sniper whose next hit on the holder springs the called part, and what it does.</summary>
        public Actor CalledBy { get; set; }
        public System.Action<Missile> OnCalledHit { get; set; }
        /// <summary>Shredder Ammo: extra damage of this type added to the holder's weapon hits, once per interval.</summary>
        public Data.DamageType? ShredderType { get; set; }
        public int ShredderDamage { get; set; }
        public int ShredderIntervalMs { get; set; }
        public long ShredderReadyAt { get; set; }
        /// <summary>Polarity Field: resistance rating (negative: a vulnerability) against one type.</summary>
        public Data.DamageType? VulnerableType { get; set; }
        public int VulnerableRating { get; set; }
        /// <summary>Explosive Nanites: explosions left, and what each does; Busy stops an explosion setting off the next.</summary>
        public int NanitesLeft { get; set; }
        public System.Action<GameEffect> OnDamaged { get; set; }
        public bool Busy { get; set; }
        /// <summary>Feedback: what happens when the holder attacks.</summary>
        public System.Action<GameEffect> OnAttack { get; set; }
        /// <summary>Cloak Wave: creatures do not see the holder; any combat action ends it.</summary>
        public bool Stealth { get; set; }
        /// <summary>A server-side timer with no client effect behind it: nothing is sent when it attaches or ends.</summary>
        public bool ServerOnly { get; set; }
    }

    /// <summary>A shield bubble: the percent of each hit it takes, and how much it has left before it breaks.</summary>
    public class ShieldPool
    {
        public int Percent { get; set; }
        public int Remaining { get; set; }
        public System.Action Broken { get; set; }
    }
}
