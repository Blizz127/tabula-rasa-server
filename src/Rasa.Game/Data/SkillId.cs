namespace Rasa.Data
{
    public enum SkillId
    {
        None = -1,
        // reqruit Skill's
        Firearms = 1,
        HandToHand = 8,

        /// <summary>
        /// skilldata T2_SPECIALIST_TOOLS = 14. Behind every client/actions/tools/ module;
        /// healdisc.py hardcodes the same number as HEALING_SKILL_ID.
        /// </summary>
        SpecialistTools = 14,

        MotorAssistArmor = 19,
        Lightning = 49,
        Sprint = 165
    }
}
