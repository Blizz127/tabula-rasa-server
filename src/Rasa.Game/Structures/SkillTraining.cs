using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public static class SkillTraining
    {
        // Existing Rasa.NET skill/ability IDs; numeric IDs are protocol data.
        private static readonly int[] SkillIds = {
            1,8,14,19,20,21,22,23,24,
            25,26,28,30,31,32,34,35,
            36,37,39,40,43,47,48,49,
            50,54,55,57,58,63,66,67,
            68,72,73,77,79,80,82,89,
            92,102,110,111,113,114,121,135,
            136,147,148,149,150,151,152,153,
            154,155,156,157,158,159,160,161,
            162,163,164,165,166,172,173,174
        };
        private static readonly int[] AbilityIds =
        {
            -1, -1, -1, -1, 137, -1, -1, -1, -1, 178, 177, 158, -1, -1,
            197, 186, 188, 162, 187, -1, -1, 233, 234, -1, 194, -1, 10000005,
            -1, -1, -1, 301, -1, -1, 185, 251, 240, 302, 232, 229, -1,
            231, 305, 392, 252, 282, 381, 267, 298, 246, 253, 307, 393,
            281, 390, 295, 304, 386, 193, 385, 176, 260, 384, 383, 303,
            388, 389, 387, 380, 401, 430, 262, 421, 446
        };
        private static readonly int[] RankCosts = { 0, 1, 3, 6, 10, 15 };

        // Client 1.16.5.0 skilldata caps these at one. Its skill window has no
        // purchase controls for signatures; their grant path is separate.
        // See docs/final-client-skill-evidence.md for the original bytecode.
        private static readonly int[] SignatureSkillIds = { 20, 47, 92, 110, 149, 154, 156, 157 };

        // Class ownership and ancestry verified against client 1.16.5.0
        // skilldata and gameuiutil; see docs/final-client-skill-evidence.md.
        // Index is class ID. Inherited skills remain trainable after promotion.
        private static readonly int[][] ClassSkills =
        {
            Array.Empty<int>(),
            new[] { 1, 8, 19, 49, 165 },
            new[] { 21, 22, 25, 147 },
            new[] { 14, 30, 31, 36 },
            new[] { 24, 28, 39, 77, 164 },
            new[] { 48, 54, 55, 162, 163 },
            new[] { 57, 58, 111, 160, 174 },
            new[] { 34, 35, 66, 67, 173 },
            new[] { 40, 47, 79, 80, 155 },
            new[] { 23, 26, 43, 89, 92 },
            new[] { 50, 149, 150, 151, 166 },
            new[] { 82, 102, 110, 148, 161 },
            new[] { 20, 63, 113, 114, 159 },
            new[] { 32, 121, 157, 158, 172 },
            new[] { 37, 135, 152, 153, 154 },
            new[] { 68, 72, 73, 136, 156 }
        };
        private static readonly int[] ParentClasses = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };

        // Recruit skills begin at rank one. The five-point starting allowance
        // below accounts for these already assigned ranks, not unspent points.
        // See docs/new-character-client-evidence.md for dated original sources.
        public static Dictionary<SkillId, SkillsData> CreateInitialRecruitSkills()
        {
            var skills = new Dictionary<SkillId, SkillsData>();
            foreach (var id in ClassSkills[1])
            {
                var index = Array.IndexOf(SkillIds, id);
                skills.Add((SkillId)id, new SkillsData((SkillId)id, AbilityIds[index], 1));
            }
            return skills;
        }

        /// <summary>
        /// The ability tray a new Recruit starts with, as (drawer slot, ability id). Slot ids are the
        /// client's: abilitydrawerwindow numbers a slot as its position on the page plus page times
        /// NUM_ABILITY_DRAWER_SLOTS, from 0, so the tray's first slot is 0.
        ///
        /// Slot 0 is Lightning (194), observed: the first HUD frame of the boot-camp footage (A2-011,
        /// 7Lrst9SG3pk t=224.8) shows "slot 1 has a white lightning-bolt icon", and when the Eloh grants the
        /// Power Logos (A2-026, t=252.733) that slot flashes. Slot 1 is Sprint (401), inferred: the same
        /// frame shows slot 2 holding a dark red icon, and by level 2 (A3-068) slot 2 is Sprint's blue
        /// running figure with no drag seen in between. Without these a new character's tray is empty, and
        /// "Use your Lightning power on the Target Dummy" asks for a power the player cannot find
        /// (live report 2026-09-19).
        /// </summary>
        public static readonly (int Slot, int AbilityId)[] InitialRecruitAbilityDrawer =
        {
            (0, 194),   // Lightning, skill 49
            (1, 401)    // Sprint, skill 165
        };

        private static bool IsSignature(int skillId) => Array.IndexOf(SignatureSkillIds, skillId) >= 0;

        public static int GetMaximumRank(int skillId) => IsSignature(skillId) ? 1 : 5;

        public static bool TryGetSkillForAbility(int abilityId, out SkillId skillId)
        {
            var index = abilityId > 0 ? Array.IndexOf(AbilityIds, abilityId) : -1;
            skillId = index >= 0 ? (SkillId)SkillIds[index] : default;
            return index >= 0;
        }

        public static bool IsAvailableToClass(uint classId, int skillId)
        {
            if (classId == 0 || classId >= ClassSkills.Length)
                return false;

            for (var current = (int)classId; current != 0; current = ParentClasses[current])
                if (Array.IndexOf(ClassSkills[current], skillId) >= 0)
                    return true;
            return false;
        }

        public static int GetAvailablePoints(Manifestation player)
        {
            if (player.Level < 1)
                return 0;

            var points = (player.Level - 1) * 2L + 5;
            if (player.Level >= 5) points += 2;
            if (player.Level >= 15) points += 2;
            if (player.Level >= 30) points += 2;
            if (player.Level >= 50) points += 4;

            foreach (var skill in player.Skills.Values)
            {
                if (skill.SkillLevel < 0 || skill.SkillLevel > GetMaximumRank((int)skill.SkillId))
                    return 0;
                points -= RankCosts[skill.SkillLevel];
            }
            return (int)Math.Max(0, points);
        }

        public static bool TryPlan(Manifestation player, int[] skillIds, int[] ranks,
            out Dictionary<SkillId, SkillsData> changes)
        {
            changes = new Dictionary<SkillId, SkillsData>();
            if (skillIds == null || ranks == null || skillIds.Length != ranks.Length || skillIds.Length > SkillIds.Length)
                return false;

            var proposed = new Dictionary<SkillId, SkillsData>();
            var seen = new HashSet<int>();
            var available = GetAvailablePoints(player);
            for (var i = 0; i < skillIds.Length; i++)
            {
                var index = Array.IndexOf(SkillIds, skillIds[i]);
                if (index < 0 || !seen.Add(skillIds[i]) || !IsAvailableToClass(player.Class, skillIds[i]))
                    return false;

                var id = (SkillId)skillIds[i];
                var previous = player.Skills.TryGetValue(id, out var skill) ? skill.SkillLevel : 0;
                var rank = ranks[i];
                var maximumRank = GetMaximumRank(skillIds[i]);
                if (previous < 0 || previous > maximumRank || rank < previous || rank > maximumRank)
                    return false;

                if (rank > previous && IsSignature(skillIds[i]))
                    return false;

                available -= RankCosts[rank] - RankCosts[previous];
                if (available < 0)
                    return false;

                if (rank > previous)
                    proposed.Add(id, new SkillsData(id, AbilityIds[index], rank));
            }

            changes = proposed;
            return true;
        }
    }
}
