using System;
using System.IO;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.ClientMethod.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class PlayerDeathPacketTests
    {
        [TestMethod]
        public void PlayerDeadPreservesSourceAndAllRequiredHospitalFields()
        {
            var hospital = new GraveyardInfo(3, new Vector3(10.25f, -20.5f, 30.75f), false);
            var packet = new PlayerDeadPacket(0x100000002UL, new[] { hospital }, true);
            Assert.AreEqual(GameOpcode.PlayerDead, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual(1, reader.ReadList());
            ReadHospital(reader, 3, hospital.Position, false);
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void EmptyHospitalChoiceIsAListAndNotNone()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new PlayerDeadPacket(0, Array.Empty<GraveyardInfo>()).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(0UL, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void QueuedHospitalChoiceKeepsItsOriginalSnapshot()
        {
            var original = new GraveyardInfo(6, new Vector3(1, 2, 3), true);
            var hospitals = new[] { original };
            var packet = new PlayerDeadPacket(7, hospitals);
            hospitals[0] = new GraveyardInfo(5, Vector3.Zero, false);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(7UL, reader.ReadULong());
            Assert.AreEqual(1, reader.ReadList());
            ReadHospital(reader, 6, original.Position, true);
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void PlayerDeadRejectsNonIterableOrNoneHospitalEntries()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new PlayerDeadPacket(0, null));
            Assert.ThrowsException<ArgumentException>(() => new PlayerDeadPacket(0, new GraveyardInfo[] { null }));
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void DeadOnArrivalCarriesOnlyTheRevivalFlag(bool canRevive)
        {
            var packet = new DeadOnArrivalPacket(canRevive);
            Assert.AreEqual(GameOpcode.DeadOnArrival, packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(canRevive ? 1 : 0, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void KillFallbackHasNoArgumentsAndRevivedKeepsTheSourceEntityId()
        {
            var killed = new ActorKilledPacket();
            var revived = new RevivedPacket(0x100000002UL);
            Assert.AreEqual(GameOpcode.ActorKilled, killed.Opcode);
            Assert.AreEqual(GameOpcode.Revived, revived.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            killed.Write(writer);
            revived.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(0, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(3)]
        [DataRow(218)]
        [DataRow(-1)]
        public void ReviveMePreservesTheSignedHospitalIdForServerValidation(int id)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(1);
            writer.WriteInt(id);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new ReviveMePacket();
            Assert.AreEqual(GameOpcode.ReviveMe, packet.Opcode);
            packet.Read(reader);
            Assert.AreEqual((int?)id, packet.GraveyardId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ReviveMeAcceptsTheNormalUiNoneRequest()
        {
            using var stream = new MemoryStream(new byte[] { 0x81, 0x00 });
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new ReviveMePacket();
            packet.Read(reader);
            Assert.IsNull(packet.GraveyardId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void ReviveMeRejectsWrongArityAndNonIdentifierArguments()
        {
            foreach (var payload in new[]
            {
                new byte[] { 0x80 }, new byte[] { 0x82, 0x13, 0x13 },
                new byte[] { 0x71, 0x13 }, new byte[] { 0x81, 0x01 },
                new byte[] { 0x81, 0x02 }, new byte[] { 0x81, 0x40 },
                new byte[] { 0x81, 0x70 }
            })
            {
                using var stream = new MemoryStream(payload);
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.ThrowsException<InvalidClientMessageException>(() => new ReviveMePacket().Read(reader));
            }
        }

        [TestMethod]
        public void BurialIsASeparateEmptyRequest()
        {
            using var stream = new MemoryStream(new byte[] { 0x80 });
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new BuryMePacket();
            Assert.AreEqual(GameOpcode.BuryMe, packet.Opcode);
            packet.Read(reader);
            Assert.AreEqual(stream.Length, stream.Position);
            using var invalid = new MemoryStream(new byte[] { 0x81, 0x00 });
            using var invalidReader = new PythonReader(new BinaryReader(invalid));
            Assert.ThrowsException<InvalidClientMessageException>(() => packet.Read(invalidReader));
        }

        private static void ReadHospital(PythonReader reader, int id, Vector3 position, bool isSafe)
        {
            Assert.AreEqual(4, reader.ReadDictionary());
            Assert.AreEqual("Id", reader.ReadString());
            Assert.AreEqual(id, reader.ReadInt());
            Assert.AreEqual("pos", reader.ReadString());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((double)position.X, reader.ReadDouble());
            Assert.AreEqual((double)position.Y, reader.ReadDouble());
            Assert.AreEqual((double)position.Z, reader.ReadDouble());
            Assert.AreEqual("isSafe", reader.ReadString());
            Assert.AreEqual(isSafe ? 1 : 0, reader.ReadInt());
            Assert.AreEqual("name", reader.ReadString());
            reader.ReadNoneStruct();
        }
    }
}
