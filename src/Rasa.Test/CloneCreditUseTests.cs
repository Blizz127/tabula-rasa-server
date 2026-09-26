using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Migrations.WildernessData;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.Manifestation.Client;
using Rasa.Packets.Manifestation.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// RequestUseCloneCredit (opcode 706), the right-click "Use" of a Clone Credit token (client clonecredit.pyo
    /// InventoryUse; CT-CLONE-TOKEN in docs/evidence/class-trainer-evidence.json): one token from the backpack is spent
    /// and the character gains one clone credit, saved and sent as CloneCredits (the client shows PM 955 on a rise).
    /// Anything else - another item, a token in the footlocker or another character's backpack - changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CloneCreditUseTests
    {
        private const uint CharacterId = 101;
        private const uint TokenTemplate = 111219;
        private const EntityClasses TokenClass = (EntityClasses)26491;
        private const EntityClasses OtherClass = (EntityClasses)26490;

        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Items": return new ItemRepository(Context);
                    case "get_Characters": return new CharacterRepository(Context);
                    case "Complete": Context.SaveChanges(); return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)unit).Context = WeaponReloadPersistenceTests.Context(_connection);
                return unit;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private SqliteConnection _connection;
        private Factory _factory;
        private CharacterManager _realCharacters;
        private InventoryManager _realInventory;
        private Client _client;
        private readonly List<Item> _items = new List<Item>();
        private Logger.LoggerConfig _oldLogger;

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _factory = new Factory(_connection);

            using (var context = WeaponReloadPersistenceTests.Context(_connection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.AddRange(
                    new CharacterEntry { Id = CharacterId, AccountId = 10, Slot = 1, Name = "Owner", Level = 16, Class = 4, CloneCredits = 1, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow },
                    new CharacterEntry { Id = 102, AccountId = 10, Slot = 2, Name = "Sibling", Level = 5, Class = 2, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow });
                // 1: three tokens in the backpack; 2: a token in the footlocker; 3: another item in the backpack;
                // 4: a token in the sibling's backpack; 5: a single token in the backpack.
                context.ItemEntries.AddRange(
                    new ItemEntry { ItemId = 1, ItemTemplateId = TokenTemplate, StackSize = 3, CrafterName = "" },
                    new ItemEntry { ItemId = 2, ItemTemplateId = TokenTemplate, StackSize = 1, CrafterName = "" },
                    new ItemEntry { ItemId = 3, ItemTemplateId = 99999, StackSize = 1, CrafterName = "" },
                    new ItemEntry { ItemId = 4, ItemTemplateId = TokenTemplate, StackSize = 1, CrafterName = "" },
                    new ItemEntry { ItemId = 5, ItemTemplateId = TokenTemplate, StackSize = 1, CrafterName = "" });
                context.CharacterInventoryEntries.AddRange(
                    new CharacterInventoryEntry(10, CharacterId, (uint)InventoryType.Personal, 40, 1),
                    new CharacterInventoryEntry(10, 0, (uint)InventoryType.HomeInventory, 41, 2),
                    new CharacterInventoryEntry(10, CharacterId, (uint)InventoryType.Personal, 42, 3),
                    new CharacterInventoryEntry(10, 102, (uint)InventoryType.Personal, 43, 4),
                    new CharacterInventoryEntry(10, CharacterId, (uint)InventoryType.Personal, 44, 5));
                context.SaveChanges();
            }

            _realCharacters = Swap<CharacterManager>(Activator.CreateInstance(typeof(CharacterManager),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { _factory }, null));
            _realInventory = Swap<InventoryManager>(Activator.CreateInstance(typeof(InventoryManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { _factory }, null));

            EntityClassManager.Instance.LoadedEntityClasses[TokenClass] = new EntityClass((uint)TokenClass, "ItemCloneCredit", 776, 1,
                new List<AugmentationType> { AugmentationType.Item, AugmentationType.CloneCredit }, false);
            EntityClassManager.Instance.LoadedEntityClasses[OtherClass] = new EntityClass((uint)OtherClass, "fixture item", 0, 0,
                new List<AugmentationType> { AugmentationType.Item }, false);

            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = 10, SelectedSlot = 1 });
            _client.Player = new Manifestation { Id = CharacterId, Level = 16, Class = 4, CloneCredits = 1 };
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _client.Player.Inventory.HomeInventory.AddRange(new ulong[480]);
            Place(1, CharacterId, 40, TokenClass, 3, _client.Player.Inventory.PersonalInventory);
            Place(2, 0, 41, TokenClass, 1, _client.Player.Inventory.HomeInventory);
            Place(3, CharacterId, 42, OtherClass, 1, _client.Player.Inventory.PersonalInventory);
            Place(4, 102, 43, TokenClass, 1, null);
            Place(5, CharacterId, 44, TokenClass, 1, _client.Player.Inventory.PersonalInventory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Swap<CharacterManager>(_realCharacters);
            Swap<InventoryManager>(_realInventory);
            EntityClassManager.Instance.LoadedEntityClasses.Remove(TokenClass);
            EntityClassManager.Instance.LoadedEntityClasses.Remove(OtherClass);
            foreach (var item in _items)
            {
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.UnregisterEntity(item.EntityId);
            }
            _connection.Dispose();
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
        }

        private static T Swap<T>(object replacement)
        {
            var field = typeof(T).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = (T)field.GetValue(null);
            field.SetValue(null, replacement);
            return previous;
        }

        private void Place(uint id, uint owner, uint slot, EntityClasses itemClass, uint stack, List<ulong> slots)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = itemClass == TokenClass ? TokenTemplate : 99999, ItemClass = (uint)itemClass });
            var item = new Item { Id = id, OwnerId = owner, OwnerSlotId = slot, StackSize = stack, ItemTemplate = template, ItemTemplateId = template.ItemTemplateId };
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            if (slots != null) slots[(int)slot] = item.EntityId;
            _items.Add(item);
        }

        private List<(ulong EntityId, PythonPacket Packet)> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<(ulong, PythonPacket)>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                if (packet.Message is CallMethodMessage message)
                    packets.Add((message.EntityId, message.Packet));
            return packets;
        }

        private uint SavedCredits()
        {
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            return context.CharacterEntries.Single(c => c.Id == CharacterId).CloneCredits;
        }

        private uint? SavedStack(uint itemId)
        {
            using var context = WeaponReloadPersistenceTests.Context(_connection);
            return context.CharacterInventoryEntries.Find(itemId) == null ? (uint?)null : context.ItemEntries.Find(itemId).StackSize;
        }

        [TestMethod]
        public void TheClientsUseRequestDecodesThroughItsRegisteredHandler()
        {
            var router = new PacketRouter<ClientPacketHandler, GameOpcode>();
            var packet = (RequestUseCloneCreditPacket)Activator.CreateInstance(router.GetPacketType(GameOpcode.RequestUseCloneCredit));
            Assert.AreEqual(706, (int)GameOpcode.RequestUseCloneCredit);

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                writer.WriteULong(0x1234_5678_9ABCUL);
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            Assert.AreEqual(0x1234_5678_9ABCUL, packet.EntityId);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(2)]
        [DataRow(-1)]
        public void AnyOtherArgumentShapeIsRejected(int shape)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                if (shape < 0) { writer.WriteTuple(1); writer.WriteInt(7); }
                else
                {
                    writer.WriteTuple(shape);
                    for (var i = 0; i < shape; i++) writer.WriteULong(7);
                }
            }
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.ThrowsException<InvalidClientMessageException>(() => new RequestUseCloneCreditPacket().Read(reader));
        }

        [TestMethod]
        public void UsingATokenSpendsOneAndGainsOneCloneCredit()
        {
            var token = _items[0];
            ManifestationManager.Instance.RequestUseCloneCredit(_client, token.EntityId);

            Assert.AreEqual(2u, _client.Player.CloneCredits);
            Assert.AreEqual(2u, SavedCredits());
            Assert.AreEqual(2u, token.StackSize);
            Assert.AreEqual(2u, SavedStack(1));
            var packets = Drain();
            Assert.AreEqual(2u, packets.Select(p => p.Packet).OfType<SetStackCountPacket>().Single().StackSize);
            var credits = packets.Single(p => p.Packet is CloneCreditsPacket);
            Assert.AreEqual(_client.Player.EntityId, credits.EntityId);
            Assert.AreEqual(2u, ((CloneCreditsPacket)credits.Packet).CloneCredits);
            Assert.AreEqual(GameOpcode.CloneCredits, credits.Packet.Opcode);
            Assert.AreEqual(2, packets.Count);
        }

        [TestMethod]
        public void TheLastTokenOfAStackLeavesTheBackpack()
        {
            var token = _items[4];
            ManifestationManager.Instance.RequestUseCloneCredit(_client, token.EntityId);

            Assert.AreEqual(2u, SavedCredits());
            Assert.IsNull(SavedStack(5));
            Assert.AreEqual(0ul, _client.Player.Inventory.PersonalInventory[44]);
            Assert.IsNull(EntityManager.Instance.GetItem(token.EntityId));
            var packets = Drain().Select(p => p.Packet).ToList();
            Assert.AreEqual(token.EntityId, packets.OfType<InventoryRemoveItemPacket>().Single().EntityId);
            Assert.AreEqual(2u, packets.OfType<CloneCreditsPacket>().Single().CloneCredits);

            // Used again, the gone token gives nothing.
            ManifestationManager.Instance.RequestUseCloneCredit(_client, token.EntityId);
            Assert.AreEqual((2u, 2u), (_client.Player.CloneCredits, SavedCredits()));
            Assert.AreEqual(0, Drain().Count);
        }

        [DataTestMethod]
        [DataRow(1, "a token in the footlocker (OD-115)")]
        [DataRow(2, "an item that is no clone credit")]
        [DataRow(3, "a token in another character's backpack")]
        [DataRow(-1, "an unknown entity")]
        public void NothingElseGivesACredit(int index, string what)
        {
            var entityId = index < 0 ? 0xDEAD_BEEFUL : _items[index].EntityId;
            ManifestationManager.Instance.RequestUseCloneCredit(_client, entityId);

            Assert.AreEqual(1u, _client.Player.CloneCredits, what);
            Assert.AreEqual(1u, SavedCredits(), what);
            Assert.AreEqual(0, Drain().Count, what);
            Assert.AreEqual(3u, SavedStack(1));
            Assert.AreEqual(1u, SavedStack(2));
            Assert.AreEqual(1u, SavedStack(3));
            Assert.AreEqual(1u, SavedStack(4));
        }

        /// <summary>
        /// The evidence file names the token the migration flags and the method ids the handler answers, and every clone
        /// credit source that is not live is a gap in the manifest rather than a guessed reward row.
        /// </summary>
        [TestMethod]
        public void TheCloneCreditEvidenceMatchesTheImplementationAndItsGapsAreInTheManifest()
        {
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("class-trainer-evidence.json")));
            var sources = evidence.RootElement.GetProperty("clone_credit_sources");
            var item = sources.GetProperty("item");
            Assert.AreEqual(CloneCreditNotTradableRows.CloneCreditTemplate, item.GetProperty("item_template").GetUInt32());
            Assert.AreEqual(TokenTemplate, CloneCreditNotTradableRows.CloneCreditTemplate);
            Assert.AreEqual((uint)TokenClass, item.GetProperty("entity_class").GetUInt32());
            Assert.IsTrue(item.GetProperty("not_tradable").GetBoolean());
            Assert.AreEqual(1, CloneCreditNotTradableRows.NotTradable);

            var token = evidence.RootElement.GetProperty("rules").EnumerateArray().Single(rule => rule.GetProperty("id").GetString() == "CT-CLONE-TOKEN");
            StringAssert.Contains(token.GetProperty("rule").GetString(), $"(method {(int)GameOpcode.RequestUseCloneCredit})");
            StringAssert.Contains(token.GetProperty("rule").GetString(), $"CloneCredits(count) ({(int)GameOpcode.CloneCredits})");
            Assert.AreEqual(70, (int)AugmentationType.CloneCredit); // entityclass 26491 augmentations "6,70"

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var gaps = manifest.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            var notLive = sources.GetProperty("sources").EnumerateArray().Where(source => source.GetProperty("status").GetString() != "implemented" && source.TryGetProperty("gap", out _)).ToList();
            Assert.IsTrue(notLive.Count >= 7);
            foreach (var source in notLive)
                Assert.IsTrue(gaps.Contains(source.GetProperty("gap").GetString()), source.GetProperty("source").GetString());
            foreach (var gap in new[] { "GAP-CLONE-TOKEN-LOCKBOX-USE", "GAP-CLONE-TOKEN-FLAGS", "GAP-CLONE-TOO-AUTOCOMPLETE", "GAP-CLONE-SOCIAL-STATE" })
                Assert.IsTrue(gaps.Contains(gap), gap);

            var change = manifest.RootElement.GetProperty("changes").EnumerateArray()
                .Single(entry => entry.GetProperty("migration").GetString() == CloneCreditNotTradableRows.Migration);
            Assert.AreEqual(("itemtemplate", "not_tradable_flag"), (change.GetProperty("table").GetString(), change.GetProperty("field").GetString()));
            Assert.AreEqual(CloneCreditNotTradableRows.CloneCreditTemplate, change.GetProperty("key").GetProperty("id").GetUInt32());
            Assert.AreEqual((0, 1), (change.GetProperty("old").GetInt32(), change.GetProperty("new").GetInt32()));
            Assert.AreEqual("observed", change.GetProperty("tier").GetString());
        }

        [TestMethod]
        public void TheMigrationFlagsOnlyTheTokenAndRollsBackToThePlaceholder()
        {
            foreach (var rollback in new[] { false, true })
            {
                var builder = new Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder(SeedMigrationParity.SqliteProvider);
                if (rollback) CloneCreditNotTradableRows.DeleteData(builder);
                else CloneCreditNotTradableRows.InsertData(builder);
                var update = (Microsoft.EntityFrameworkCore.Migrations.Operations.UpdateDataOperation)builder.Operations.Single();
                Assert.AreEqual("itemtemplate", update.Table);
                CollectionAssert.AreEqual(new[] { "id" }, update.KeyColumns);
                Assert.AreEqual(TokenTemplate, update.KeyValues[0, 0]);
                CollectionAssert.AreEqual(new[] { "not_tradable_flag" }, update.Columns);
                Assert.AreEqual(rollback ? (byte)0 : (byte)1, update.Values[0, 0]);
            }
        }

        [TestMethod]
        public void OutsideTheWorldTheRequestIsIgnored()
        {
            _client.State = ClientState.CharacterSelection;
            ManifestationManager.Instance.RequestUseCloneCredit(_client, _items[0].EntityId);
            Assert.AreEqual((1u, 1u, 3u), (_client.Player.CloneCredits, SavedCredits(), SavedStack(1).Value));
            Assert.AreEqual(0, Drain().Count);
        }
    }
}
