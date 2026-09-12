using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;

namespace Rasa.Test
{
    [TestClass]
    public class HospitalMapMarkerTests
    {
        [TestMethod]
        public void ConsumersCannotReplaceHospitalMarkersOrMutateTheirPositions()
        {
            var entries = WildernessHospitalMapMarkers.Entries;
            var collection = (IList<HospitalMapMarkerData>)entries;
            var original = entries[0];
            var position = original.Position;
            position.X += 100;

            Assert.IsTrue(collection.IsReadOnly);
            Assert.ThrowsException<NotSupportedException>(() => collection[0] = entries[1]);
            Assert.ThrowsException<NotSupportedException>(() => collection.Clear());
            Assert.AreSame(original, entries[0]);
            Assert.AreNotEqual(position, original.Position);
        }

        [TestMethod]
        public void OriginalMarkerIdentityAndOptionalTooltipRemainDistinct()
        {
            var entries = WildernessHospitalMapMarkers.Entries;
            Assert.AreEqual(6, entries.Select(marker => marker.MarkerEntityId).Distinct().Count());
            Assert.IsTrue(entries.All(marker => marker.MarkerEntityId > uint.MaxValue));
            Assert.AreEqual(HospitalMapMarkerType.SafeZone,
                entries.Single(marker => marker.NameTextId == 229).MarkerType);
            Assert.IsNull(entries.Single(marker => marker.NameTextId == 303).TooltipTextId);
            Assert.IsNull(entries.Single(marker => marker.NameTextId == 306).TooltipTextId);
            Assert.IsTrue(entries.All(marker => marker.Position != Vector3.Zero));
        }
    }
}
