namespace Rasa.Data
{
    /// <summary>
    /// Regeneration, and what being in a fight costs it.
    ///
    /// The client's own numbers, from <c>shared/gameconstants.pyo</c> (sha256 e0fc260a...), which the client itself never
    /// reads - they were written for the server:
    /// <code>
    /// IN_COMBAT_REGEN_MODIFIER = 0.2
    /// NO_COMBAT_REGEN_MODIFIER = 1 / IN_COMBAT_REGEN_MODIFIER
    /// HEALTH_REGEN_PERIOD = 1
    /// POWER_REGEN_PERIOD = 1
    /// ARMOR_REGEN_PERIOD = 1
    /// </code>
    /// The client predicts regeneration itself: <c>ActorAttribute._EvaluatePredictedRefresh</c> adds
    /// <c>elapsed * refreshAmount / refreshPeriod</c>. <c>Actor.UpdateAttribute</c>, the receiver of UpdateHealth,
    /// UpdatePower and UpdateArmor, sets the period to 1 on every update (line 961), so the in-combat modifier can only
    /// ride on the amount: a period changed through AttributeInfo would be put back to 1 by the next hit.
    ///
    /// The server keeps the same count the client predicts - the rounded whole amount each second - so the two agree
    /// and a later update does not snap the bar back.
    /// </summary>
    public static class CombatRegen
    {
        /// <summary>IN_COMBAT_REGEN_MODIFIER.</summary>
        public const double InCombatModifier = 0.2;

        /// <summary>HEALTH_REGEN_PERIOD, POWER_REGEN_PERIOD and ARMOR_REGEN_PERIOD, in seconds: all three are 1.</summary>
        public const int RegenPeriodSeconds = 1;

        /// <summary>
        /// How long a player stays in combat after the last damage dealt or taken. Inferred: PlayerEnteredCombat and
        /// PlayerExitedCombat carry no arguments and no shipped constant names a timeout. 15 s follows Ellimist's
        /// ellimist/development commit 94b4485.
        /// </summary>
        public const long CombatTimeoutMs = 15000;
    }
}
