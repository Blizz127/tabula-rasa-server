namespace Rasa.Data
{
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
    }
}
