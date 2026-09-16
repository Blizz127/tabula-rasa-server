using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Structures.World;

    /// <summary>
    /// The escort rule: an escort objective ("Take Milpas to Apirka") is not met by the player arriving alone.
    /// </summary>
    [TestClass]
    public class ContentEscortTests
    {
        private static ContentAreaEntry Area(double x, double y, double z, double radius, byte shape = 1,
            double halfHeight = 0)
            => new ContentAreaEntry { PosX = x, PosY = y, PosZ = z, Radius = radius, Shape = shape, HalfHeight = halfHeight };

        [TestMethod]
        public void AMissionWithoutEscortsIsUnaffected()
        {
            // Every non-escort area objective in the game goes through this path, so an empty escort list must pass.
            Assert.IsTrue(ContentEscort.AllInside(new List<Vector3>(), Area(0, 0, 0, 10)));
            Assert.IsTrue(ContentEscort.AllInside(null, Area(0, 0, 0, 10)));
        }

        [TestMethod]
        public void TheEscortHasToBeInsideTheArea()
        {
            var area = Area(825.0, 301.0, 499.5, 12.0);
            var playerIsThere = new Vector3(825.5f, 301.0f, 500.0f);
            var milpasBehind = new Vector3(700.0f, 301.0f, 400.0f);

            Assert.IsTrue(ContentEscort.AllInside(new List<Vector3> { playerIsThere }, area));
            Assert.IsFalse(ContentEscort.AllInside(new List<Vector3> { milpasBehind }, area));
            Assert.IsFalse(ContentEscort.AllInside(new List<Vector3> { playerIsThere, milpasBehind }, area));
        }

        [TestMethod]
        public void ASphereBoundsVerticallyByItsRadiusAndACylinderByItsHalfHeight()
        {
            var sphere = Area(0, 100, 0, 10.0);
            Assert.IsTrue(ContentEscort.Inside(new Vector3(0, 109.0f, 0), sphere));
            Assert.IsFalse(ContentEscort.Inside(new Vector3(0, 111.0f, 0), sphere));

            // 198600/198601 are vertical cylinders: 20 m of half height on a 4 m radius, so the causeway trigger
            // reaches the deck above the terrain.
            var cylinder = Area(0, 100, 0, 4.0, shape: 2, halfHeight: 20.0);
            Assert.IsTrue(ContentEscort.Inside(new Vector3(3.0f, 118.0f, 0), cylinder));
            Assert.IsFalse(ContentEscort.Inside(new Vector3(5.0f, 100.0f, 0), cylinder));
        }
    }
}
