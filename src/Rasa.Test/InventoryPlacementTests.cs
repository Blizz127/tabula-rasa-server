using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        private void Move(Client client, string route, uint source, uint destination, int quantity)
        {
            switch (route)
            {
                case "personal": _inventory.PersonalInventory_MoveItem(client, new PersonalInventory_MoveItemPacket { SrcSlot = (int)source, DestSlot = (int)destination, Quantity = quantity }); break;
                case "home": _inventory.HomeInventory_MoveItem(client, new HomeInventory_MoveItemPacket { SrcSlot = source, DestSlot = destination, Quantity = quantity }); break;
                case "deposit": _inventory.RequestMoveItemToHomeInventory(client, new RequestMoveItemToHomeInventoryPacket { SrcSlot = source, DestSlot = destination, Quantity = quantity }); break;
                case "withdraw": _inventory.RequestTakeItemFromHomeInventory(client, new RequestTakeItemFromHomeInventoryPacket { SrcSlot = source, DestSlot = destination, Quantity = quantity }); break;
                default: throw new ArgumentException(route);
            }
        }

        [DataTestMethod]
        [DataRow("personal")]
        [DataRow("home")]
        [DataRow("deposit")]
        [DataRow("withdraw")]
        public void StorageSplitMovesOnlyRequestedQuantityAndPreservesPersistedInstanceData(string route)
        {
            var fromHome = route == "home" || route == "withdraw";
            var toHome = route == "home" || route == "deposit";
            var sourceId = fromHome ? 2u : 7u;
            var sourceSlot = fromHome ? 3u : 50u;
            var destinationSlot = toHome ? 4u : 51u;
            using (var context = Context())
            {
                var entry = context.ItemEntries.Find(sourceId);
                entry.CurrentHitPoints = 17;
                entry.Color = 0xA1B2C3D4;
                entry.AmmoCount = 9;
                entry.CrafterName = "original crafter";
                entry.CreatedAt = new DateTime(2009, 2, 10, 1, 2, 3);
                context.SaveChanges();
            }
            var client = Login();
            var source = Item((fromHome ? client.Player.Inventory.HomeInventory : client.Player.Inventory.PersonalInventory)[(int)sourceSlot]);
            var count = source.StackSize;
            Drain(client);
            Move(client, route, sourceSlot, destinationSlot, 7);
            Assert.AreEqual(count - 7, source.StackSize);
            var split = Item((toHome ? client.Player.Inventory.HomeInventory : client.Player.Inventory.PersonalInventory)[(int)destinationSlot]);
            Assert.IsNotNull(split);
            Assert.AreNotEqual(source.Id, split.Id);
            Assert.AreEqual(7u, split.StackSize);
            Assert.AreEqual(toHome ? 0u : 101u, split.OwnerId);
            Assert.AreEqual(17, split.CurrentHitPoints);
            Assert.AreEqual(0xA1B2C3D4u, split.Color);
            Assert.AreEqual(9u, split.CurrentAmmo);
            Assert.AreEqual("original crafter", split.Crafter);
            var packets = Drain(client);
            Assert.AreEqual(0, packets.OfType<InventoryRemoveItemPacket>().Count());
            var createdAt = packets.FindIndex(p => p is CreatePhysicalEntityPacket e && e.EntityId == split.EntityId);
            var addedAt = packets.FindIndex(p => p is InventoryAddItemPacket e && e.EntityId == split.EntityId);
            Assert.IsTrue(createdAt >= 0 && addedAt > createdAt);
            using var reloaded = Context();
            Assert.AreEqual(count - 7, reloaded.ItemEntries.Find(source.Id).StackSize);
            var persistedSplit = reloaded.ItemEntries.Find(split.Id);
            Assert.AreEqual(7u, persistedSplit.StackSize);
            Assert.AreEqual(new DateTime(2009, 2, 10, 1, 2, 3), persistedSplit.CreatedAt);
            Assert.AreEqual(9u, persistedSplit.AmmoCount);
            var location = reloaded.CharacterInventoryEntries.Single(e => e.ItemId == split.Id);
            Assert.AreEqual(toHome ? 0u : 101u, location.CharacterId);
            Assert.AreEqual(destinationSlot, location.SlotId);
            Assert.AreEqual(toHome ? 2u : 1u, location.InventoryType);
            var relogged = Login();
            var onLogin = Item((toHome ? relogged.Player.Inventory.HomeInventory : relogged.Player.Inventory.PersonalInventory)[(int)destinationSlot]);
            Assert.AreEqual(split.Id, onLogin.Id);
            Assert.AreEqual(7u, onLogin.StackSize);
        }

        [DataTestMethod]
        [DataRow("personal", 1, 2, 1, 16)]
        [DataRow("home", 4, 5, 14, 15)]
        [DataRow("deposit", 1, 4, 1, 14)]
        [DataRow("withdraw", 4, 1, 14, 1)]
        public void WholeNonstackableStorageSwapCommitsBothLocationsBeforePublication(string route, int sourceSlot, int destinationSlot, int sourceId, int destinationId)
        {
            using (var context = Context())
            {
                AddItem(context, 14, 10, 0, InventoryType.HomeInventory, 4, WeaponTemplate, 1, 14);
                AddItem(context, 15, 10, 0, InventoryType.HomeInventory, 5, WeaponTemplate, 1, 15);
                AddItem(context, 16, 10, 101, InventoryType.Personal, 2, WeaponTemplate, 1, 16);
                context.SaveChanges();
            }
            var client = Login();
            var sourceType = route == "home" || route == "withdraw" ? InventoryType.HomeInventory : InventoryType.Personal;
            var destinationType = route == "home" || route == "deposit" ? InventoryType.HomeInventory : InventoryType.Personal;
            Drain(client);
            Move(client, route, (uint)sourceSlot, (uint)destinationSlot, 1);
            Assert.AreEqual((uint)destinationId, Item((sourceType == InventoryType.Personal ? client.Player.Inventory.PersonalInventory : client.Player.Inventory.HomeInventory)[sourceSlot]).Id);
            Assert.AreEqual((uint)sourceId, Item((destinationType == InventoryType.Personal ? client.Player.Inventory.PersonalInventory : client.Player.Inventory.HomeInventory)[destinationSlot]).Id);
            var packets = Drain(client);
            Assert.AreEqual(2, packets.OfType<InventoryRemoveItemPacket>().Count());
            Assert.AreEqual(2, packets.OfType<InventoryAddItemPacket>().Count());
            Assert.IsTrue(packets.Take(2).All(p => p is InventoryRemoveItemPacket));
            using var reloaded = Context();
            var source = reloaded.CharacterInventoryEntries.Single(e => e.ItemId == sourceId);
            var destination = reloaded.CharacterInventoryEntries.Single(e => e.ItemId == destinationId);
            Assert.AreEqual((uint)destinationSlot, source.SlotId);
            Assert.AreEqual((uint)destinationType, source.InventoryType);
            Assert.AreEqual(destinationType == InventoryType.HomeInventory ? 0u : 101u, source.CharacterId);
            Assert.AreEqual((uint)sourceSlot, destination.SlotId);
            Assert.AreEqual((uint)sourceType, destination.InventoryType);
            Assert.AreEqual(sourceType == InventoryType.HomeInventory ? 0u : 101u, destination.CharacterId);
            Assert.AreEqual(1u, reloaded.ItemEntries.Find((uint)sourceId).StackSize);
        }

        [DataTestMethod]
        [DataRow("late_insert")]
        [DataRow("late_location")]
        [DataRow("stale_stack")]
        [DataRow("stale_template")]
        [DataRow("stale_destination")]
        [DataRow("source_owner")]
        [DataRow("stale_tabs")]
        [DataRow("locked_tab")]
        [DataRow("no_lockbox")]
        [DataRow("dead")]
        [DataRow("zero")]
        [DataRow("too_many")]
        public void FailedStorageSplitLeavesNoPartialQuantityOrNewItem(string failure)
        {
            var client = Login();
            var source = Item(client.Player.Inventory.PersonalInventory[50]);
            if (failure == "dead") client.Player.State = CharacterState.Dead;
            if (failure == "no_lockbox") source.ItemTemplate.NotPlaceableInLockbox = true;
            using (var context = Context())
            {
                if (failure == "late_insert") context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_split BEFORE INSERT ON items BEGIN SELECT RAISE(ABORT, 'fixture split insert'); END");
                if (failure == "late_location") context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_split_location BEFORE INSERT ON character_inventory BEGIN SELECT RAISE(ABORT, 'fixture split location'); END");
                if (failure == "stale_template") context.Database.ExecuteSqlRaw("UPDATE items SET item_template_id = 1 WHERE item_id = 7");
                if (failure == "stale_stack") context.Database.ExecuteSqlRaw("UPDATE items SET stack_size = 39 WHERE item_id = 7");
                if (failure == "stale_destination") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET character_id = 0, invenotry_type = 2, slot_id = 4 WHERE item_id = 3");
                if (failure == "source_owner") context.Database.ExecuteSqlRaw("UPDATE character_inventory SET character_id = 102 WHERE item_id = 7");
                if (failure == "stale_tabs") context.Database.ExecuteSqlRaw("UPDATE character_lockbox SET purashed_tabs = 2 WHERE account_id = 10");
            }
            Drain(client);
            Move(client, "deposit", 50, failure == "locked_tab" ? 96u : 4u, failure == "zero" ? 0 : failure == "too_many" ? 41 : 7);
            Assert.AreEqual(40u, source.StackSize);
            Assert.AreEqual(0ul, client.Player.Inventory.HomeInventory[4]);
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(failure == "stale_stack" ? 39u : 40u, reopened.ItemEntries.Find(7u).StackSize);
            Assert.AreEqual(13, reopened.ItemEntries.Count());
            Assert.AreEqual(13, reopened.CharacterInventoryEntries.Count());
        }

        [TestMethod]
        public void FailedWholeStorageSwapRollsBackEarlierLocationWrite()
        {
            using (var context = Context())
            {
                AddItem(context, 14, 10, 0, InventoryType.HomeInventory, 4, WeaponTemplate, 1, 14);
                context.SaveChanges();
                context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_storage_swap BEFORE UPDATE ON character_inventory WHEN OLD.item_id = 14 BEGIN SELECT RAISE(ABORT, 'fixture second move'); END");
            }
            var client = Login();
            Drain(client);
            Move(client, "deposit", 1, 4, 1);
            Assert.AreEqual(1u, Item(client.Player.Inventory.PersonalInventory[1]).Id);
            Assert.AreEqual(14u, Item(client.Player.Inventory.HomeInventory[4]).Id);
            Assert.AreEqual(0, Drain(client).Count);
            using var reopened = Context();
            Assert.AreEqual(1u, reopened.CharacterInventoryEntries.Find(1u).InventoryType);
            Assert.AreEqual(2u, reopened.CharacterInventoryEntries.Find(14u).InventoryType);
        }

        [DataTestMethod]
        [DataRow(1, 95, true)]
        [DataRow(1, 96, false)]
        [DataRow(2, 191, true)]
        [DataRow(2, 192, false)]
        [DataRow(5, 479, true)]
        [DataRow(5, 480, false)]
        public void StorageDestinationUsesOriginalNinetySixSlotTabBoundaries(int tabs, int slot, bool allowed)
        {
            using (var context = Context())
            {
                context.CharacterLockboxEntries.Find(10u).PurashedTabs = tabs;
                context.SaveChanges();
            }
            var client = Login();
            client.Player.LockboxTabs = tabs;
            Drain(client);
            Move(client, "deposit", 1, (uint)slot, 1);
            Assert.AreEqual(allowed, client.Player.Inventory.PersonalInventory[1] == 0);
            Assert.AreEqual(allowed ? 2 : 0, Drain(client).Count);
        }

        [TestMethod]
        public void WrongPersonalCategoryAndOutOfRangeWithdrawalLeaveItemsInPlace()
        {
            var client = Login();
            Drain(client);
            Move(client, "withdraw", 3, 0, 7); // Consumables belong to slots 50–99.
            Move(client, "withdraw", 3, 250, 7);
            Move(client, "withdraw", 480, 51, 7);
            Move(client, "personal", uint.MaxValue, 51, 7);
            Assert.AreEqual(27u, Item(client.Player.Inventory.HomeInventory[3]).StackSize);
            Assert.AreEqual(0, Drain(client).Count);
        }
        [TestMethod]
        public void LockboxRestrictionsAlsoApplyToEquipmentTransfers()
        {
            using (var context = Context())
            {
                AddItem(context, 14, 10, 0, InventoryType.HomeInventory, 96, WeaponTemplate, 1, 14);
                context.SaveChanges();
            }
            var client = Login();
            var equipped = Item(client.Player.Inventory.WeaponDrawer[1]);
            Drain(client);
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket
                { InventoryType = InventoryType.HomeInventory, SrcSlot = 96, DestSlot = 1 });
            Assert.AreSame(equipped, Item(client.Player.Inventory.WeaponDrawer[1]));
            equipped.ItemTemplate.NotPlaceableInLockbox = true;
            _inventory.RequestEquipWeapon(client, new RequestEquipWeaponPacket
                { InventoryType = InventoryType.HomeInventory, SrcSlot = 4, DestSlot = 1 });
            Assert.AreSame(equipped, Item(client.Player.Inventory.WeaponDrawer[1]));
            Assert.AreEqual(0ul, client.Player.Inventory.HomeInventory[4]);
            Assert.AreEqual(0, Drain(client).Count);
        }
    }

    [TestClass]
    public class InventoryMovePacketTests
    {
        private static ClientPythonPacket[] Packets() => new ClientPythonPacket[]
        {
            new PersonalInventory_MoveItemPacket(), new HomeInventory_MoveItemPacket(),
            new RequestMoveItemToHomeInventoryPacket(), new RequestTakeItemFromHomeInventoryPacket()
        };

        [TestMethod]
        public void EveryStorageMoveRetainsLongEncodedQuantity()
        {
            foreach (var packet in Packets())
            {
                using var stream = new MemoryStream(new byte[] { 0x83, 0x11, 0x12, 0x2F, 7, 0, 0, 0, 0, 0, 0, 0 });
                using var reader = new PythonReader(new BinaryReader(stream));
                packet.Read(reader);
                Assert.AreEqual(7, packet.GetType().GetProperty("Quantity").GetValue(packet));
                Assert.AreEqual(stream.Length, stream.Position);
            }
        }

        [DataTestMethod]
        [DataRow("arity")]
        [DataRow("slot_type")]
        [DataRow("negative_slot")]
        [DataRow("negative_quantity")]
        [DataRow("quantity_width")]
        [DataRow("long_tag")]
        public void InvalidStorageMoveFieldsUseHandledMessageException(string failure)
        {
            foreach (var packet in Packets())
            {
                using var stream = new MemoryStream();
                using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                {
                    writer.WriteTuple(failure == "arity" ? 2 : 3);
                    if (failure == "slot_type") writer.WriteString("1");
                    else writer.WriteInt(failure == "negative_slot" ? -1 : 1);
                    writer.WriteInt(2);
                    if (failure == "long_tag") stream.WriteByte(0x21);
                    else if (failure == "quantity_width") writer.WriteLong((long)int.MaxValue + 1);
                    else writer.WriteInt(failure == "negative_quantity" ? -1 : 7);
                }
                stream.Position = 0;
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.ThrowsException<InvalidClientMessageException>(() => packet.Read(reader));
            }
        }
    }
}
