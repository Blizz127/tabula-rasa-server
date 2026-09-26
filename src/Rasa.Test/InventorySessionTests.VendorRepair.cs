using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;

namespace Rasa.Test
{
    public partial class InventorySessionTests
    {
        /// <summary>
        /// A repair charges what the client's repair tab showed for the item (vendorwindow._GetRepairPrice): the
        /// item-info buyback price times the missing condition, truncated, never below one credit. The fixture items
        /// stand at 82 of 100 hit points (condition 82).
        /// </summary>
        [TestMethod]
        public void VendorRepairChargesTheClientsRepairPrice()
        {
            var characters = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var realCharacters = characters.GetValue(null);
            characters.SetValue(null, Activator.CreateInstance(typeof(CharacterManager),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { _factory }, null));
            const ulong vendor = 0x7ffffff0UL;
            EntityManager.Instance.VendorItems[vendor] = new List<ulong>();
            try
            {
                var client = Login();
                client.Player.Credits[CurencyType.Credits] = 100;
                var npcs = new NpcManager(_factory, new MissionManager(_factory));
                var first = Item(client.Player.Inventory.PersonalInventory[1]);
                var second = Item(client.Player.Inventory.WeaponDrawer[1]);
                Assert.AreEqual(82, first.CurrentHitPoints);

                // Buyback 103: int(103 * 18 * 0.01) = int(18.54) = 18. The old charge rounded to 19.
                first.ItemTemplate.ItemInfo.BuyBackPrice = 103;
                npcs.RequestVendorRepair(client, new RequestVendorRepairPacket { VendorEntityId = vendor, ItemEntitesId = { first.EntityId } });
                Assert.AreEqual(100, first.CurrentHitPoints);
                Assert.AreEqual(82, client.Player.Credits[CurencyType.Credits]);

                // Buyback 0 still costs one credit - what the client showed for every repair while the price was never
                // sent, and the price it shows for an item with no sale value. The old charge was free.
                second.ItemTemplate.ItemInfo.BuyBackPrice = 0;
                npcs.RequestVendorRepair(client, new RequestVendorRepairPacket { VendorEntityId = vendor, ItemEntitesId = { second.EntityId, first.EntityId } });
                Assert.AreEqual(100, second.CurrentHitPoints);
                // The already-repaired item is not charged again.
                Assert.AreEqual(81, client.Player.Credits[CurencyType.Credits]);
            }
            finally
            {
                EntityManager.Instance.VendorItems.Remove(vendor);
                characters.SetValue(null, realCharacters);
            }
        }
    }
}
