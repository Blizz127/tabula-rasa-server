using System;
using System.Collections.Generic;
using Rasa.Structures;

namespace Rasa.Data
{
    /// <summary>
    /// Original client module classes identified from the filmed Training Day
    /// rewards. The template ids are inferred; the module ids are original
    /// client data (see docs/evidence/training-day-reward-modules.json).
    /// </summary>
    public static class MissionRewardModuleBindings
    {
        // Effect ids 9/115 and their tooltip keys 1722/1726 are original
        // generated/client/gameeffectdata.pyo rows. The observed Training Day
        // tooltips show -15 for 15 sec. Encoding -15 as a flat coefficient,
        // set level 0 and unused arguments as None is an inferred server tuple.
        public static ItemModule DefinitionFor(int moduleId) => moduleId switch
        {
            900221 => new ItemModule(900221, null,
                new ModuleInfo(9, 0, -15, 0, 0, null, 15, null, null)),
            900256 => new ItemModule(900256, null,
                new ModuleInfo(115, 0, -15, 0, 0, null, 15, null, null)),
            _ => null
        };

        public static IReadOnlyList<int> For(uint missionId, uint templateId)
        {
            if (missionId != 1526)
                return Array.Empty<int>();

            return templateId switch
            {
                116929 => new[] { 900221 }, // Vextronics Pistol, physical resist module
                116930 => new[] { 900256 }, // Vextronics Pulse Pistol, EMP resist module
                _ => Array.Empty<int>()
            };
        }
    }
}
