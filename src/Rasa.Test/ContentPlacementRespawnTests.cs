using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class ContentPlacementRespawnTests
    {
        [TestMethod]
        public void DeathWithZeroRespawnStaysDefeatedUntilTheInstanceIsRebuilt()
        {
            var instance = new MapChannel();
            Assert.IsTrue(instance.CanMaterializeContentPlacement(198691));

            instance.RecordContentPlacementDeath(198691, 0, 1000);
            Assert.IsFalse(instance.CanMaterializeContentPlacement(198691),
                "Mission state refresh after corpse cleanup must not recreate a defeated one-shot Warrior.");
            Assert.IsFalse(instance.ContentRespawns.ContainsKey(198691));
            Assert.IsTrue(new MapChannel().CanMaterializeContentPlacement(198691),
                "A new private instance starts with fresh placements.");
        }

        [TestMethod]
        public void TimedRespawnCannotBePulledForwardByAConditionRefresh()
        {
            var instance = new MapChannel();
            instance.RecordContentPlacementDeath(198700, 5000, 1000);
            Assert.AreEqual(6000L, instance.ContentRespawns[198700]);
            Assert.IsFalse(instance.CanMaterializeContentPlacement(198700));

            // MapChannelManager removes the pending entry when the due tick arrives.
            instance.ContentRespawns.Remove(198700);
            Assert.IsTrue(instance.CanMaterializeContentPlacement(198700));
        }
    }
}
