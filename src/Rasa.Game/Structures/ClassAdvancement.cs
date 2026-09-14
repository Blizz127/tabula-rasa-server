using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;

    /// <summary>
    /// Tier (class) advancement at a class trainer, reconstructed for final live in
    /// research/20260914-class-trainer (class-trainer-spec.json, protocol-shapes.json).
    /// A class holds its character at the level before its tier level until the player trains into a
    /// child class; training changes the class and releases the withheld level-ups.
    /// </summary>
    public static class ClassAdvancement
    {
        public const uint Recruit = 1;

        // gameuiutil.pyo g_ClassTree: parent (higherClass) of each class id 1..15 (original).
        private static readonly uint[] ParentClasses = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };

        /// <summary>
        /// Single class trainers since D12 (live 2008-09-18). Only Training Officer Kincaid of Alia Das
        /// (npc package 2588) is evidenced so far; trainers at other hubs are unrecovered.
        /// </summary>
        public static readonly IReadOnlyCollection<uint> TrainerNpcPackages = new HashSet<uint> { 2588 };

        // tierselect.Update closes the window beyond MAX_MISSION_SHARE_RANGE of the trainer (original client range).
        public const float TrainerRange = 20f;

        // npctrainerdialoglanguage generic group: 337 can train now, 339 already knows everything, 340 not ready yet.
        public const int DialogCanTrain = 337;
        public const int DialogFinalTier = 339;
        public const int DialogNotReady = 340;

        public static uint ParentOf(uint classId) => classId < ParentClasses.Length ? ParentClasses[classId] : 0;

        /// <summary>The classes this class trains into: its immediate children (gameuiutil.IsTrainableClass).</summary>
        public static IReadOnlyList<uint> ChildrenOf(uint classId) =>
            Enumerable.Range(1, ParentClasses.Length - 1).Select(id => (uint)id).Where(id => ParentClasses[id] == classId).ToList();

        /// <summary>The tier level a class cannot reach by experience alone: 5 for Recruit, 15 for tier 2, 30 for tier 3 (text 5695).</summary>
        public static int? GateLevel(uint classId) => classId switch
        {
            1 => 5,
            2 or 3 => 15,
            >= 4 and <= 7 => 30,
            _ => null
        };

        /// <summary>
        /// The character stands at its class's gate: the level before the tier level with the experience for the
        /// tier level (observed: the Recruit held at 4 with a full experience bar, footage B2).
        /// </summary>
        public static bool IsAtGate(uint classId, int level, uint experience) =>
            GateLevel(classId) is { } gate && level >= gate - 1 && experience >= ExpPerLevel.ExpRequred[gate - 1];

        /// <summary>The experience-driven level-up to <paramref name="nextLevel"/> is withheld for this class.</summary>
        public static bool IsGatedLevel(uint classId, int nextLevel) => GateLevel(classId) == nextLevel;

        public static int DialogFor(uint classId, int level, uint experience) =>
            ChildrenOf(classId).Count == 0 ? DialogFinalTier : IsAtGate(classId, level, experience) ? DialogCanTrain : DialogNotReady;
    }
}
