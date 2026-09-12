using System;

namespace Rasa.Data
{
    // Original shared/scaling.py, client 1.16.5.0. This scales a base amount;
    // it does not include damage mitigation, critical hits or effect modifiers.
    public static class AbilityScaling
    {
        public const int ActorLinear = 1;
        public const int ActorExponential = 2;

        public static int ScaleActorAmount(int baseAmount, int level, int? scaleType)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));

            switch (scaleType)
            {
                case ActorLinear:
                    return (int)(baseAmount / 100.0 * (100 + (level - 1) * 100.0 / 3.0));
                case ActorExponential:
                    return (int)(baseAmount * Math.Pow(2, (level - 1) / 8.0));
                default:
                    return baseAmount;
            }
        }
    }
}
