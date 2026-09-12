using System;

namespace Rasa.Data
{
    public static class LightningAbilityData
    {
        public const int WindupMilliseconds = 500;
        public const int RecoveryMilliseconds = 700;
        public const int ReuseMilliseconds = 1200;
        private static readonly int[] PowerCosts = { 0, 25, 50, 75, 100, 150 };

        public static int GetPowerCost(uint rank)
        {
            if (rank < 1 || rank > 5)
                throw new ArgumentOutOfRangeException(nameof(rank));
            return PowerCosts[rank];
        }

        // generated/client/actiondata.py, abilityData[(194, rank)], properties
        // DAMAGE_AMOUNT_MIN (3), DAMAGE_AMOUNT_MAX (4), DAMAGE_SCALE_TYPE (40).
        // Only ranks 1–5 are trainable; rows 6–7 are separate client variants.
        // See docs/lightning-client-evidence.md for remaining combat work.
        public static (int Minimum, int Maximum) GetBaseDamageRange(uint rank, int level)
        {
            if (rank < 1 || rank > 5)
                throw new ArgumentOutOfRangeException(nameof(rank));

            var minimum = rank == 1 ? 180 : 240;
            var maximum = rank == 1 ? 240 : 300;
            return (
                AbilityScaling.ScaleActorAmount(minimum, level, AbilityScaling.ActorExponential),
                AbilityScaling.ScaleActorAmount(maximum, level, AbilityScaling.ActorExponential));
        }
    }
}
