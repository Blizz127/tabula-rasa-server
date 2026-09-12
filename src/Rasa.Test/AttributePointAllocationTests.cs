using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class AttributePointAllocationTests
    {
        [DataTestMethod]
        [DataRow(1, 0)]
        [DataRow(2, 3)]
        [DataRow(5, 12)]
        [DataRow(15, 42)]
        [DataRow(30, 87)]
        [DataRow(50, 147)]
        public void EarnsThreePointsPerLevel(int level, int expected)
        {
            Assert.AreEqual(expected, AttributePointAllocation.GetAvailablePoints(new Manifestation { Level = (byte)level }));
        }

        [TestMethod]
        public void SpendingEntireBudgetCannotBeReplayed()
        {
            var player = new Manifestation { Level = 5, SpentBody = 3 };
            Assert.IsTrue(AttributePointAllocation.TryAllocate(player, 2, 3, 4));
            Assert.AreEqual(5, player.SpentBody);
            Assert.AreEqual(3, player.SpentMind);
            Assert.AreEqual(4, player.SpentSpirit);
            Assert.AreEqual(0, AttributePointAllocation.GetAvailablePoints(player));
            Assert.IsFalse(AttributePointAllocation.TryAllocate(player, 2, 3, 4));
            Assert.AreEqual(5, player.SpentBody);
        }

        [DataTestMethod]
        [DataRow(-1, 1, 1)]
        [DataRow(1, -1, 1)]
        [DataRow(1, 1, -1)]
        [DataRow(3, 3, 4)]
        [DataRow(int.MaxValue, int.MaxValue, 3)]
        [DataRow(0, 0, 0)]
        public void RejectsInvalidRequestsWithoutPartialChanges(int body, int mind, int spirit)
        {
            var player = new Manifestation { Level = 5, SpentBody = 1, SpentMind = 1, SpentSpirit = 1 };
            Assert.IsFalse(AttributePointAllocation.TryAllocate(player, body, mind, spirit));
            Assert.AreEqual(1, player.SpentBody);
            Assert.AreEqual(1, player.SpentMind);
            Assert.AreEqual(1, player.SpentSpirit);
        }

        [DataTestMethod]
        [DataRow(-1, 0, 0)]
        [DataRow(0, -1, 0)]
        [DataRow(0, 0, -1)]
        [DataRow(100, 100, 100)]
        [DataRow(int.MaxValue, int.MaxValue, 3)]
        public void LegacyInvalidAllocationsDoNotGrantMorePoints(int body, int mind, int spirit)
        {
            var player = new Manifestation { Level = 5, SpentBody = body, SpentMind = mind, SpentSpirit = spirit };
            Assert.AreEqual(0, AttributePointAllocation.GetAvailablePoints(player));
            Assert.IsFalse(AttributePointAllocation.TryAllocate(player, 1, 0, 0));
        }
    }
}
