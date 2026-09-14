using System;

namespace Rasa.Data
{
    /// <summary>
    /// Creature kill rewards. Constants are the 1.16.5.0 client's shared/gameconstants.py; the
    /// formulas are fitted to the final-week footage. Tiers and data points:
    /// docs/evidence/kill-rewards.json.
    /// </summary>
    public static class KillRewardRules
    {
        /// <summary>gameconstants BASE_KILL_XP.</summary>
        public const double BaseKillXp = 62.5;

        /// <summary>gameconstants STREAK_BASE_PER_PARTY_MEMBER: kills that raise the streak one step (solo).</summary>
        public const int StreakKillsPerStep = 3;

        /// <summary>gameconstants STREAK_LEVEL_BASIS.</summary>
        public const int StreakLevelBasis = 10;

        /// <summary>gameconstants STREAK_MAX_VALUE.</summary>
        public const int StreakMaxValue = 5;

        /// <summary>gameconstants MAX_KILLING_STREAK_PRESTIGE_POINT_BONUS.</summary>
        public const int MaxStreakPrestige = 1;

        /// <summary>
        /// Time after a kill within which the next kill keeps the streak. Inferred: in the final-week
        /// footage a streak kill 12.5 s after the previous one kept the bonus and one 16.1 s after did
        /// not (A4-18, A4-28).
        /// </summary>
        public const long StreakWindowMs = 15000;

        /// <summary>
        /// Ten times the unrounded base kill experience for a creature of this level:
        /// 62.5 + 4.2·L + 0.2·L². Fitted to every clean kill in the footage (levels 1, 2, 4, 6, 7, 9),
        /// including the streak-doubled lines that only fit if the multiplier applies before
        /// truncation, as xpinfo.ApplyModifier does (int(gained * mod)).
        /// </summary>
        public static long BaseXpTimesTen(int creatureLevel)
        {
            var level = Math.Max(1L, creatureLevel);
            return 625 + 42 * level + 2 * level * level;
        }

        public static uint BaseExperience(int creatureLevel) => (uint)(BaseXpTimesTen(creatureLevel) / 10);

        /// <summary>int(base * streakMod), the base kept unrounded until the modifier is applied.</summary>
        public static uint Experience(int creatureLevel, int streakMod)
            => (uint)(BaseXpTimesTen(creatureLevel) * Math.Max(1, streakMod) / 10);

        /// <summary>Five credits per creature level: 5, 10, 20, 30, 35 and 45 at levels 1, 2, 4, 6, 7 and 9.</summary>
        public static int Credits(int creatureLevel) => 5 * Math.Max(1, creatureLevel);

        /// <summary>
        /// Highest streak step for a player level: one step per STREAK_LEVEL_BASIS levels, capped at
        /// STREAK_MAX_VALUE (inferred; levels 2-10 are observed reaching "max kill streak" at +100%).
        /// </summary>
        public static int MaxStreak(int playerLevel)
            => Math.Clamp((playerLevel + StreakLevelBasis - 1) / StreakLevelBasis, 1, StreakMaxValue);
    }
}
