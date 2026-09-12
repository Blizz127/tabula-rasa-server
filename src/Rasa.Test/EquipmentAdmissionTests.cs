using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        [DataTestMethod]
        [DataRow("level")]
        [DataRow("race")]
        [DataRow("skill")]
        [DataRow("condition")]
        [DataRow("dead")]
        [DataRow("wrong_slot_class")]
        public void RejectedEquipmentKeepsBothInstancesAndPersistedLocations(string reason)
        {
            var client = Login();
            var incoming = Item(client.Player.Inventory.PersonalInventory[1]);
            var oldWeapon = WeaponActionManager.CurrentWeapon(client);
            client.Player.CurrentWeaponAction = Reload(client, oldWeapon);
            if (reason == "level") incoming.ItemTemplate.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = 50;
            if (reason == "race") incoming.ItemTemplate.ItemInfo.RaceReq = 2;
            if (reason == "skill") incoming.ItemTemplate.EquipableInfo = new EquipableInfo(50, 1);
            if (reason == "condition") { incoming.CurrentHitPoints = 1; EntityClassManager.Instance.LoadedEntityClasses[WeaponClass].ItemClassInfo.MaxHitPoints = 200; }
            if (reason == "dead") client.Player.State = CharacterState.Dead;
            if (reason == "wrong_slot_class") EntityClassManager.Instance.LoadedEntityClasses[WeaponClass].EquipableClassInfo.EquipmentSlotId = EquipmentData.Helmet;
            Drain(client);
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(oldWeapon, WeaponActionManager.CurrentWeapon(client));
            Assert.AreSame(incoming, Item(client.Player.Inventory.PersonalInventory[1]));
            Assert.IsNotNull(client.Player.CurrentWeaponAction);
            Assert.AreEqual(0, Drain(client).OfType<InventoryRemoveItemPacket>().Count());
            using var context = Context();
            Assert.AreEqual(1u, context.CharacterInventoryEntries.Find(1u).InventoryType);
            Assert.AreEqual(9u, context.CharacterInventoryEntries.Find(5u).InventoryType);
        }

        [TestMethod]
        public void EquipmentSwapCommitsBothLocationsThenRefreshesSelectedWeapon()
        {
            var client = Login();
            var oldWeapon = WeaponActionManager.CurrentWeapon(client);
            var incoming = Item(client.Player.Inventory.PersonalInventory[1]);
            client.Player.CurrentWeaponAction = Reload(client, oldWeapon);
            Drain(client);
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(incoming, WeaponActionManager.CurrentWeapon(client));
            Assert.AreSame(oldWeapon, Item(client.Player.Inventory.PersonalInventory[1]));
            Assert.IsNull(client.Player.CurrentWeaponAction);
            var packets = Drain(client);
            Assert.AreEqual(2, packets.OfType<InventoryRemoveItemPacket>().Count());
            Assert.AreEqual(2, packets.OfType<InventoryAddItemPacket>().Count());
            using var context = Context();
            Assert.AreEqual(9u, context.CharacterInventoryEntries.Find(1u).InventoryType);
            Assert.AreEqual(1u, context.CharacterInventoryEntries.Find(5u).InventoryType);
            Assert.AreEqual(101u, context.CharacterInventoryEntries.Find(1u).CharacterId);
            Assert.AreEqual(12u, context.ItemEntries.Find(1u).AmmoCount);
            Assert.AreEqual(7u, context.ItemEntries.Find(5u).AmmoCount);
        }

        [DataTestMethod]
        [DataRow("late_write")]
        [DataRow("source_slot")]
        [DataRow("destination_owner")]
        [DataRow("destination_account")]
        [DataRow("duplicate_destination")]
        [DataRow("empty_stack")]
        [DataRow("memory_owner")]
        public void StaleOrFailedEquipmentSwapPublishesNoPartialMove(string failure)
        {
            var client = Login();
            var oldWeapon = WeaponActionManager.CurrentWeapon(client);
            var incoming = Item(client.Player.Inventory.PersonalInventory[1]);
            client.Player.CurrentWeaponAction = Reload(client, oldWeapon);
            using (var context = Context())
            {
                if (failure == "late_write") context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_second_swap BEFORE UPDATE ON character_inventory WHEN OLD.item_id = 5 BEGIN SELECT RAISE(ABORT, 'fixture second write'); END");
                if (failure == "source_slot") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET slot_id = 2 WHERE item_id = 1");
                if (failure == "destination_owner") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET character_id = 102 WHERE item_id = 5");
                if (failure == "destination_account") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET account_id = 20 WHERE item_id = 5");
                if (failure == "duplicate_destination") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET character_id = 101, slot_id = 1 WHERE item_id = 9");
                if (failure == "empty_stack") context.Database.ExecuteSqlRaw("UPDATE items SET stack_size = 0 WHERE item_id = 1");
            }
            if (failure == "memory_owner") incoming.OwnerId = 2;
            Drain(client);
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(oldWeapon, WeaponActionManager.CurrentWeapon(client));
            Assert.AreSame(incoming, Item(client.Player.Inventory.PersonalInventory[1]));
            Assert.IsNotNull(client.Player.CurrentWeaponAction);
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(1u, reopened.CharacterInventoryEntries.Find(1u).InventoryType);
            Assert.AreEqual(9u, reopened.CharacterInventoryEntries.Find(5u).InventoryType);
        }

        [TestMethod]
        public void HomeEquipmentSwapAndEmptyUnequipPreserveAccountStorageOwnership()
        {
            using (var context = Context())
            {
                AddItem(context, 14, 10, 0, InventoryType.HomeInventory, 4, WeaponTemplate, 1, 17);
                context.SaveChanges();
            }
            var client = Login();
            var homeWeapon = Item(client.Player.Inventory.HomeInventory[4]);
            var oldWeapon = WeaponActionManager.CurrentWeapon(client);
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.HomeInventory, SrcSlot = 4, DestSlot = 1 });
            Assert.AreSame(homeWeapon, WeaponActionManager.CurrentWeapon(client));
            Assert.AreEqual(101u, homeWeapon.OwnerId);
            Assert.AreSame(oldWeapon, Item(client.Player.Inventory.HomeInventory[4]));
            Assert.AreEqual(0u, oldWeapon.OwnerId);
            using (var context = Context())
            {
                Assert.AreEqual(101u, context.CharacterInventoryEntries.Find(14u).CharacterId);
                Assert.AreEqual(9u, context.CharacterInventoryEntries.Find(14u).InventoryType);
                Assert.AreEqual(0u, context.CharacterInventoryEntries.Find(5u).CharacterId);
                Assert.AreEqual(2u, context.CharacterInventoryEntries.Find(5u).InventoryType);
            }
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket { InventoryType = InventoryType.HomeInventory, SrcSlot = 5, DestSlot = 1 });
            Assert.IsNull(WeaponActionManager.CurrentWeapon(client));
            Assert.IsFalse(client.Player.WeaponReady);
            Assert.AreEqual(0u, homeWeapon.OwnerId);
            var anotherCharacter = Login(102);
            Assert.AreEqual(14u, Item(anotherCharacter.Player.Inventory.HomeInventory[5]).Id);
            Assert.AreEqual(5u, Item(anotherCharacter.Player.Inventory.HomeInventory[4]).Id);
        }

        [TestMethod]
        public void ArmorUsesItsOriginalClassSlotAndAllowsRemovingBrokenEquipment()
        {
            var client = Login();
            client.Player.Level = 1;
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            var incoming = Item(client.Player.Inventory.PersonalInventory[1]);
            var info = EntityClassManager.Instance.LoadedEntityClasses[WeaponClass];
            // Turn this isolated class fixture into helmet equipment.
            info.EquipableClassInfo.EquipmentSlotId = EquipmentData.Helmet;
            info.WeaponClassInfo = null;
            info.ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { RegenRate = 1 });
            incoming.ItemTemplate.WeaponInfo = null;
            incoming.ItemTemplate.ArmorValue = 100;
            foreach (var invalidSlot in new uint[] { 2, 13, 22, uint.MaxValue })
            {
                _inventory.RequestEquipArmor(client, new RequestEquipArmorPacket { SrcInventory = InventoryType.Personal, SrcSlot = 1, DestSlot = invalidSlot });
                Assert.AreSame(incoming, Item(client.Player.Inventory.PersonalInventory[1]));
            }
            _inventory.RequestEquipArmor(client, new RequestEquipArmorPacket { SrcInventory = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(incoming, Item(client.Player.Inventory.EquippedInventory[1]));
            Assert.AreEqual(0ul, client.Player.Inventory.PersonalInventory[1]);
            Assert.AreEqual((uint)WeaponClass, client.Player.AppearanceData[EquipmentData.Helmet].Class);
            incoming.CurrentHitPoints = 0;
            _inventory.RequestEquipArmor(client, new RequestEquipArmorPacket { SrcInventory = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(incoming, Item(client.Player.Inventory.PersonalInventory[1]));
            Assert.AreEqual(0ul, client.Player.Inventory.EquippedInventory[1]);
            using var context = Context();
            Assert.AreEqual(1u, context.CharacterInventoryEntries.Find(1u).InventoryType);
        }

        [TestMethod]
        public void AppearanceSaveFailureDoesNotLeaveCommittedArmorWithOldStats()
        {
            var client = Login();
            client.Player.Level = 1;
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            var incoming = Item(client.Player.Inventory.PersonalInventory[1]);
            var info = EntityClassManager.Instance.LoadedEntityClasses[WeaponClass];
            info.EquipableClassInfo.EquipmentSlotId = EquipmentData.Helmet;
            info.WeaponClassInfo = null;
            info.ArmorClassInfo = new ArmorClassInfo(new ArmorClassEntry { RegenRate = 1 });
            incoming.ItemTemplate.WeaponInfo = null;
            incoming.ItemTemplate.ArmorValue = 100;
            using (var context = Context())
                context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_appearance BEFORE INSERT ON character_appearance BEGIN SELECT RAISE(ABORT, 'fixture appearance save'); END");
            Drain(client);
            _inventory.RequestEquipArmor(client, new RequestEquipArmorPacket { SrcInventory = InventoryType.Personal, SrcSlot = 1, DestSlot = 1 });
            Assert.AreSame(incoming, Item(client.Player.Inventory.EquippedInventory[1]));
            Assert.IsTrue(client.Player.Attributes[Attributes.Armor].CurrentMax > 0);
            Assert.AreEqual((uint)WeaponClass, client.Player.AppearanceData[EquipmentData.Helmet].Class);
            Assert.AreEqual(1, Drain(client).OfType<EquipmentInfoPacket>().Count());
            using var reopened = Context();
            Assert.AreEqual(8u, reopened.CharacterInventoryEntries.Find(1u).InventoryType);
            Assert.AreEqual(0, reopened.CharacterAppearanceEntries.Count());
        }

        [TestMethod]
        public void InitialActorDataPublishesRaceBeforeControlAndEquipment()
        {
            var client = Login();
            client.Player.EntityClass = WeaponClass;
            client.Player.Race = Race.Thrax;
            var data = _manifestation.CreatePlayerEntityData(client);
            var race = data.OfType<RaceIdPacket>().Single();
            Assert.IsTrue(data.IndexOf(race) < data.FindIndex(p => p is ActorControllerInfoPacket));
            Assert.IsTrue(data.IndexOf(race) < data.FindIndex(p => p is EquipmentInfoPacket));
            client.Player.Race = Race.Human;
            using var stream = new MemoryStream(Serialize(race));
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(4, reader.ReadInt());
        }
    }
}
