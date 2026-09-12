using Rasa.Structures;

namespace Rasa.Data
{
    public enum EquipmentRequirementFailure
    {
        None, MinimumLevel, MaximumLevel, Body, Mind, Spirit, Race, Skill, Broken
    }

    public static class EquipmentRequirements
    {
        // Original Item.GetCondition uses Python 2 integer floor division for
        // integer ItemInfo/ItemStatus values. It neither clamps nor rejects
        // negative condition; CanActorEquip rejects condition == 0 exactly.
        public static long GetCondition(int? currentHitPoints, int? maximumHitPoints)
        {
            if (!currentHitPoints.HasValue || !maximumHitPoints.HasValue || maximumHitPoints <= 0)
                return 100;
            var numerator = 100L * currentHitPoints.Value;
            var denominator = maximumHitPoints.Value;
            var value = numerator / denominator;
            return numerator < 0 && numerator % denominator != 0 ? value - 1 : value;
        }

        public static EquipmentRequirementFailure Check(Manifestation actor, Item item, ItemClassInfo classInfo)
        {
            if (item == null)
                return EquipmentRequirementFailure.None; // Removing equipment has no incoming item requirements.
            var requirements = item.ItemTemplate.ItemInfo.Requirements;
            if (requirements.TryGetValue(RequirementsType.ReqXpLevel, out var minimum) && actor.Level < minimum)
                return EquipmentRequirementFailure.MinimumLevel;
            if (requirements.TryGetValue(RequirementsType.ReqXpLevelMax, out var maximum) && actor.Level > maximum)
                return EquipmentRequirementFailure.MaximumLevel;
            if (requirements.TryGetValue(RequirementsType.ReqBody, out var body) && Current(actor, Attributes.Body) < body)
                return EquipmentRequirementFailure.Body;
            if (requirements.TryGetValue(RequirementsType.ReqMind, out var mind) && Current(actor, Attributes.Mind) < mind)
                return EquipmentRequirementFailure.Mind;
            if (requirements.TryGetValue(RequirementsType.ReqSpirit, out var spirit) && Current(actor, Attributes.Spirit) < spirit)
                return EquipmentRequirementFailure.Spirit;
            var race = item.ItemTemplate.ItemInfo.RaceReq;
            if (race != 0 && race != (int)actor.Race)
                return EquipmentRequirementFailure.Race;
            var skill = item.ItemTemplate.EquipableInfo;
            if (skill != null && skill.SkillLevel > 0 &&
                (!actor.Skills.TryGetValue((SkillId)skill.SkillId, out var learned) || learned.SkillLevel < skill.SkillLevel))
                return EquipmentRequirementFailure.Skill;
            return GetCondition(item.CurrentHitPoints, classInfo?.MaxHitPoints) == 0
                ? EquipmentRequirementFailure.Broken : EquipmentRequirementFailure.None;
        }

        private static int Current(Manifestation actor, Attributes attribute)
            => actor.Attributes.TryGetValue(attribute, out var value) ? value.Current : 0;
    }
}
