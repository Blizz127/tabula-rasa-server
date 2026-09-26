using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class DestroyablePlacementTests
    {
        /// <summary>
        /// A destroying-hit binding whose objective has a counter counts destroyed objects, as 323 Pirate Radio's
        /// "Infestation Remaining" counts two Living Infestations per dish (PravusResearchInstance): the first
        /// destruction advances the counter, the second completes the objective, and a hit that does not destroy
        /// counts nothing.
        /// </summary>
        [TestMethod]
        public void ADestroyingHitAdvancesTheObjectivesCounterUntilItsTarget()
        {
            foreach (var binding in _content.Content.LiveBindings.Where(b => b.MissionId == MissionId && b.ObjectiveId == 3))
                binding.CounterId = 0;
            ((List<NpcMissionObjectiveCounterEntry>)_content.Content.Catalog.Counters).Add(
                new NpcMissionObjectiveCounterEntry { MissionId = MissionId, ObjectiveId = 3, CounterId = 0, InitialValue = 0, TargetValue = 2 });

            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);

            Assert.IsNull(_content.DamageContentUsable(_map, _dummy.EntityId, 40, _client));
            Assert.IsFalse(_client.Player.Missions[MissionId].Counters.ContainsKey((3u, (byte)0)), "a hit that does not destroy counts nothing");

            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, 60, _client));
            Assert.AreEqual(1, _client.Player.Missions[MissionId].Counters[(3u, (byte)0)]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);

            _now += 931;
            _content.RestoreDestroyedUsables(_map, _now);
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, 100, _client));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[MissionId].Objectives[3]);
        }
    }
}
