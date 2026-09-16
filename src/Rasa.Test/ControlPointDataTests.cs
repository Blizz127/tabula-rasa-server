using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// The PvP control points as the final client carries them: the 17-row controlpointdata table with its fields
    /// named from gameuiutil's readers, the ControlPointStatus struct from shared/controlpointdefs.py, the list-shaped
    /// ControlPointStatus call and the SetOwnerId call. The tests pin the decoded data and the wire shapes the client's
    /// handlers were written against; nothing here is about capture rules, which are not evidenced.
    /// </summary>
    [TestClass]
    public class ControlPointDataTests
    {
        [TestMethod]
        public void TheTableIsTheClientsSeventeenRows()
        {
            Assert.AreEqual(17, ControlPointData.Rows.Length);
            Assert.AreEqual(17, ControlPointData.Rows.Select(row => row.Id).Distinct().Count());

            // Every row but one is team-owned; the clan-owned one is a test-map row.
            var clanOwned = ControlPointData.Rows.Where(row => row.OwnershipType == ControlPointOwnershipType.ClanOwned).ToArray();
            Assert.AreEqual(1, clanOwned.Length);
            Assert.AreEqual(10000001u, clanOwned[0].Id);
            Assert.IsFalse(clanOwned[0].IsLive);

            // Row 4 is (3, 6017, 2365, 50, 1): Whiskey on the proving grounds, first in the tracker.
            Assert.IsTrue(ControlPointData.TryGet(4u, out var whiskey));
            Assert.AreEqual(ControlPointOwnershipType.TeamOwned, whiskey.OwnershipType);
            Assert.AreEqual(6017u, whiskey.NameId);
            Assert.AreEqual("Whiskey", whiskey.Name);
            Assert.AreEqual(2365u, whiskey.MapTemplateId);
            Assert.AreEqual("adv_wargame_provinggroundsv002", whiskey.MapName);
            Assert.AreEqual(50u, whiskey.Level);
            Assert.AreEqual(1u, whiskey.SortOrder);

            Assert.IsFalse(ControlPointData.TryGet(215u, out _));
        }

        [TestMethod]
        public void TheTwoLiveBattlegroundsEachHaveTheirOwnPoints()
        {
            CollectionAssert.AreEquivalent(
                new[] { "adv_wargame_provinggroundsv002", "adv_wargame_edmundrange2" },
                ControlPointData.LiveMapNames.ToArray());

            // The proving grounds: Whiskey, Charlie, Echo and the two bases.
            var provingGrounds = ControlPointData.ForMap("adv_wargame_provinggroundsv002");
            CollectionAssert.AreEqual(new uint[] { 3, 4, 5, 6, 7 }, provingGrounds.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(new[] { "Echo", "Whiskey", "Charlie", "Blue Base", "Red Base" }, provingGrounds.Select(row => row.Name).ToArray());

            // Edmund Range: the same five names plus the two depots.
            var edmundRange = ControlPointData.ForMap("ADV_WARGAME_EDMUNDRANGE2");
            CollectionAssert.AreEqual(new uint[] { 8, 9, 10, 11, 12, 13, 14 }, edmundRange.Select(row => row.Id).ToArray());
            Assert.IsTrue(edmundRange.All(row => row.Level == 50u));
            Assert.AreEqual(2, edmundRange.Count(row => row.Name.StartsWith("Control Point: ")));

            // A map without control points has none, and the name lookup is safe on nothing.
            Assert.AreEqual(0, ControlPointData.ForMap("Adv_Foreas_Concordia_Wilderness").Count);
            Assert.AreEqual(0, ControlPointData.ForMap(null).Count);
        }

        [TestMethod]
        public void TheTrackerOrderIsSortOrderThenIdWithNoneFirst()
        {
            // gameuiutil.SortControlPointList sorts (sortOrder, cpId) tuples; None sorts before any number in Python 2.
            var edmundRange = ControlPointData.ForMap("adv_wargame_edmundrange2").Select(row => row.Id);
            CollectionAssert.AreEqual(new uint[] { 8, 9, 13, 14, 12, 10, 11 }, ControlPointData.SortForTracker(edmundRange).ToArray());

            // An id the table has not still sorts, with the None group.
            CollectionAssert.AreEqual(new uint[] { 215, 12 }, ControlPointData.SortForTracker(new uint[] { 12, 215 }).ToArray());
        }

        [TestMethod]
        public void ControlPointStatusIsAListOfFourTuplesWithANullableOwner()
        {
            var packet = new ControlPointStatusPacket(new[]
            {
                new ControlPointStatus(12u, null, ControlPointState.New, 0u),
                new ControlPointStatus(10u, ControlPointOwner.BlueTeam, ControlPointState.War, 123456u),
            });
            Assert.AreEqual(GameOpcode.ControlPointStatus, packet.Opcode);

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            // Recv_ControlPointStatus(statusList): one argument, a list.
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());

            var first = reader.ReadStruct<ControlPointStatus>();
            Assert.AreEqual(12u, first.ControlPointId);
            Assert.IsNull(first.OwnerId);
            Assert.AreEqual(ControlPointState.New, first.StateId);
            Assert.AreEqual(0u, first.EndTime);

            var second = reader.ReadStruct<ControlPointStatus>();
            Assert.AreEqual(10u, second.ControlPointId);
            Assert.AreEqual(2L, second.OwnerId);
            Assert.AreEqual(ControlPointState.War, second.StateId);
            Assert.AreEqual(123456u, second.EndTime);

            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void SetOwnerIdCarriesTheOwnerAsItsOneArgument()
        {
            var packet = new SetOwnerIdPacket(ControlPointOwner.RedTeam);
            Assert.AreEqual(GameOpcode.SetOwnerId, packet.Opcode);

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);

            // The "no package" owner is -1, which the writer keeps as a signed byte.
            stream.SetLength(0);
            new SetOwnerIdPacket(ControlPointOwner.NoPackage).Write(writer);
            stream.Position = 0;
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(-1, reader.ReadInt());
        }

        [TestMethod]
        public void UseCarriesExtraArgumentsAfterTheThreeFixedOnes()
        {
            // Recv_Use(actorId, curStateId, windupTimeMs, *args): with no extras the tuple stays at three.
            var plain = new UsePacket(0x100000002UL, UseObjectState.MechpadOnReady, 100);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            plain.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual((uint)UseObjectState.MechpadOnReady, reader.ReadUInt());
            Assert.AreEqual(100, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);

            // mechpad.py OnBeforeUse(actorId, boardingTimeMs, effectTypeId): two extras.
            var boarding = new UsePacket(0x100000002UL, UseObjectState.MechpadBoarding, 100);
            boarding.Args.Add(3000);
            boarding.Args.Add(457);
            stream.SetLength(0);
            boarding.Write(writer);
            stream.Position = 0;
            Assert.AreEqual(5, reader.ReadTuple());
            reader.ReadULong();
            reader.ReadUInt();
            reader.ReadInt();
            Assert.AreEqual(3000, reader.ReadInt());
            Assert.AreEqual(457, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void AChannelAnswersWithItsMapsPointsUnheldAndNew()
        {
            var edmundRange = new MapChannel { MapInfo = new MapInfo(2374u, "adv_wargame_edmundrange2", 0u, 0u) };
            var statuses = ControlPointManager.Instance.StatusFor(edmundRange);
            CollectionAssert.AreEqual(new uint[] { 8, 9, 10, 11, 12, 13, 14 }, statuses.Select(status => status.ControlPointId).ToArray());
            Assert.IsTrue(statuses.All(status => status.OwnerId == null && status.StateId == ControlPointState.New && status.EndTime == 0u));

            // A map with no control points answers with an empty list rather than someone else's points.
            var wilderness = new MapChannel { MapInfo = new MapInfo(1220u, "Adv_Foreas_Concordia_Wilderness", 0u, 0u) };
            Assert.AreEqual(0, ControlPointManager.Instance.StatusFor(wilderness).Count);

            // A channel without a map yet answers with nothing, and a null channel is safe.
            Assert.AreEqual(0, ControlPointManager.Instance.StatusFor(new MapChannel()).Count);
            Assert.AreEqual(0, ControlPointManager.Instance.StatusFor(null).Count);

            // Two channels of the same map keep separate state, so an owner set on one is not on the other.
            var second = new MapChannel { MapInfo = new MapInfo(2374u, "adv_wargame_edmundrange2", 0u, 0u), ClientList = new System.Collections.Generic.List<Rasa.Game.Client>() };
            Assert.IsTrue(ControlPointManager.Instance.SetOwner(second, 12u, ControlPointOwner.RedTeam, ControlPointState.War, 5000u));
            Assert.AreEqual(1L, ControlPointManager.Instance.StatusFor(second).Single(status => status.ControlPointId == 12u).OwnerId);
            Assert.IsNull(ControlPointManager.Instance.StatusFor(edmundRange).Single(status => status.ControlPointId == 12u).OwnerId);

            // A point the map has not is refused rather than invented.
            Assert.IsFalse(ControlPointManager.Instance.SetOwner(second, 4u, ControlPointOwner.RedTeam, ControlPointState.War, 5000u));
            Assert.IsFalse(ControlPointManager.Instance.SetOwner(second, 215u, ControlPointOwner.RedTeam, ControlPointState.War, 5000u));

            ControlPointManager.Instance.Forget(edmundRange);
            ControlPointManager.Instance.Forget(second);
        }
    }
}
