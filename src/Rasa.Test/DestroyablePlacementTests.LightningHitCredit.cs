using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;

namespace Rasa.Test
{
    public partial class DestroyablePlacementTests
    {
        [TestMethod]
        public void LightningLessonCreditsFirstDamagingHitWithoutDestroyingTarget()
        {
            foreach (var binding in _content.Content.LiveBindings.Where(b => b.MissionId == MissionId && b.ObjectiveId == 3))
            {
                binding.ActionId = 194;
                binding.DestroyingHitOnly = false;
            }

            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);

            // Neither an unrelated weapon hit nor a zero-damage Lightning event satisfies the lesson.
            _content.DamageContentUsable(_map, _dummy.EntityId, 10, _client, 1);
            _content.DamageContentUsable(_map, _dummy.EntityId, 0, _client, 194);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);
            Assert.AreEqual(90u, _dummy.HitPoints);

            _content.DamageContentUsable(_map, _dummy.EntityId, 10, _client, 194);
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[MissionId].Objectives[3]);
            Assert.AreEqual(80u, _dummy.HitPoints);
        }
    }
}
