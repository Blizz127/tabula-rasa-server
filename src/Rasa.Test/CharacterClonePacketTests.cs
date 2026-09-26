using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Client;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterClonePacketTests
    {
        // Original client/inputstate/charactercreation.pyo, OnCreateCharacter line 92,
        // offsets 70-97: source slot, destination slot, name, gender, height, appearance, race.
        [TestMethod]
        public void OriginalCloneMessageDecodesThroughItsRegisteredHandler()
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var packet = (RequestCloneCharacterToSlotPacket)Activator.CreateInstance(
                router.GetPacketType(GameOpcode.RequestCloneCharacterToSlot));
            using var stream = Message(2, 16, 1, 1.02);
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(2, packet.CloneSlotNum);
            Assert.AreEqual(16, packet.SlotNum);
            Assert.AreEqual("Clone", packet.CharacterName);
            Assert.AreEqual(1, packet.Gender);
            Assert.AreEqual(1.02, packet.Scale, 0.000001);
            Assert.AreEqual(Race.Human, packet.RaceId);
            Assert.AreEqual(36u, packet.AppearanceData[EquipmentData.Hair].Class);
            Assert.AreEqual(0x12345678u, packet.AppearanceData[EquipmentData.Hair].Color.Hue);
            Assert.AreEqual(CreateCharacterResult.Success, packet.Validate());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(6)]
        [DataRow(8)]
        public void WrongTupleShapeIsRejectedBeforeAnyFieldsAreRead(int count)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(count);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidDataException>(() => new RequestCloneCharacterToSlotPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(-255, 2, 0)]
        [DataRow(257, 2, 0)]
        [DataRow(1, -255, 0)]
        [DataRow(1, 257, 0)]
        [DataRow(1, 2, -256)]
        [DataRow(1, 2, 256)]
        public void IntegersCannotWrapIntoADifferentSlotOrGender(int source, int destination, int gender)
        {
            using var stream = Message(source, destination, gender, 1);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<OverflowException>(() => new RequestCloneCharacterToSlotPacket().Read(reader));
        }

        [DataTestMethod]
        [DataRow(double.NaN)]
        [DataRow(double.NegativeInfinity)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(0.899)]
        [DataRow(1.061)]
        [DataRow(0.89999991655349731)] // next single-precision value below the minimum
        [DataRow(1.06000006198883057)] // next single-precision value above the maximum
        public void InvalidHeightsAreRefusedByBothCreationPaths(double height)
        {
            using var stream = Message(1, 2, 0, height);
            using var reader = new PythonReader(new BinaryReader(stream));
            var clone = new RequestCloneCharacterToSlotPacket();
            clone.Read(reader);
            Assert.AreEqual(CreateCharacterResult.InvalidCharacterHeight, clone.Validate());
            var create = new RequestCreateCharacterInSlotPacket
                { CharacterName = "First", FamilyName = "Family", Scale = height, RaceId = Race.Human };
            Assert.AreEqual(CreateCharacterResult.InvalidCharacterHeight, create.Validate());
        }

        [DataTestMethod]
        [DataRow(0.9)]
        [DataRow(1.06)]
        public void OriginalHeightBoundariesRemainValid(double height)
        {
            var clone = new RequestCloneCharacterToSlotPacket
                { CharacterName = "Clone", Scale = height, RaceId = Race.Human };
            var create = new RequestCreateCharacterInSlotPacket
                { CharacterName = "First", FamilyName = "Family", Scale = height, RaceId = Race.Human };
            Assert.AreEqual(CreateCharacterResult.Success, clone.Validate());
            Assert.AreEqual(CreateCharacterResult.Success, create.Validate());

            // Exercise the single-precision protocol form too. 0.9 becomes
            // 0.8999999761581421; rejecting it rejects the original slider endpoint.
            using var stream = Message(1, 2, 0, height);
            using var reader = new PythonReader(new BinaryReader(stream));
            clone.Read(reader);
            create.Scale = clone.Scale;
            Assert.AreEqual(CreateCharacterResult.Success, clone.Validate());
            Assert.AreEqual(CreateCharacterResult.Success, create.Validate());
        }

        [TestMethod]
        public void CloneValidationRejectsNullNameAndUndefinedGenderOrRace()
        {
            var clone = new RequestCloneCharacterToSlotPacket { Scale = 1, RaceId = Race.Human };
            Assert.AreEqual(CreateCharacterResult.InvalidEncoding, clone.Validate());
            clone.CharacterName = "Clone";
            clone.Gender = 2;
            Assert.AreEqual(CreateCharacterResult.InvalidEncoding, clone.Validate());
            clone.Gender = 0;
            clone.RaceId = (Race)5;
            Assert.AreEqual(CreateCharacterResult.CharacterCreationInvalidRace, clone.Validate());
        }

        private static MemoryStream Message(int source, int destination, int gender, double height)
        {
            var stream = new MemoryStream();
            var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(7);
            writer.WriteInt(source);
            writer.WriteInt(destination);
            writer.WriteUnicodeString("Clone");
            writer.WriteInt(gender);
            writer.WriteDouble(height);
            writer.WriteDictionary(1);
            writer.WriteInt((int)EquipmentData.Hair);
            writer.WriteTuple(2);
            writer.WriteUInt(36);
            new Color(0x12345678).Write(writer);
            writer.WriteInt((int)Race.Human);
            stream.Position = 0;
            return stream;
        }
    }
}
