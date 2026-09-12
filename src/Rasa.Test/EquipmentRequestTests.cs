using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Protocol;

namespace Rasa.Test
{
    [TestClass]
    public class EquipmentRequestTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void EquipTransportRetainsOriginalSwapFieldsIncludingHomeAndClan(bool weapon)
        {
            foreach (var type in new[] { InventoryType.Personal, InventoryType.HomeInventory, InventoryType.ClanInventory })
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));
                writer.WriteTuple(3); writer.WriteUInt(uint.MaxValue); writer.WriteInt((int)type); writer.WriteUInt(4);
                stream.Position = 0;
                using var reader = new PythonReader(new BinaryReader(stream));
                if (weapon)
                {
                    var request = new RequestEquipWeaponPacket(); request.Read(reader);
                    Assert.AreEqual(uint.MaxValue, request.SrcSlot);
                    Assert.AreEqual(type, request.InventoryType);
                    Assert.AreEqual(4u, request.DestSlot);
                }
                else
                {
                    var request = new RequestEquipArmorPacket(); request.Read(reader);
                    Assert.AreEqual(uint.MaxValue, request.SrcSlot);
                    Assert.AreEqual(type, request.SrcInventory);
                    Assert.AreEqual(4u, request.DestSlot);
                }
                Assert.AreEqual(stream.Length, stream.Position);
            }
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void WrongEquipFieldTypesUseTheHandledMalformedMessageException(bool weapon)
        {
            for (var malformed = -1; malformed < 3; malformed++)
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));
                if (malformed < 0) writer.WriteList(3);
                else writer.WriteTuple(3);
                for (var field = 0; field < 3; field++)
                    if (field == malformed) writer.WriteString("invalid");
                    else writer.WriteUInt(1);
                stream.Position = 0;
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.ThrowsException<InvalidClientMessageException>(() =>
                {
                    if (weapon) new RequestEquipWeaponPacket().Read(reader);
                    else new RequestEquipArmorPacket().Read(reader);
                });
            }
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void IncorrectEquipTupleArityFailsBeforeAnySlotsAreRead(bool weapon)
        {
            foreach (var count in new[] { 0, 2, 4 })
            {
                using var stream = new MemoryStream();
                using var writer = new PythonWriter(new BinaryWriter(stream));
                writer.WriteTuple(count);
                stream.Position = 0;
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.ThrowsException<InvalidClientMessageException>(() =>
                {
                    if (weapon) new RequestEquipWeaponPacket().Read(reader);
                    else new RequestEquipArmorPacket().Read(reader);
                });
            }
        }
    }
}
