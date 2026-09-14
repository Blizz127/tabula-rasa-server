using System;

namespace Rasa.Data
{
    /// <summary>
    /// Resuscitation Trauma. Constants are the 1.16.5.0 client's shared/gameconstants.py; the rules
    /// are client text (tooltip 506, help 5687) and the official D10.6 live notes. Tiers:
    /// docs/player-death-implementation.md.
    /// </summary>
    public static class DeathPenaltyRules
    {
        /// <summary>gameeffectdata: gameeffects.rezsickness.RezSickness.</summary>
        public const int RezSicknessEffectType = 196;

        /// <summary>gameeffectdata: gameeffects.rezsickness.RezSicknessNoHeal.</summary>
        public const int RezSicknessNoHealEffectType = 195;

        /// <summary>gameconstants DEATH_PENALTY_MIN_LEVEL.</summary>
        public const int MinLevel = 5;

        /// <summary>gameconstants REZ_SICKNESS_MAX_STACK.</summary>
        public const int MaxStacks = 3;

        /// <summary>gameconstants REZ_SICKNESS_PENALTY (-20.0 percent per stack).</summary>
        public const double PenaltyPerStack = 0.20;

        /// <summary>gameconstants REZ_SICKNESS_DURATION (120 s).</summary>
        public const int DurationPerStackMs = 120000;

        /// <summary>gameconstants REZ_SICKNESS_MAX_DURATION (360 s).</summary>
        public const int MaxDurationMs = 360000;

        /// <summary>gameconstants REZ_SICKNESS_NO_HEAL_DURATION (30 s).</summary>
        public const int NoHealDurationMs = 30000;

        /// <summary>
        /// Attribute points removed at a trauma stack: "Reduces attributes by 20% ... up to maximum of
        /// 60%" (tooltip 506). Truncated to whole points (inferred; the rounding is unrecovered).
        /// </summary>
        public static int AttributePenalty(int attributeTotal, int stacks)
            => (int)(Math.Max(0, attributeTotal) * PenaltyPerStack * Math.Clamp(stacks, 0, MaxStacks));

        /// <summary>
        /// Remaining time after another resuscitation while traumatized: "an additional two minutes for
        /// each resuscitation in the same time frame up to a maximum of six minutes" (help 5687).
        /// </summary>
        public static int NextDurationMs(long remainingMs)
            => (int)Math.Min(MaxDurationMs, Math.Max(0, remainingMs) + DurationPerStackMs);
    }
}
