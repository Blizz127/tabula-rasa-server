using System;

namespace Rasa.Data
{
    /// <summary>
    /// Creature kill rewards. Constants are the 1.16.5.0 client's shared/gameconstants.py; the
    /// base formulas are fitted to the final-week footage, the squad share is the client's own
    /// experience-bar arithmetic, and the danger ramp is inferred. Tiers and data points:
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

        /// <summary>gameconstants XP_MOD_PER_PARTY_MEMBER 0.08, in percent: what each further squad member takes off every member's share.</summary>
        public const int XpModPerPartyMemberPercent = 8;

        /// <summary>gameconstants MAX_PARTY_SIZE.</summary>
        public const int MaxSquadSize = 6;

        /// <summary>gameconstants DANGER_PENALTY_VALUE 0.2, in percent: the experience lost per level of the danger ramp.</summary>
        public const int DangerPenaltyPercent = 20;

        /// <summary>gameconstants DANGER_PENALTY_LEVELDIFF_MIN.</summary>
        public const int DangerPenaltyLevelDiffMin = 5;

        /// <summary>gameconstants DANGER_PENALTY_LEVELDIFF_MAX.</summary>
        public const int DangerPenaltyLevelDiffMax = 9;

        /// <summary>
        /// gameconstants MIN_DISTANCE_FOR_KILL_CREDIT, metres. The client never reads it; using it as the
        /// range within which a squad member shares a kill is inferred from its name and TaRapedia's
        /// "so long as you are in range when it is killed" (Experience, 2008-10-06).
        /// </summary>
        public const float MinDistanceForKillCredit = 100f;

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

        /// <summary>
        /// Each member's share of a squad kill as a percentage of the solo experience: the client's own
        /// curXP, experiencebarwindow.OnXPEntered (line 222), 100 - (squad size - 1) · XP_MOD_PER_PARTY_MEMBER · 100.
        /// </summary>
        public static int SquadSharePercent(int squadSize)
            => 100 - (Math.Clamp(squadSize, 1, MaxSquadSize) - 1) * XpModPerPartyMemberPercent;

        /// <summary>
        /// xpinfo groupMod: the share over an even split, curXP / normalXP in experiencebarwindow.OnXPEntered
        /// (lines 221-223), so the client's "(+N% Group Bonus)" reads 84, 152, 204, 240 and 260 for two to six members.
        /// </summary>
        public static double GroupMod(int squadSize)
        {
            var size = Math.Clamp(squadSize, 1, MaxSquadSize);
            return size * SquadSharePercent(size) / 100.0;
        }

        /// <summary>
        /// The danger penalty, percent of the experience kept, for a player <c>playerLevel - creatureLevel</c>
        /// levels above the creature. Full below DANGER_PENALTY_LEVELDIFF_MIN, DANGER_PENALTY_VALUE less per
        /// level from there to 20% at DANGER_PENALTY_LEVELDIFF_MAX, none beyond. The three constants are
        /// original; the linear ramp is inferred (the client never reads them): it is the one use of the three
        /// that agrees with TaRapedia's "ten or more levels above ... no experience" (Experience, 2008-10-06) and
        /// with full experience observed one level above (B3-047). docs/evidence/kill-rewards.json xp.danger.
        /// </summary>
        public static int DangerPercent(int playerLevel, int creatureLevel)
        {
            var above = playerLevel - Math.Max(1, creatureLevel);
            if (above <= DangerPenaltyLevelDiffMin)
                return 100;
            if (above > DangerPenaltyLevelDiffMax)
                return 0;
            return 100 - DangerPenaltyPercent * (above - DangerPenaltyLevelDiffMin);
        }

        /// <summary>
        /// One member's experience for a kill. The base is split evenly (TaRapedia: "(base experience) /
        /// (squad members) * (squad multiplier)"), then each modifier truncates as xpinfo.ApplyModifier does:
        /// streak, group bonus, danger penalty. The solo fit only fixes that the streak sees the unrounded
        /// base and that nothing truncates it first; the order after that is inferred, and a group bonus
        /// applied before the streak or on the unrounded share would pay a few points more
        /// (GAP-XP-MODIFIER-ORDER). Solo and within five levels this is exactly <see cref="Experience(int, int)"/>.
        /// </summary>
        public static KillExperience Experience(int creatureLevel, int streakMod, int squadSize, int playerLevel)
        {
            var size = Math.Clamp(squadSize, 1, MaxSquadSize);
            var streak = Math.Max(1, streakMod);
            var base10 = BaseXpTimesTen(creatureLevel);

            var gained = base10 * streak / (10L * size);
            if (size > 1)
                gained = gained * size * SquadSharePercent(size) / 100;

            var danger = DangerPercent(playerLevel, creatureLevel);
            if (danger < 100)
                gained = gained * danger / 100;

            return new KillExperience((uint)gained, (uint)(base10 / (10L * size)), streak, GroupMod(size));
        }

        /// <summary>Five credits per creature level: 5, 10, 20, 30, 35 and 45 at levels 1, 2, 4, 6, 7 and 9.</summary>
        public static int Credits(int creatureLevel) => 5 * Math.Max(1, creatureLevel);

        /// <summary>
        /// Highest streak step for a player level: one step per STREAK_LEVEL_BASIS levels, capped at
        /// STREAK_MAX_VALUE (inferred; levels 2-10 are observed reaching "max kill streak" at +100%).
        /// </summary>
        public static int MaxStreak(int playerLevel)
            => Math.Clamp((playerLevel + StreakLevelBasis - 1) / StreakLevelBasis, 1, StreakMaxValue);
    }

    /// <summary>What one kill pays one player: xpinfo's gained, baseGained, streakMod, groupMod and wasCritKill.</summary>
    public readonly struct KillExperience
    {
        public uint Gained { get; }
        public uint BaseGained { get; }
        public int StreakMod { get; }
        public double GroupMod { get; }
        public bool WasCritKill { get; }

        public KillExperience(uint gained, uint baseGained, int streakMod, double groupMod, bool wasCritKill = false)
        {
            Gained = gained;
            BaseGained = baseGained;
            StreakMod = streakMod;
            GroupMod = groupMod;
            WasCritKill = wasCritKill;
        }

        public KillExperience AsCritKill() => new(Gained, BaseGained, StreakMod, GroupMod, true);
    }
}
