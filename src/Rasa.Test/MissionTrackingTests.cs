using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// The MissionTrack character options that decide what the client's on-screen tracker shows
    /// after it reloads them. The option ids and the thirty-slot limit come from the recovered
    /// 1.16.5.0 client (generated/client/defaultoption.py, client/gameui.py:130); see
    /// MissionTrackingRules for the full citation.
    /// </summary>
    [TestClass]
    public class MissionTrackingTests
    {
        private static readonly uint[] ClientSlotIds =
        {
            55, 56, 57, 58, 59, 60, 61, 62, 63, 64,
            85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
            185, 187, 188, 189, 190
        };

        private static List<CharacterOptions> Slots(params uint[] missionIds)
            => MissionTrackingRules.TrackSlots
                .Select((slot, index) => new CharacterOptions(slot, index < missionIds.Length ? missionIds[index].ToString() : "0"))
                .ToList();

        private static uint[] Values(IEnumerable<CharacterOptions> options)
            => options.Select(option => uint.Parse(option.Value)).ToArray();

        [TestMethod]
        public void SlotOptionIdsAreTheOnesTheClientReads()
        {
            Assert.AreEqual(30, MissionTrackingRules.MaxTrackedMissions);
            CollectionAssert.AreEqual(ClientSlotIds, MissionTrackingRules.TrackSlots.Select(slot => (uint)slot).ToArray());
        }

        [TestMethod]
        public void AcceptingWithNothingStoredTracksTheNewMissionInTheFirstSlot()
        {
            var slots = MissionTrackingRules.Track(new List<CharacterOptions>(), _ => true, 7001);

            Assert.AreEqual(30, slots.Count);
            Assert.AreEqual(CharacterOption.MissionTrack0, slots[0].OptionId);
            Assert.AreEqual("7001", slots[0].Value);
            Assert.IsTrue(slots.Skip(1).All(slot => slot.Value == "0"));
        }

        [TestMethod]
        public void AcceptingAppendsAfterTheMissionsAlreadyTracked()
        {
            var slots = MissionTrackingRules.Track(Slots(7001, 7002), _ => true, 7003);

            CollectionAssert.AreEqual(new uint[] { 7001, 7002, 7003 }, Values(slots).Take(3).ToArray());
            Assert.IsTrue(slots.Skip(3).All(slot => slot.Value == "0"));
        }

        [TestMethod]
        public void AMissionAlreadyTrackedIsNotTrackedTwiceAndKeepsItsPlace()
        {
            var slots = MissionTrackingRules.Track(Slots(7001, 7002, 7003), _ => true, 7002);

            CollectionAssert.AreEqual(new uint[] { 7001, 7002, 7003 }, Values(slots).Take(3).ToArray());
            Assert.IsTrue(slots.Skip(3).All(slot => slot.Value == "0"));
        }

        [TestMethod]
        public void MissionsTheLogNoLongerHoldsAreDroppedAndTheirSlotsCleared()
        {
            // FilterMissionTrackingData (client/gameui.py:1754) drops exactly these on the client side.
            var slots = MissionTrackingRules.Track(Slots(7001, 7002, 7003), missionId => missionId != 7002, 7004);

            CollectionAssert.AreEqual(new uint[] { 7001, 7003, 7004 }, Values(slots).Take(3).ToArray());
            Assert.IsTrue(slots.Skip(3).All(slot => slot.Value == "0"));
        }

        [TestMethod]
        public void TheStoredSlotsAreReadBackInSlotOrderIgnoringTheZeroDefault()
        {
            var options = Slots(7001, 7002);
            options[5] = new CharacterOptions(MissionTrackingRules.TrackSlots[5], "7009");

            CollectionAssert.AreEqual(new uint[] { 7001, 7002, 7009 }, MissionTrackingRules.Tracked(options).ToArray());
        }

        [TestMethod]
        public void OptionsOutsideTheTrackSlotsAreIgnored()
        {
            var options = Slots(7001);
            options.Add(new CharacterOptions(CharacterOption.MaximizeInventory, "1"));
            options.Add(new CharacterOptions(CharacterOption.MissionTrack30, "7002"));

            CollectionAssert.AreEqual(new uint[] { 7001 }, MissionTrackingRules.Tracked(options).ToArray());
        }

        [TestMethod]
        public void AThirtyFirstMissionIsDroppedTheWayTheClientDropsIt()
        {
            // SaveMissionTrackingData writes list[idx] for idx < len and 0 beyond, so the oldest
            // thirty survive. MissionRules.MaxMissionCount is also 30, so the log cannot get here.
            var full = Enumerable.Range(1, 30).Select(index => (uint)(7000 + index)).ToArray();
            var slots = MissionTrackingRules.Track(Slots(full), _ => true, 7031);

            Assert.AreEqual(30, slots.Count);
            CollectionAssert.AreEqual(full, Values(slots));
            Assert.IsFalse(slots.Any(slot => slot.Value == "7031"));
        }

        [DataTestMethod]
        [DataRow("1,992", 1992)]
        [DataRow("12,345", 12345)]
        [DataRow("123,456", 123456)]
        [DataRow("1,234,567", 1234567)]
        [DataRow("2,147,483,647", int.MaxValue)]
        public void OriginalUsGroupedOptionReadsWithoutChangingItsStoredText(string value, int expected)
        {
            var option = new CharacterOptions(CharacterOption.MissionTrack0, value);
            CollectionAssert.AreEqual(new[] { (uint)expected }, MissionTrackingRules.Tracked(new[] { option }).ToArray());
            Assert.AreEqual(value, option.Value);
        }

        [DataTestMethod]
        [DataRow("1,99")]
        [DataRow("19,92")]
        [DataRow("1992,")]
        [DataRow(",1992")]
        [DataRow("1,,992")]
        [DataRow("01,992")]
        [DataRow("0,000")]
        [DataRow("1.992")]
        [DataRow("1 992")]
        [DataRow("1,992suffix")]
        [DataRow("1,992 ")]
        [DataRow("-1,992")]
        [DataRow("4,294,967,296")]
        public void GroupedMissionIdsRejectNoncanonicalSeparatorsAndOverflow(string value)
        {
            var option = new CharacterOptions(CharacterOption.MissionTrack0, value);
            Assert.AreEqual(0, MissionTrackingRules.Tracked(new[] { option }).Count);
            Assert.AreEqual(value, option.Value);
        }

        [TestMethod]
        public void GroupedAndUngroupedOptionsDeduplicateWithoutRetrackingAnIntentionalZero()
        {
            var options = new[]
            {
                new CharacterOptions(CharacterOption.MissionTrack0, "1,992"),
                new CharacterOptions(CharacterOption.MissionTrack1, "1992"),
                new CharacterOptions(CharacterOption.MissionTrack2, "0")
            };
            CollectionAssert.AreEqual(new[] { 1992u }, MissionTrackingRules.Tracked(options).ToArray());
            var updated = MissionTrackingRules.Track(options, _ => true, 1993);
            CollectionAssert.AreEqual(new[] { 1992u, 1993u }, MissionTrackingRules.Tracked(updated).ToArray());
            Assert.IsTrue(updated.Skip(2).All(option => option.Value == "0"));
            CollectionAssert.AreEqual(new[] { "1,992", "1992", "0" }, options.Select(option => option.Value).ToArray());
        }
    }
}
