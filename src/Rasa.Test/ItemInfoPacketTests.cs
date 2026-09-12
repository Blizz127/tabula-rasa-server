using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class ItemInfoPacketTests
    {
        [DataTestMethod]
        [DataRow(Race.Human, 1)]
        [DataRow(Race.Forean, 2)]
        [DataRow(Race.Brann, 3)]
        [DataRow(Race.Thrax, 4)]
        public void RaceIdUsesOriginalMethodAndNumericConstants(Race race, int expected)
        {
            var packet = new RaceIdPacket(race);
            Assert.AreEqual(760, (int)packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(expected, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ItemInfoKeepsQueuedFieldsAndUsesTheOriginalNegativeTradeFlag(bool tradable)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry
            {
                ItemTemplateId = 145, ItemClass = 6048
            })
            {
                HasSellableFlag = true, HasCharacterUniqueFlag = false,
                HasAccountUniqueFlag = true, HasBoEFlag = false, QualityId = 3,
                BoundToCharacter = true, NotPlaceableInLockbox = false,
                InventoryCategory = InventoryCategory.Equipment
            };
            template.ItemInfo.Tradable = tradable;
            var item = new Item { ItemTemplate = template, CurrentHitPoints = 37, Crafter = "Original crafter" };
            var classInfo = new EntityClass(6048, "fixture", 0, 0, new List<AugmentationType>(), false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 160 })
            };
            var packet = new ItemInfoPacket(item, classInfo);

            item.CurrentHitPoints = 160;
            item.Crafter = "Changed";
            classInfo.ItemClassInfo.MaxHitPoints = 500;
            template.ItemTemplateId = 999;
            template.HasSellableFlag = false;
            template.HasCharacterUniqueFlag = true;
            template.HasAccountUniqueFlag = false;
            template.HasBoEFlag = true;
            template.QualityId = 0;
            template.BoundToCharacter = false;
            template.ItemInfo.Tradable = !tradable;
            template.NotPlaceableInLockbox = true;
            template.InventoryCategory = InventoryCategory.Misc;

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(15, reader.ReadTuple());
            Assert.AreEqual(37, reader.ReadInt());
            Assert.AreEqual(160, reader.ReadInt());
            Assert.AreEqual("Original crafter", reader.ReadString());
            Assert.AreEqual(145u, reader.ReadUInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.IsTrue(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(0, reader.ReadList()); // module data remains unimplemented
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(3, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
            var notTradable = reader.ReadBool();
            Assert.AreEqual(!tradable, notTradable);
            Assert.AreEqual(tradable, !notTradable); // original client's UNARY_NOT consumer
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual((int)InventoryCategory.Equipment, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
