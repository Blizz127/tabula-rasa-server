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
    public class WorldEquipmentTests
    {
        [TestMethod]
        public void WeaponArchetypeKeepsTheOriginalTemplateIdDistinctFromEntityClassId()
        {
            // Original weaponclass.lookup[6048] starts with template 1, not 6048.
            var info = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = 6048, WeaponTemplatId = 1, AttackActionId = 1, AttackActionArgId = 133,
                MinDamage = 55, MaxDamage = 55, DamageType = 1, ClipSize = 20, AmmoClassId = 3147
            });
            Assert.AreEqual(1U, info.WeaponTemplateid);
            Assert.AreEqual(ActionId.WeaponAttack, info.WeaponAttackActionId);
            Assert.AreEqual(133U, info.WeaponAttackArgId);
            Assert.AreEqual(55, info.MinDamage);
            Assert.AreEqual(55, info.MaxDamage);
        }

        [TestMethod]
        public void WeaponInfoRetainsAllQueuedFieldsAfterInstanceAndTemplateMutations()
        {
            var weapon = new WeaponInfo(new ItemTemplateWeaponEntry
            {
                AimRate = 1.75, ReloadTime = 3210, AltActionId = 174, AltActionArgId = 3,
                AeType = 2, AeRadius = 18, RecoilAmount = 6, CoolRate = 8,
                HeatPerShot = 2.5, ToolType = 15, AmmoPerShot = 2
            });
            var item = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                {
                    ItemTemplateId = 145, ItemClass = 6048
                }) { WeaponInfo = weapon },
                CurrentAmmo = 7, IsJammed = true, CammeraProfile = 4
            };
            var entityClass = new EntityClass(6048, "test", 0, 0, new List<AugmentationType>(), false)
            {
                WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry { ClipSize = 23 })
            };
            var packet = new WeaponInfoPacket(item, entityClass);
            item.CurrentAmmo = 23;
            item.IsJammed = false;
            item.CammeraProfile = 0;
            weapon.AimRate = 0;
            weapon.ReloadTime = 0;
            weapon.AltActionId = 0;
            weapon.AltActionArgId = 0;
            weapon.AeType = 0;
            weapon.AeRadius = 0;
            weapon.RecoilAmount = 0;
            weapon.CoolRate = 0;
            weapon.HeatPerShot = 0;
            weapon.ToolType = 0;
            weapon.AmmoPerShot = 0;
            entityClass.WeaponClassInfo.ClipSize = 0;

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(17, reader.ReadTuple());
            reader.ReadNoneStruct(); // existing unknown weapon name
            Assert.AreEqual(23U, reader.ReadUInt());
            Assert.AreEqual(7U, reader.ReadUInt());
            Assert.AreEqual(1.75, reader.ReadDouble());
            Assert.AreEqual(3210U, reader.ReadUInt());
            Assert.AreEqual(174U, reader.ReadUInt());
            Assert.AreEqual(3U, reader.ReadUInt());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(18U, reader.ReadUInt());
            Assert.AreEqual(6U, reader.ReadUInt());
            reader.ReadNoneStruct(); // existing unknown reuse override
            Assert.AreEqual(8U, reader.ReadUInt());
            Assert.AreEqual(2.5, reader.ReadDouble());
            Assert.AreEqual(15, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(4, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
