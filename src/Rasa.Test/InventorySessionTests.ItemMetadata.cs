using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.Char.Items;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        [TestMethod]
        public void InstanceModulesAndRestrictionsSurviveLoginAndDoNotAffectTheSameTemplate()
        {
            var modules = new[] { new ItemLootModule(900178, null), new ItemLootModule(900312, 2) };
            using (var context = Context())
            {
                var row = context.ItemEntries.Find(5u);
                row.LootModulesJson = ItemLootModules.Serialize(modules);
                row.TradableOverride = false;
                row.SellableOverride = false;
                context.SaveChanges();
            }
            var first = Login();
            var customized = Item(first.Player.Inventory.WeaponDrawer[1]);
            CollectionAssert.AreEqual(modules, customized.LootModules.ToArray());
            Assert.IsFalse(customized.IsTradable);
            Assert.IsFalse(customized.IsSellable);
            var plain = Item(first.Player.Inventory.PersonalInventory[1]);
            Assert.AreSame(customized.ItemTemplate, plain.ItemTemplate);
            Assert.AreEqual(0, plain.LootModules.Count);
            Assert.IsTrue(plain.IsTradable);
            Assert.IsNull(plain.TradableOverride);
            Assert.IsNull(plain.SellableOverride);

            // Neither a queued packet nor reconnect may depend on the old object.
            customized.LootModules = new[] { new ItemLootModule(123, 9) };
            customized.TradableOverride = true;
            var second = Login();
            var reloaded = Item(second.Player.Inventory.WeaponDrawer[1]);
            Assert.AreNotSame(customized, reloaded);
            CollectionAssert.AreEqual(modules, reloaded.LootModules.ToArray());
            Assert.IsFalse(reloaded.IsTradable);
            Assert.IsFalse(reloaded.IsSellable);
            Assert.AreEqual(0, Item(second.Player.Inventory.PersonalInventory[1]).LootModules.Count);
        }

        [TestMethod]
        public void SplittingSharedStoragePreservesMetadataWithoutSharingMutableState()
        {
            var modules = new[] { new ItemLootModule(42, 3), new ItemLootModule(41, null) };
            uint splitId;
            using (var context = Context())
            {
                var row = context.ItemEntries.Find(2u);
                row.LootModulesJson = ItemLootModules.Serialize(modules);
                row.TradableOverride = false;
                row.SellableOverride = true;
                context.SaveChanges();
                var split = new CharacterInventoryRepository(context).TrySplitItemStack(
                    10, 101, (uint)InventoryType.HomeInventory, 3, 2, 27, AmmoTemplate,
                    (uint)InventoryType.Personal, 51, 5, 1);
                Assert.IsNotNull(split);
                splitId = split.ItemId;
            }
            var client = Login();
            var source = Item(client.Player.Inventory.HomeInventory[3]);
            var splitItem = Item(client.Player.Inventory.PersonalInventory[51]);
            Assert.AreEqual(22u, source.StackSize);
            Assert.AreEqual(splitId, splitItem.Id);
            Assert.AreEqual(5u, splitItem.StackSize);
            CollectionAssert.AreEqual(modules, source.LootModules.ToArray());
            CollectionAssert.AreEqual(modules, splitItem.LootModules.ToArray());
            Assert.IsFalse(splitItem.IsTradable);
            Assert.IsTrue(splitItem.IsSellable);
            splitItem.LootModules = new[] { new ItemLootModule(99, 1) };
            CollectionAssert.AreEqual(modules, source.LootModules.ToArray());
        }

        [TestMethod]
        public void VendorSaleAndTradeCommitRejectInstanceRestrictions()
        {
            var client = Login();
            var item = Item(client.Player.Inventory.PersonalInventory[1]);
            item.ItemTemplate.HasSellableFlag = true;
            item.SellableOverride = false;
            client.Player.Credits[CurencyType.Credits] = 100;
            new NpcManager(_factory, new MissionManager(_factory)).RequestVendorSale(client,
                new RequestVendorSalePacket { ItemEntityId = item.EntityId, Quantity = 1 });
            Assert.AreEqual(item.EntityId, client.Player.Inventory.PersonalInventory[1]);
            Assert.AreEqual(100, client.Player.Credits[CurencyType.Credits]);
            using (var context = Context())
                Assert.IsNotNull(context.CharacterInventoryEntries.Find(item.Id));

            // Exercise the final trade ownership/restriction guard as well as the
            // offer-time guard: an override changed during negotiation cannot slip through.
            var guard = typeof(TradeManager).GetMethod("StillHolds", BindingFlags.NonPublic | BindingFlags.Static);
            var offered = new List<ulong> { item.EntityId };
            Assert.IsTrue((bool)guard.Invoke(null, new object[] { client, offered }));
            item.TradableOverride = false;
            Assert.IsFalse((bool)guard.Invoke(null, new object[] { client, offered }));
            item.TradableOverride = null;
            Assert.IsTrue((bool)guard.Invoke(null, new object[] { client, offered }));
        }

        [DataTestMethod]
        [DataRow(true, false)]
        [DataRow(false, true)]
        public void InventoryPlacementDoesNotEraseModulesOrRestrictionsByMerging(bool modules, bool restriction)
        {
            var client = Login();
            var existing = Item(client.Player.Inventory.PersonalInventory[50]);
            var incoming = new Item
            {
                ItemTemplateId = AmmoTemplate, ItemTemplate = existing.ItemTemplate,
                StackSize = 5, Crafter = "", CurrentHitPoints = 1,
                LootModules = modules ? new[] { new ItemLootModule(42, null) } : null,
                TradableOverride = restriction ? false : (bool?)null
            };
            using (var context = Context())
                incoming.Id = new ItemRepository(context).CreateItem(incoming);
            EntityManager.Instance.RegisterEntity(incoming.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(incoming.EntityId, incoming);
            Assert.AreSame(incoming, _inventory.AddItemToInventory(client, incoming));
            Assert.AreEqual(40u, existing.StackSize);
            Assert.AreEqual(incoming.EntityId, client.Player.Inventory.PersonalInventory[51]);
            var reloaded = Login();
            var restored = Item(reloaded.Player.Inventory.PersonalInventory[51]);
            Assert.AreEqual(incoming.Id, restored.Id);
            Assert.AreEqual(5u, restored.StackSize);
            Assert.AreEqual(modules ? 1 : 0, restored.LootModules.Count);
            Assert.AreEqual(incoming.TradableOverride, restored.TradableOverride);
        }
    }
}
