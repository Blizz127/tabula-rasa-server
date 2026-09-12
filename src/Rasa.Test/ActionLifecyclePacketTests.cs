using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class ActionLifecyclePacketTests
    {
        [TestMethod]
        public void ActionFailedCarriesOnlyTheMatchingActionPair()
        {
            var packet = new ActionFailedPacket(ActionId.AaRecruitLightning, 5);
            Assert.AreEqual(GameOpcode.ActionFailed, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(194U, reader.ReadUInt());
            Assert.AreEqual(5U, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void InterruptCarriesSourceBeforeTheMatchingActionPair()
        {
            var packet = new ActionInterruptPacket(0x100000002UL, ActionId.AaRecruitLightning, 2);
            Assert.AreEqual(GameOpcode.ActionInterrupt, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual(194U, reader.ReadUInt());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ReuseTimesCarriesOneListOfActionAndRemainingMillisecondsPairs()
        {
            var packet = new ActionReuseTimesPacket(new[]
            {
                (ActionId.AaRecruitLightning, 1900L),
                (ActionId.AaRecruitSprint, 0L)
            });
            Assert.AreEqual(GameOpcode.ActionReuseTimes, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(194U, reader.ReadUInt());
            Assert.AreEqual(1900L, reader.ReadLong());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(401U, reader.ReadUInt());
            Assert.AreEqual(0L, reader.ReadLong());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void EmptyReuseSnapshotStillCarriesTheRequiredListArgument()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new ActionReuseTimesPacket(Array.Empty<(ActionId, long)>()).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void PowerUpdateRetainsTheFullSourceEntityId()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new UpdatePowerPacket(new ActorAttributes(Attributes.Power, 200, 200, 175, 0, 1),
                0x100000002UL).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(175, reader.ReadInt());
            Assert.AreEqual(200, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void QueuedPowerUpdatesKeepTheirOriginalResourceSnapshots()
        {
            var power = new ActorAttributes(Attributes.Power, 200, 200, 175, 3, 2);
            var first = new UpdatePowerPacket(power, 5);
            power.Current = 150;
            var second = new UpdatePowerPacket(power, 5);
            power.NormalMax = 500;
            power.CurrentMax = 450;
            power.Current = 100;
            power.RefreshAmount = 0;
            power.RefreshPeriod = 1;

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            first.Write(writer);
            second.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            foreach (var expected in new[] { 175, 150 })
            {
                Assert.AreEqual(4, reader.ReadTuple());
                Assert.AreEqual(expected, reader.ReadInt());
                Assert.AreEqual(200, reader.ReadInt());
                Assert.AreEqual(3, reader.ReadInt());
                Assert.AreEqual(5UL, reader.ReadULong());
            }
            Assert.AreEqual(200, first.Power.NormalMax);
            Assert.AreEqual(2, first.Power.RefreshPeriod);
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
