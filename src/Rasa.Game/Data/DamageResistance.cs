using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Data
{
    using Structures;

    // Original 1.16.5.0 shared/damageresistance.py. Live Deployment 14
    // independently confirms the positive-value curve for players and creatures.
    // This converts an already-resolved resistance value; it does not determine
    // that value, damage ordering, or rounding. See combat-damage-client-evidence.md.
    public static class DamageResistance
    {
        public static double GetDamageMultiplier(double resistanceValue)
        {
            if (resistanceValue < 0.0)
                return (100.0 - resistanceValue) / 100.0;
            return 1.0 / ((100.0 + 2.0 * resistanceValue) / 100.0);
        }

        public static double GetResistedPercent(double resistanceValue)
        {
            return (1.0 - GetDamageMultiplier(resistanceValue)) * 100.0;
        }

        /// <summary>
        /// A landed hit after the target's resistance to its damage type, rounded to the nearest point and never
        /// below one: the published curve never reaches immunity, and rounding a small hit against a high resistance
        /// to nothing would.
        /// </summary>
        public static int ScaleDamage(int damage, int resistance)
        {
            if (damage <= 0 || resistance <= 0)
                return damage;

            var scaled = (int)Math.Round(damage * GetDamageMultiplier(resistance), MidpointRounding.AwayFromZero);
            return scaled < 1 ? 1 : scaled;
        }

        /// <summary>
        /// A target's resistance to one damage type, summed from the resistances its equipment contributes. A
        /// missile with no damage type, or a target with none of that type, takes the hit in full.
        /// </summary>
        public static int ResistanceFor(IEnumerable<ResistanceData> resistances, DamageType? damageType)
        {
            if (resistances == null || damageType == null)
                return 0;

            return resistances
                .Where(resistance => resistance.ResistanceType == damageType.Value)
                .Select(resistance => resistance.ResistanceAmmount)
                .DefaultIfEmpty(0)
                .Sum();
        }
    }
}
