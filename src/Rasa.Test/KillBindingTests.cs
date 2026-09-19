using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// A kill binding names a placement (1994/1 Tizzik Gi, 430/3-6 the Bane Mortars) or a creature type (427/6
    /// Proctor Fulgor). Only the second ever completed: the match compared creature_id with the dead creature's id,
    /// and a placement binding's creature_id is 0.
    /// </summary>
    [TestClass]
    public class KillBindingTests
    {
        private static NpcMissionObjectiveBindingEntry Binding(uint placementId, uint creatureId) => new NpcMissionObjectiveBindingEntry
        {
            MissionId = 430, ObjectiveId = 3, Kind = (byte)ObjectiveBindingKind.Kill, PlacementId = placementId, CreatureId = creatureId
        };

        [TestMethod]
        public void APlacementBindingIsCompletedByTheCreatureOfThatPlacement()
        {
            var mortar = new Creature { DbId = 199720, ContentPlacementId = 199700 };
            Assert.IsTrue(MissionManager.KillBindingNames(Binding(199700, 0), mortar));
            Assert.IsFalse(MissionManager.KillBindingNames(Binding(199701, 0), mortar), "another mortar's objective");
        }

        [TestMethod]
        public void ACreatureBindingIsCompletedByAnyCreatureOfThatType()
        {
            Assert.IsTrue(MissionManager.KillBindingNames(Binding(0, 76), new Creature { DbId = 76 }));
            Assert.IsTrue(MissionManager.KillBindingNames(Binding(0, 76), new Creature { DbId = 76, ContentPlacementId = 5 }));
            Assert.IsFalse(MissionManager.KillBindingNames(Binding(0, 76), new Creature { DbId = 77 }));
        }
    }
}
