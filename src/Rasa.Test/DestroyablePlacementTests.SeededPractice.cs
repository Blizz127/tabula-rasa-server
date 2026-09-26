using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    public partial class DestroyablePlacementTests
    {
        [DataTestMethod]
        [DataRow(1)]
        [DataRow(84)]
        public void SeededPracticeDummyFallsToOnePositiveHitAndRestores(int damage)
        {
            // Replay the actual S2 seed and corrective data migrations, then load those
            // combat values through the database/catalog/materializer fixture path.
            var seed = BootcampS2ProvenanceTests.CurrentSeededRows()[("content_placement", 198652u)];
            using (var context = WorldContext(_worldConnection))
            {
                var placement = context.ContentPlacementEntries.Single(row => row.Id == PlacementId);
                placement.HitPoints = Convert.ToUInt32(seed["hit_points"]);
                placement.RestoreMs = Convert.ToUInt32(seed["restore_ms"]);
                context.SaveChanges();
            }
            _map.ContentUsables.Remove(_dummy.EntityId);
            _map.DynamicObjects.Remove(_dummy);
            CellManager.Instance.RemoveFromWorld(_map, _dummy);
            _content.Load(() => new BootcampConfig(), new References(), _missions.LoadedMissions);
            _dummy = _map.DynamicObjects.Single(obj => obj.DynamicObjectType == DynamicObjectType.ContentUsable);
            Assert.AreEqual(1u, _dummy.HitPoints, "1 HP is an explicit reconstruction, not recovered original HP.");
            Assert.AreEqual(1u, _dummy.MaxHitPoints);
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);
            Drain(_client);
            _content.TickNow = () => _now;

            Assert.IsNull(_content.DamageContentUsable(_map, _dummy.EntityId, 0, _client));
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, damage, _client));
            Assert.AreEqual(0u, _dummy.HitPoints);
            Assert.AreEqual(UseObjectState.StateDestroyed, _dummy.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[MissionId].Objectives[3]);
            var restoreAt = _dummy.RestoreAt;
            Assert.AreEqual(_now + 930, restoreAt, "The measured nominal recovery is unchanged.");
            _content.RestoreDestroyedUsables(_map, restoreAt - 1);
            Assert.AreEqual(0u, _dummy.HitPoints);
            _content.RestoreDestroyedUsables(_map, restoreAt);
            Assert.AreEqual(1u, _dummy.HitPoints);
            Assert.AreEqual((UseObjectState)110, _dummy.StateId);
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, damage, _client));
            Assert.AreEqual(UseObjectState.StateDestroyed, _dummy.StateId);
        }
    }
}
