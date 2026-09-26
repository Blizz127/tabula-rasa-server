using System;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Managers;

namespace Rasa.Test
{
    [TestClass]
    public class BootcampCoverGeometryTests
    {
        private static CoverGeometry LoadBootcamp()
        {
            var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "../../../../Rasa.Game/Content/Cover/adv_bootcamp.cover.json"));
            return CoverGeometry.Load(path);
        }

        [TestMethod]
        public void OriginalSandbagTrianglesOccludeCourtyardDeathPosition()
        {
            var geometry = LoadBootcamp();
            Assert.AreEqual(110, geometry.TriangleCount);

            // Placement 198665 killed Alden at the logged (296.5,120.1,73.9). These
            // are emulator positions, not evidence of original retail combat timing.
            var enemy = new Vector3(291.77f, 120.5f, 63.77f);
            var player = new Vector3(296.5f, 120.1f, 73.9f);
            Assert.AreEqual(9, geometry.OccludedSamples(enemy, player));
            Assert.AreEqual(25, geometry.ScaleDamage(enemy, player, 100, out var blocked, out var modifier));
            Assert.AreEqual(9, blocked);
            Assert.AreEqual(0.25f, modifier);
        }

        [TestMethod]
        public void ClientCoverTableEndpointsAndNoCoverAreRespected()
        {
            Assert.AreEqual(1f, CoverGeometry.DamageFactor(0f));
            Assert.AreEqual(1f, CoverGeometry.DamageFactor(0.2f));
            Assert.AreEqual(0.7f, CoverGeometry.DamageFactor(0.4f), 0.00001f);
            Assert.AreEqual(0.5f, CoverGeometry.DamageFactor(0.6f), 0.00001f);
            Assert.AreEqual(0.35f, CoverGeometry.DamageFactor(0.8f), 0.00001f);
            Assert.AreEqual(0.25f, CoverGeometry.DamageFactor(1f));

            var geometry = LoadBootcamp();
            var enemy = new Vector3(286.23f, 120.5f, 65.35f);
            var clear = new Vector3(289f, 120.5f, 66f);
            Assert.AreEqual(0, geometry.OccludedSamples(enemy, clear));
            Assert.AreEqual(100, geometry.ScaleDamage(enemy, clear, 100, out var clearBlocked, out var clearModifier));
            Assert.AreEqual(0, clearBlocked);
            Assert.AreEqual(1f, clearModifier);

            var partial = new Vector3(295.8f, 120.5f, 67.8f);
            var blocked = geometry.OccludedSamples(enemy, partial);
            Assert.IsTrue(blocked > 0 && blocked < 9);
            var scaled = geometry.ScaleDamage(enemy, partial, 100, out var measuredBlocked, out var partialModifier);
            Assert.AreEqual(blocked, measuredBlocked);
            Assert.IsTrue(scaled < 100 && scaled > 25);
            Assert.IsTrue(partialModifier < 1f && partialModifier > 0.25f);
        }
    }
}
