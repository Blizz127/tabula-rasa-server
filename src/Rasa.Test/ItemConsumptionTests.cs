using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Inventory.Client;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class ItemConsumptionTests
    {
        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name == "get_Items") return new ItemRepository(Context);
                if (method.Name == "Dispose") { Context.Dispose(); return null; }
                throw new NotSupportedException(method.Name);
            }
        }
        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public int Calls;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar()
            {
                Calls++;
                var result = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)result).Context = WeaponReloadPersistenceTests.Context(_connection);
                return result;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private SqliteConnection _connection;
        private Factory _factory;
        private InventoryManager _inventory;
        private Client _client;
        private Item _personal, _home;
        private Logger.LoggerConfig _oldLogger;

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _factory = new Factory(_connection);
            _inventory = (InventoryManager)Activator.CreateInstance(typeof(InventoryManager),
                BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { _factory }, null);
            _client = new Client(null, new ClientPacketHandler())
                { State = ClientState.Ingame, Player = new Manifestation { Id = 101 } };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client,
                new GameAccountEntry { Id = 10, SelectedSlot = 2 });
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _client.Player.Inventory.HomeInventory.AddRange(new ulong[480]);
            _personal = Register(1, 101);
            _home = Register(2, 0);
            _client.Player.Inventory.PersonalInventory[50] = _personal.EntityId;
            _client.Player.Inventory.HomeInventory[50] = _home.EntityId;
            using var context = Context();
            context.Database.EnsureCreated();
            for (uint id = 1; id <= 5; id++)
                context.ItemEntries.Add(new ItemEntry { ItemId = id, ItemTemplateId = 145, StackSize = 10,
                    AmmoCount = 7, CurrentHitPoints = 82, Color = 12345, CrafterName = "fixture" });
            context.CharacterInventoryEntries.AddRange(
                new CharacterInventoryEntry(10, 101, 1, 50, 1),
                new CharacterInventoryEntry(10, 0, 2, 50, 2),
                new CharacterInventoryEntry(10, 102, 1, 50, 3),
                new CharacterInventoryEntry(20, 0, 2, 50, 4),
                new CharacterInventoryEntry(10, 101, 1, 51, 5));
            context.SaveChanges();
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var item in new[] { _personal, _home })
            {
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.UnregisterEntity(item.EntityId);
            }
            _connection.Dispose();
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
        }

        [DataTestMethod]
        [DataRow(false, 3)]
        [DataRow(false, 10)]
        [DataRow(true, 3)]
        [DataRow(true, 10)]
        public void RepositoryConsumesOnlyTheExpectedItemAndPreservesAllOtherRows(bool home, int amount)
        {
            var otherRows = Snapshot(home ? 2u : 1u);
            using (var context = Context())
                Assert.IsTrue(new ItemRepository(context).TryConsumeItemStack(10, home ? 0u : 101u,
                    home ? 2u : 1u, 50, home ? 2u : 1u, 10, (uint)amount));
            using var reopened = Context();
            var id = home ? 2u : 1u;
            var item = reopened.ItemEntries.Find(id);
            Assert.AreEqual((uint)(10 - amount), item.StackSize);
            Assert.AreEqual(7u, item.AmmoCount);
            Assert.AreEqual(82, item.CurrentHitPoints);
            Assert.AreEqual(12345u, item.Color);
            Assert.AreEqual("fixture", item.CrafterName);
            var link = reopened.CharacterInventoryEntries.Find(id);
            if (amount == 10) Assert.IsNull(link);
            else
            {
                Assert.AreEqual(home ? 0u : 101u, link.CharacterId);
                Assert.AreEqual(home ? 2u : 1u, link.InventoryType);
                Assert.AreEqual(50u, link.SlotId);
            }
            CollectionAssert.AreEqual(otherRows, Snapshot(id));
        }

        [DataTestMethod]
        [DataRow("count")]
        [DataRow("account")]
        [DataRow("character")]
        [DataRow("type")]
        [DataRow("slot")]
        [DataRow("missing_item")]
        [DataRow("missing_link")]
        public void StaleExpectedStateCannotChangeDatabaseRows(string changed)
        {
            using (var context = Context())
            {
                var sql = changed switch
                {
                    "count" => "UPDATE items SET stack_size=9 WHERE item_id=1",
                    "account" => "UPDATE character_inventory SET account_id=20 WHERE item_id=1",
                    "character" => "UPDATE character_inventory SET character_id=102 WHERE item_id=1",
                    "type" => "UPDATE character_inventory SET invenotry_type=9 WHERE item_id=1",
                    "slot" => "UPDATE character_inventory SET slot_id=51 WHERE item_id=1",
                    "missing_item" => "DELETE FROM items WHERE item_id=1",
                    _ => "DELETE FROM character_inventory WHERE item_id=1"
                };
                context.Database.ExecuteSqlRaw(sql);
            }
            var before = Snapshot();
            using (var context = Context())
                Assert.IsFalse(new ItemRepository(context).TryConsumeItemStack(10, 101, 1, 50, 1, 10, 10));
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FailedInventoryLinkRemovalRollsBackTheEarlierStackUpdate(bool home)
        {
            using (var context = Context())
                context.Database.ExecuteSqlRaw("CREATE TRIGGER prevent_consumption BEFORE DELETE ON character_inventory BEGIN SELECT RAISE(IGNORE); END");
            var before = Snapshot();
            using (var context = Context())
                Assert.IsFalse(new ItemRepository(context).TryConsumeItemStack(10, home ? 0u : 101u,
                    home ? 2u : 1u, 50, home ? 2u : 1u, 10, 10));
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [DataTestMethod]
        [DataRow(0L)]
        [DataRow(11L)]
        [DataRow(4294967295L)]
        public void ZeroOrExcessConsumptionCannotUnderflowAStack(long amount)
        {
            var before = Snapshot();
            using (var context = Context())
                Assert.IsFalse(new ItemRepository(context).TryConsumeItemStack(10, 101, 1, 50, 1, 10, (uint)amount));
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [DataTestMethod]
        [DataRow(0, 1, 50, 1)]
        [DataRow(101, 2, 50, 2)]
        [DataRow(101, 9, 0, 1)]
        [DataRow(101, 1, 250, 1)]
        [DataRow(0, 2, 480, 2)]
        [DataRow(101, 1, 50, 3)]
        [DataRow(0, 2, 50, 4)]
        public void WrongOwnerTypeSlotOrItemIdentityCannotDeleteAnotherInventoryRow(int owner, int type, int slot, int id)
        {
            var before = Snapshot();
            using (var context = Context())
                Assert.IsFalse(new ItemRepository(context).TryConsumeItemStack(10, (uint)owner, (uint)type,
                    (uint)slot, (uint)id, 10, 10));
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [DataTestMethod]
        [DataRow(false, 3)]
        [DataRow(false, 10)]
        [DataRow(true, 3)]
        [DataRow(true, 10)]
        public void RealDestroyHandlersCommitBeforePublishingPartialOrFullChanges(bool home, int amount)
        {
            var item = home ? _home : _personal;
            var otherRows = Snapshot(item.Id);
            Destroy(home, item.EntityId, (uint)amount);
            Assert.AreEqual((uint)(10 - amount), item.StackSize);
            var packets = Drain();
            var slots = home ? _client.Player.Inventory.HomeInventory : _client.Player.Inventory.PersonalInventory;
            using var reopened = Context();
            Assert.AreEqual(item.StackSize, reopened.ItemEntries.Find(item.Id).StackSize);
            if (amount == 10)
            {
                Assert.AreEqual(0ul, slots[50]);
                Assert.IsNull(EntityManager.Instance.GetItem(item.EntityId));
                Assert.IsNull(reopened.CharacterInventoryEntries.Find(item.Id));
                var removal = packets.OfType<InventoryRemoveItemPacket>().Single();
                Assert.AreEqual(home ? InventoryType.HomeInventory : InventoryType.Personal, removal.InventoryType);
                Assert.AreEqual(item.EntityId, removal.EntityId);
                Assert.AreEqual(1, packets.Count(p => p.Opcode == GameOpcode.DestroyPhysicalEntity));
                Assert.AreEqual(2, packets.Count);
                Destroy(home, item.EntityId, (uint)amount);
                Assert.AreEqual(0, Drain().Count);
                Assert.AreEqual(1, _factory.Calls);
            }
            else
            {
                Assert.AreEqual(item.EntityId, slots[50]);
                Assert.AreSame(item, EntityManager.Instance.GetItem(item.EntityId));
                Assert.IsNotNull(reopened.CharacterInventoryEntries.Find(item.Id));
                Assert.AreEqual(7u, packets.OfType<SetStackCountPacket>().Single().StackSize);
                Assert.AreEqual(1, packets.Count);
            }
            CollectionAssert.AreEqual(otherRows, Snapshot(item.Id));
        }

        [DataTestMethod]
        [DataRow("owner")]
        [DataRow("slot")]
        [DataRow("slot_entity")]
        [DataRow("unregistered")]
        [DataRow("forged_instance")]
        [DataRow("zero")]
        [DataRow("excess")]
        public void RejectedInMemoryIdentityAndQuantityNeverReachTheDatabase(string changed)
        {
            var item = _personal;
            if (changed == "owner") item.OwnerId = 2; // Roster slot is not character identity.
            if (changed == "slot") item.OwnerSlotId = uint.MaxValue;
            if (changed == "slot_entity") _client.Player.Inventory.PersonalInventory[50] = _home.EntityId;
            if (changed == "unregistered") EntityManager.Instance.UnregisterItem(item.EntityId);
            if (changed == "forged_instance") item = new Item { Id = 1, OwnerId = 101, OwnerSlotId = 50, StackSize = 10 };
            var quantity = changed == "zero" ? 0u : changed == "excess" ? uint.MaxValue : 3u;
            var before = Snapshot();
            Assert.IsFalse(_inventory.ReduceStackCount(_client, InventoryType.Personal, item, quantity));
            Assert.AreEqual(10u, item.StackSize);
            Assert.AreEqual(0, Drain().Count);
            Assert.AreEqual(0, _factory.Calls);
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void StaleDatabaseAndDeleteExceptionLeaveMemoryAndClientUntouched(bool home)
        {
            var item = home ? _home : _personal;
            using (var context = Context())
                context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_consumption BEFORE DELETE ON character_inventory BEGIN SELECT RAISE(ABORT, 'fixture failure'); END");
            var before = Snapshot();
            Destroy(home, item.EntityId, 10);
            Assert.AreEqual(10u, item.StackSize);
            Assert.AreSame(item, EntityManager.Instance.GetItem(item.EntityId));
            Assert.AreEqual(item.EntityId, (home ? _client.Player.Inventory.HomeInventory : _client.Player.Inventory.PersonalInventory)[50]);
            Assert.AreEqual(0, Drain().Count);
            CollectionAssert.AreEqual(before, Snapshot());
            using (var context = Context())
            {
                context.Database.ExecuteSqlRaw("DROP TRIGGER fail_consumption");
                context.Database.ExecuteSqlInterpolated($"UPDATE items SET stack_size=9 WHERE item_id={item.Id}");
            }
            before = Snapshot();
            Destroy(home, item.EntityId, 3);
            Assert.AreEqual(10u, item.StackSize);
            Assert.AreEqual(0, Drain().Count);
            CollectionAssert.AreEqual(before, Snapshot());
        }

        [TestMethod]
        public void OppositeInventoryAndUnknownEntitiesCannotBeDestroyed()
        {
            var before = Snapshot();
            Destroy(true, _personal.EntityId, 10);
            Destroy(false, _home.EntityId, 10);
            Destroy(true, 0, 10);
            Destroy(false, ulong.MaxValue, 10);
            Assert.AreEqual(0, _factory.Calls);
            Assert.AreEqual(0, Drain().Count);
            CollectionAssert.AreEqual(before, Snapshot());
        }

        private SqliteCharContext Context() => WeaponReloadPersistenceTests.Context(_connection);
        private static Item Register(uint id, uint owner)
        {
            var item = new Item { Id = id, OwnerId = owner, OwnerSlotId = 50, StackSize = 10 };
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            return item;
        }
        private void Destroy(bool home, ulong id, uint quantity)
        {
            if (home) _inventory.HomeInventory_DestroyItem(_client, new HomeInventory_DestroyItemPacket { EntityId = id, Quantity = quantity });
            else _inventory.PersonalInventory_DestroyItem(_client, new PersonalInventory_DestroyItemPacket { EntityId = id, Quantity = quantity });
        }
        private string[] Snapshot(uint excluding = 0)
        {
            using var context = Context();
            return context.ItemEntries.AsNoTracking().OrderBy(i => i.ItemId).ToList().Where(i => i.ItemId != excluding)
                .Select(i => $"item:{i.ItemId}:{i.ItemTemplateId}:{i.StackSize}:{i.AmmoCount}:{i.CurrentHitPoints}:{i.Color}:{i.CrafterName}")
                .Concat(context.CharacterInventoryEntries.AsNoTracking().OrderBy(i => i.ItemId).ToList().Where(i => i.ItemId != excluding)
                    .Select(i => $"link:{i.ItemId}:{i.AccountId}:{i.CharacterId}:{i.InventoryType}:{i.SlotId}")).ToArray();
        }
        private List<ServerPythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call) packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
    }

    [TestClass]
    public class ItemDestructionPacketTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void NonNegativeIntegerAndLongQuantitiesPreserveTheirExactValue(bool home)
        {
            Assert.AreEqual(0u, Read(home, writer => writer.WriteInt(0)));
            Assert.AreEqual(0u, Read(home, writer => writer.WriteLong(0)));
            Assert.AreEqual(3u, Read(home, writer => writer.WriteInt(3)));
            Assert.AreEqual(3u, Read(home, writer => writer.WriteLong(3)));
            Assert.AreEqual(uint.MaxValue, Read(home, writer => writer.WriteLong(uint.MaxValue)));
        }

        [DataTestMethod]
        [DataRow(false, -1L)]
        [DataRow(true, -1L)]
        [DataRow(false, 4294967297L)]
        [DataRow(true, 4294967297L)]
        public void InvalidQuantityCannotWrapIntoASmallerPositiveDestruction(bool home, long quantity)
            => Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteLong(quantity)));

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void InvalidArityNegativeIntegerAndNonIntegerQuantitiesAreRejected(bool home)
        {
            Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteLong(1), 1));
            Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteLong(1), 3));
            Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteInt(-1)));
            Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteNoneStruct()));
            Assert.ThrowsException<InvalidClientMessageException>(() => Read(home, writer => writer.WriteDouble(1.5)));
        }

        private static uint Read(bool home, Action<PythonWriter> writeQuantity, int arity = 2)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            writer.WriteTuple(arity);
            writer.WriteULong(0x100000005UL);
            writeQuantity(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            if (home)
            {
                var packet = new HomeInventory_DestroyItemPacket();
                packet.Read(reader);
                Assert.AreEqual(0x100000005UL, packet.EntityId);
                Assert.AreEqual(stream.Length, stream.Position);
                return packet.Quantity;
            }
            else
            {
                var packet = new PersonalInventory_DestroyItemPacket();
                packet.Read(reader);
                Assert.AreEqual(0x100000005UL, packet.EntityId);
                Assert.AreEqual(stream.Length, stream.Position);
                return packet.Quantity;
            }
        }
    }
}
