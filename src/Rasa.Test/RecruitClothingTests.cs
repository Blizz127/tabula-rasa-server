using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class RecruitClothingTests
    {
        // Original itemclass/equipmentdata/entityclass rows: no Armor augmentation,
        // no armorclass row and no item-template skill requirement for these clothes.
        private static (ItemTemplate Template, EntityClass Class) Recruit(uint templateId, uint classId, int slot, int durability)
            => (new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = templateId, ItemClass = classId }),
                new EntityClass(classId, "Recruit clothing", 776, 1,
                    new List<AugmentationType> { AugmentationType.Equipable, AugmentationType.Item }, false)
                {
                    EquipableClassInfo = new EquipableClassInfo((EquipmentData)slot),
                    ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = durability })
                });

        [DataTestMethod]
        [DataRow(122854, 10000068, 2, 35)]
        [DataRow(122855, 10000069, 16, 59)]
        [DataRow(122856, 10000070, 15, 70)]
        public void RecruitTooltipHasIterableEmptyResistancesWithoutInventingSkillOrArmor(
            int templateId, int classId, int slot, int durability)
        {
            var (template, entityClass) = Recruit((uint)templateId, (uint)classId, slot, durability);
            Assert.IsNull(template.EquipableInfo);
            Assert.IsNull(entityClass.ArmorClassInfo);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new ItemTemplateTooltipInfoPacket(template, entityClass).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual((uint)templateId, reader.ReadUInt());
            Assert.AreEqual((uint)classId, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Equipable, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple(), "Original tooltip and icon consumers unconditionally unpack the skill requirement pair.");
            reader.ReadNoneStruct(); // no skill identifier
            reader.ReadNoneStruct(); // no skill level
            Assert.AreEqual(0, reader.ReadList(), "Original elemental-icon consumer iterates this field unconditionally.");
            Assert.AreEqual((int)AugmentationType.Item, reader.ReadInt());
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadBool();
            Assert.AreEqual(durability, reader.ReadInt());
            reader.ReadInt();
            Assert.AreEqual(0, reader.ReadList());
            reader.ReadNoneStruct();
            reader.ReadNoneStruct();
            Assert.AreEqual(stream.Length, stream.Position, "No invented Armor augmentation follows the Item data.");
        }

        [TestMethod]
        public void EquipableTooltipPreservesPresentSkillRequirementAndResistance()
        {
            // Synthetic equipable metadata checks the non-null branch without
            // assigning an invented requirement to an original Recruit template.
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 1, ItemClass = 2 })
            {
                EquipableInfo = new EquipableInfo(50, 2)
            };
            template.EquipableInfo.ResistList.Add(new ResistanceData((DamageType)1, 7));
            var entityClass = new EntityClass(2, "Test equipable", 0, 1,
                new List<AugmentationType> { AugmentationType.Equipable }, false);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new ItemTemplateTooltipInfoPacket(template, entityClass).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(1u, reader.ReadUInt());
            Assert.AreEqual(2u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)AugmentationType.Equipable, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(50, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(7, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void RecruitClothingContributesNoArmorAndOnlyInvalidEquipmentLogsAnError()
        {
            var player = new Manifestation { Level = 1, Race = Race.Human };
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            var client = new Client(null, new ClientPacketHandler()) { Player = player };
            var originalClasses = new Dictionary<EntityClasses, EntityClass>();
            var items = new List<Item>();
            var originalOutput = Console.Out;
            using var output = new StringWriter();
            try
            {
                foreach (var (templateId, classId, slot, durability) in new[]
                {
                    (122854u, 10000068u, 2, 35), (122855u, 10000069u, 16, 59), (122856u, 10000070u, 15, 70)
                })
                {
                    var (template, entityClass) = Recruit(templateId, classId, slot, durability);
                    EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(template.Class, out var original);
                    originalClasses[template.Class] = original;
                    EntityClassManager.Instance.LoadedEntityClasses[template.Class] = entityClass;
                    var item = new Item { ItemTemplate = template, CurrentHitPoints = durability };
                    items.Add(item);
                    EntityManager.Instance.RegisterItem(item.EntityId, item);
                    player.Inventory.EquippedInventory[slot] = item.EntityId;
                }
                Console.SetOut(output);
                ManifestationManager.Instance.UpdateStatsValues(client, true);
                Assert.AreEqual(0, player.Attributes[Attributes.Armor].CurrentMax);
                Assert.AreEqual(0D, player.ArmorRegenRate);
                Assert.AreEqual(0, player.ResistanceData.Count);
                Assert.IsFalse(output.ToString().Contains("UpdateStatsValues:"), output.ToString());

                // Corrupt fixture metadata still receives a diagnostic.
                EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)10000068].EquipableClassInfo = null;
                ManifestationManager.Instance.UpdateStatsValues(client, false);
                StringAssert.Contains(output.ToString(), "Equipped item has neither armor nor equipable class data");
            }
            finally
            {
                Console.SetOut(originalOutput);
                foreach (var item in items) EntityManager.Instance.UnregisterItem(item.EntityId);
                foreach (var (classId, original) in originalClasses)
                    if (original == null) EntityClassManager.Instance.LoadedEntityClasses.Remove(classId);
                    else EntityClassManager.Instance.LoadedEntityClasses[classId] = original;
            }
        }
    }
}
