using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;

    public static class AbilityRequirements
    {
        // Client 1.16.5.0 logosstone.logosSequences, joined to the active skill
        // catalog. Membership is required for use, not for purchasing ranks.
        // Provenance: docs/ability-use-client-evidence.md.
        private static readonly Dictionary<int, uint[]> RequiredLogos = new()
        {
            { 137, new uint[] { 48U, 6U, 131U, 125U } },
            { 178, new uint[] { 24U, 1U } },
            { 177, new uint[] { 25U, 1U, 38U, 6U } },
            { 158, new uint[] { 49U, 3U, 50U } },
            { 197, new uint[] { 52U, 34U, 6U, 9U } },
            { 186, new uint[] { 10U, 17U, 41U } },
            { 188, new uint[] { 1U, 15U, 17U } },
            { 162, new uint[] { 6U, 28U } },
            { 187, new uint[] { 331U, 333U, 43U, 6U } },
            { 233, new uint[] { 6U, 10U, 17U, 14U } },
            { 234, new uint[] { 48U, 50U, 3U, 45U } },
            { 194, new uint[] { 23U } },
            { 10000005, new uint[] { 1U, 7U, 4U } },
            { 301, new uint[] { 50U, 3U, 6U, 1U } },
            { 185, new uint[] { 52U, 43U, 41U, 14U } },
            { 251, new uint[] { 6U, 1U, 45U, 125U } },
            { 240, new uint[] { 52U, 43U, 33U, 300U } },
            { 302, new uint[] { 38U, 24U, 2U } },
            { 232, new uint[] { 34U, 4U, 6U, 1U } },
            { 229, new uint[] { 50U, 46U, 1U, 6U } },
            { 231, new uint[] { 373U, 1U, 9U, 52U } },
            { 305, new uint[] { 48U, 6U, 7U, 45U } },
            { 392, new uint[] { 38U, 331U, 49U, 216U } },
            { 252, new uint[] { 48U, 10U, 188U, 45U } },
            { 282, new uint[] { 34U, 6U, 55U } },
            { 381, new uint[] { 6U, 9U, 117U, 55U } },
            { 267, new uint[] { 384U, 55U, 38U, 6U } },
            { 298, new uint[] { 14U, 384U, 34U, 52U } },
            { 246, new uint[] { 6U, 9U, 322U, 347U } },
            { 253, new uint[] { 38U, 14U, 52U, 53U } },
            { 307, new uint[] { 2U, 10U } },
            { 393, new uint[] { 9U, 2U, 14U, 41U } },
            { 281, new uint[] { 48U, 10U, 2U, 14U } },
            { 390, new uint[] { 393U, 6U, 146U, 2U } },
            { 295, new uint[] { 18U, 49U, 214U, 107U } },
            { 304, new uint[] { 41U, 9U, 56U, 300U } },
            { 386, new uint[] { 38U, 7U, 6U, 146U } },
            { 193, new uint[] { 48U, 10U, 17U, 45U } },
            { 385, new uint[] { 38U, 6U, 17U, 14U } },
            { 176, new uint[] { 48U, 43U, 33U, 300U } },
            { 260, new uint[] { 48U, 275U, 7U, 14U } },
            { 384, new uint[] { 55U, 6U, 52U, 34U } },
            { 383, new uint[] { 9U, 6U, 10U, 20U } },
            { 303, new uint[] { 34U, 41U, 4U } },
            { 388, new uint[] { 7U, 331U, 33U, 146U } },
            { 389, new uint[] { 14U, 52U, 53U } },
            { 387, new uint[] { 51U, 1U, 6U } },
            { 380, new uint[] { 38U, 45U, 6U } },
            { 401, new uint[] {  } },
            { 430, new uint[] { 41U, 2U, 49U, 131U } },
            { 262, new uint[] { 32U, 34U, 43U, 53U } },
            { 421, new uint[] { 10U, 14U, 23U } },
            { 446, new uint[] { 18U, 49U, 7U } },
        };

        public static bool CanUseSkillAbility(Manifestation player, ActionId abilityId, int rank)
        {
            if (player == null || player.State == CharacterState.Dead || rank < 1 ||
                !SkillTraining.TryGetSkillForAbility((int)abilityId, out var skillId) ||
                !SkillTraining.IsAvailableToClass(player.Class, (int)skillId) ||
                !player.Skills.TryGetValue(skillId, out var learned) ||
                learned.SkillLevel < rank || learned.SkillLevel > SkillTraining.GetMaximumRank((int)skillId))
                return false;

            foreach (var logosId in RequiredLogos[(int)abilityId])
                if (!player.Logos.Contains(logosId))
                    return false;
            return true;
        }
    }
}
