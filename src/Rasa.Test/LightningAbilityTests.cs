using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class LightningAbilityTests
    {
        // Client actiondata's five trainable rows and shared/scaling.py.
        // Levels 9 and 17 double and quadruple level-one amounts respectively.
        [DataTestMethod]
        [DataRow(1, 1, 180, 240)]
        [DataRow(2, 1, 240, 300)]
        [DataRow(3, 1, 240, 300)]
        [DataRow(4, 1, 240, 300)]
        [DataRow(5, 1, 240, 300)]
        [DataRow(1, 4, 233, 311)]
        [DataRow(1, 9, 360, 480)]
        [DataRow(5, 17, 960, 1200)]
        public void DamageBoundsFollowClientRankAndExperienceLevel(int rank, int level, int minimum, int maximum)
        {
            var actual = LightningAbilityData.GetBaseDamageRange((uint)rank, level);
            Assert.AreEqual(minimum, actual.Minimum);
            Assert.AreEqual(maximum, actual.Maximum);
        }

        [TestMethod]
        public void NonTrainableClientVariantsAreNotPlayerRanks()
        {
            foreach (var rank in new uint[] { 0, 6, 7, uint.MaxValue })
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => LightningAbilityData.GetBaseDamageRange(rank, 1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => LightningAbilityData.GetBaseDamageRange(1, 0));
        }

        [DataTestMethod]
        [DataRow(1, 1, 180, 240)]
        [DataRow(5, 17, 960, 1200)]
        public void ActualRecoveryUsesScaledDamageInsteadOfFixedLevelFourSample(int rank, int level, int minimum, int maximum)
        {
            var map = new MapChannel();
            map.MapCellInfo.Cells.Add(0, new MapCell());
            var target = new Creature();
            target.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            target.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(target);
            try
            {
                var player = new Manifestation { Level = (byte)level };
                ActorActionManager.Instance.PerformRecovery(map,
                    new ActionData(player, ActionId.AaRecruitLightning, (uint)rank, target.EntityId, 0));

                var damage = 100000 - target.Attributes[Attributes.Health].Current;
                Assert.IsTrue(damage >= minimum && damage <= maximum, $"Unexpected base damage {damage}");
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(target.EntityId);
                EntityManager.Instance.UnregisterCreature(target.EntityId);
            }
        }
    }
}
