using System;

namespace Rasa.Data
{
    // Original client 1.16.5.0 actiondata.abilityData[(194, rank)].
    // These describe the effects; target selection, rolls and tick scheduling
    // remain separate server rules. See docs/lightning-effects-client-evidence.md.
    public sealed class LightningEffectData
    {
        public const int ArcEffectTypeId = 95;
        public const int StormEffectTypeId = 100;
        public const int StunEffectTypeId = 86;

        public uint Rank { get; }
        public int? PercentageChance { get; }
        public int? ArcRadius { get; }
        public int? ArcDamage { get; }
        public DamageType? ExtraDamageType { get; }
        public int? ExtraDamagePercent { get; }
        public int? StunChance { get; }
        public int? StunDurationSeconds { get; }
        public int? StormDurationMilliseconds { get; }
        public int? StormIntervalMilliseconds { get; }
        public int? StormDamageMinimum { get; }
        public int? StormDamageMaximum { get; }

        private LightningEffectData(uint rank, int level)
        {
            Rank = rank;
            if (rank >= 2)
            {
                // Property 37 is named PERCENTAGE_CHANCE. Its server-side
                // consumer has not been recovered; do not infer an arc roll.
                PercentageChance = 100;
                ArcRadius = (int)rank * 6;
                ArcDamage = AbilityScaling.ScaleActorAmount(rank == 3 ? 90 : 210,
                    level, AbilityScaling.ActorExponential);
            }
            if (rank >= 3)
            {
                ExtraDamageType = DamageType.Sonic;
                ExtraDamagePercent = 50;
            }
            if (rank >= 4)
            {
                StunChance = 50;
                StunDurationSeconds = 3;
            }
            if (rank == 5)
            {
                StormDurationMilliseconds = 6000;
                StormIntervalMilliseconds = 2000;
                StormDamageMinimum = AbilityScaling.ScaleActorAmount(60, level,
                    AbilityScaling.ActorExponential);
                StormDamageMaximum = AbilityScaling.ScaleActorAmount(90, level,
                    AbilityScaling.ActorExponential);
            }
        }

        public static LightningEffectData ForRank(uint rank, int level)
        {
            if (rank < 1 || rank > 5)
                throw new ArgumentOutOfRangeException(nameof(rank));
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));
            return new LightningEffectData(rank, level);
        }
    }
}
