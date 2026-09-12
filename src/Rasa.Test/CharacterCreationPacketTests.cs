using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterCreationPacketTests
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void OriginalCreationMessagesDecodeThroughTheirRegisteredPacketTypes(bool firstFamily)
        {
            var opcode = firstFamily ? GameOpcode.CreateCharacter : GameOpcode.RequestCreateCharacterInSlot;
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var packet = (RequestCreateCharacterInSlotPacket)Activator.CreateInstance(router.GetPacketType(opcode));
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(firstFamily ? 6 : 7);
            if (!firstFamily) writer.WriteInt(3);
            writer.WriteUnicodeString("Fixture");
            writer.WriteUnicodeString("First");
            writer.WriteInt(1);
            writer.WriteDouble(1.02);
            writer.WriteDictionary(1);
            writer.WriteInt((int)EquipmentData.Hair);
            writer.WriteTuple(2);
            writer.WriteUInt(36);
            new Color(0x12345678).Write(writer);
            writer.WriteInt((int)Race.Human);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(opcode, packet.Opcode);
            Assert.AreEqual(firstFamily ? 1 : 3, packet.SlotNum);
            Assert.AreEqual("Fixture", packet.FamilyName);
            Assert.AreEqual("First", packet.CharacterName);
            Assert.AreEqual(1, packet.Gender);
            Assert.AreEqual(1.02, packet.Scale, 0.000001);
            Assert.AreEqual(Race.Human, packet.RaceId);
            Assert.AreEqual(36u, packet.AppearanceData[EquipmentData.Hair].Class);
            Assert.AreEqual(0x12345678u, packet.AppearanceData[EquipmentData.Hair].Color.Hue);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WrongCreationMessageShapeIsRejectedBeforeReadingItsFields(bool firstFamily)
        {
            RequestCreateCharacterInSlotPacket packet = firstFamily ? new CreateCharacterPacket() : new RequestCreateCharacterInSlotPacket();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(firstFamily ? 7 : 6);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidDataException>(() => packet.Read(reader));
        }

        [TestMethod]
        public void OversizedSlotCannotWrapToAnExistingPod()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(7);
            writer.WriteInt(257);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<OverflowException>(() => new RequestCreateCharacterInSlotPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("Fixture")]
        public void SelectionDistinguishesAnUnchosenFamilyFromAnExistingName(string family)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new BeginCharacterSelectionPacket(family, false, 10, false).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(5, reader.ReadTuple());
            if (string.IsNullOrEmpty(family)) reader.ReadNoneStruct();
            else Assert.AreEqual(family, reader.ReadUnicodeString());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(10u, reader.ReadUInt());
            var races = reader.ReadTuple();
            for (var i = 0; i < races; ++i) reader.ReadInt();
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
