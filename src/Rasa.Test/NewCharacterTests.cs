using System;
using System.Collections.Generic;
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
using Rasa.Packets;
using Rasa.Packets.Game.Client;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterAppearance;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.Char.CharacterLockbox;
using Rasa.Repositories.Char.CharacterSkills;
using Rasa.Repositories.Char.GameAccount;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class NewCharacterTests
    {
        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Characters": return new CharacterRepository(Context);
                    case "get_GameAccounts": return new GameAccountRepository(Context);
                    case "get_CharacterAppearances": return new CharacterAppearanceRepository(Context);
                    case "get_CharacterSkills": return new CharacterSkillsRepository(Context);
                    case "get_Items": return new ItemRepository(Context);
                    case "get_CharacterInventories": return new CharacterInventoryRepository(Context);
                    case "get_CharacterLockboxes": return new CharacterLockboxRepository(Context);
                    case "BeginTransaction": return Context.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                    case "Complete": Context.SaveChanges(); return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        public class WorldProxy : DispatchProxy
        {
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name == "Dispose") return null;
                throw new NotSupportedException(method.Name);
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
            public IWorldUnitOfWork CreateWorld() => DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
        }

        private static readonly (uint Template, EntityClasses Class, int HitPoints, int Skill)[] Items =
        {
            (145, (EntityClasses)6048, 100, 1),
            (28, (EntityClasses)3147, 1, 0),
            (13126, (EntityClasses)15602, 47, 19),
            (13186, (EntityClasses)15662, 70, 19),
            (13156, (EntityClasses)15632, 59, 19),
            // The prior creation code incorrectly read this other item's HP.
            (17131, (EntityClasses)9100301, 120, 1)
        };

        private SqliteConnection _connection;
        private Factory _factory;
        private CharacterManager _manager;
        private Client _client;
        private Logger.LoggerConfig _oldLogger;
        private readonly Dictionary<EntityClasses, EntityClass> _oldClasses = new();
        private readonly Dictionary<uint, EntityClasses?> _oldTemplates = new();

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _factory = new Factory(_connection);
            _manager = new CharacterManager(_factory);
            using (var context = Context())
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry
                    { Id = 10, Name = "Fixture", FamilyName = "", Email = "fixture@example.invalid" });
                context.SaveChanges();
                _client = new Client(_factory, new ClientPacketHandler())
                    { State = ClientState.CharacterSelection };
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountRepository(context).Get(10));
            }
            foreach (var entry in Items)
            {
                _oldClasses[entry.Class] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(entry.Class, out var old) ? old : null;
                _oldTemplates[entry.Template] = ItemManager.Instance.ItemTemplateItemClass.TryGetValue(entry.Template, out var previous) ? previous : (EntityClasses?)null;
                var template = new ItemTemplate(new ItemTemplateItemClassEntry
                    { ItemTemplateId = entry.Template, ItemClass = (uint)entry.Class });
                if (entry.Skill != 0)
                {
                    template.EquipableInfo = new EquipableInfo(entry.Skill, 1);
                    template.ItemInfo.Requirements[RequirementsType.ReqXpLevel] = 1;
                }
                var itemClass = new EntityClass((uint)entry.Class, "creation fixture", 0, 0, new List<AugmentationType>(), false)
                    { ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = entry.HitPoints, StackSize = 100 }) };
                itemClass.ItemTemplates.Add(entry.Template, template);
                EntityClassManager.Instance.LoadedEntityClasses[entry.Class] = itemClass;
                ItemManager.Instance.ItemTemplateItemClass[entry.Template] = entry.Class;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var pair in _oldClasses)
                if (pair.Value == null) EntityClassManager.Instance.LoadedEntityClasses.Remove(pair.Key);
                else EntityClassManager.Instance.LoadedEntityClasses[pair.Key] = pair.Value;
            foreach (var pair in _oldTemplates)
                if (pair.Value.HasValue) ItemManager.Instance.ItemTemplateItemClass[pair.Key] = pair.Value.Value;
                else ItemManager.Instance.ItemTemplateItemClass.Remove(pair.Key);
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            _connection.Dispose();
        }

        [TestMethod]
        public void CreationPersistsRecruitRanksAndUsableGearBeforePublishingSuccess()
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<CharacterCreateSuccessPacket>().Count());
            Assert.IsFalse(packets.OfType<UserCreationFailedPacket>().Any());
            using var context = Context();
            var character = context.CharacterEntries.Single();
            Assert.AreEqual("Fixture", context.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual(3, context.CharacterAppearanceEntries.Count());
            var saved = new CharacterSkillsRepository(context).GetCharacterSkills(character.Id);
            CollectionAssert.AreEquivalent(new uint[] { 1, 8, 19, 49, 165 }, saved.Select(s => s.SkillId).ToArray());
            Assert.IsTrue(saved.All(s => s.SkillLevel == 1));
            var actor = new Manifestation
            {
                Level = character.Level, Class = character.Class, Race = (Race)character.Race,
                Skills = saved.ToDictionary(s => (SkillId)s.SkillId, s => new SkillsData((SkillId)s.SkillId, s.AbilityId, s.SkillLevel))
            };
            Assert.AreEqual(0, SkillTraining.GetAvailablePoints(actor));
            CollectionAssert.AreEquivalent(new[] { 194, 401 }, new AbilitiesPacket(actor.Skills).AbilityList.Values.Select(s => s.AbilityId).ToArray());
            Assert.AreEqual(0, context.CharacterLogosEntries.Count());
            Assert.AreEqual(0, context.CharacterAbilityDrawerEntries.Count());
            Assert.AreEqual(5, context.ItemEntries.Count());
            Assert.AreEqual(5, context.CharacterInventoryEntries.Count());
            foreach (var item in context.ItemEntries.ToArray())
            {
                var entry = Items.Single(i => i.Template == item.ItemTemplateId);
                Assert.AreEqual(entry.HitPoints, item.CurrentHitPoints);
                var itemClass = EntityClassManager.Instance.LoadedEntityClasses[entry.Class];
                var instance = new Item { CurrentHitPoints = item.CurrentHitPoints, ItemTemplate = itemClass.ItemTemplates[entry.Template] };
                Assert.AreEqual(EquipmentRequirementFailure.None, EquipmentRequirements.Check(actor, instance, itemClass.ItemClassInfo));
                var position = context.CharacterInventoryEntries.Single(i => i.ItemId == item.ItemId);
                Assert.AreEqual(character.Id, position.CharacterId);
                Assert.AreEqual(10u, position.AccountId);
            }
            Assert.AreEqual(100u, context.ItemEntries.Single(i => i.ItemTemplateId == 28).StackSize);
            Assert.AreEqual(1, context.CharacterLockboxEntries.Single().PurashedTabs);
        }

        [TestMethod]
        public void SecondCreationPreservesExistingTrainingAndSharedBank()
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            uint firstId;
            using (var context = Context())
            {
                firstId = context.CharacterEntries.Single().Id;
                context.CharacterEntries.Single().Level = 20;
                context.CharacterSkillsEntries.Single(s => s.SkillId == 1).SkillLevel = 4;
                var bank = context.CharacterLockboxEntries.Single();
                bank.Credits = 1234;
                bank.PurashedTabs = 3;
                context.SaveChanges();
            }
            _manager.RequestCreateCharacterInSlot(_client, Request(2, "Second"));
            using var reloaded = Context();
            var second = reloaded.CharacterEntries.Single(c => c.Slot == 2);
            Assert.AreEqual(4, reloaded.CharacterSkillsEntries.Single(s => s.CharacterId == firstId && s.SkillId == 1).SkillLevel);
            Assert.AreEqual(5, reloaded.CharacterSkillsEntries.Count(s => s.CharacterId == second.Id && s.SkillLevel == 1));
            Assert.AreEqual(1234, reloaded.CharacterLockboxEntries.Single().Credits);
            Assert.AreEqual(3, reloaded.CharacterLockboxEntries.Single().PurashedTabs);
            Assert.AreEqual(2, Drain().OfType<CharacterCreateSuccessPacket>().Count());
        }

        [DataTestMethod]
        [DataRow("character_skills", "NEW.skill_id = 49")]
        [DataRow("items", "NEW.item_template_id = 13156")]
        [DataRow("character_inventory", "NEW.slot_id = 3")]
        [DataRow("character_lockbox", "1 = 1")]
        [DataRow("character_appearance", "1 = 1")]
        public void FailedCreationRollsBackCharacterRanksItemsAndFamily(string table, string condition)
        {
            using (var context = Context())
                context.Database.ExecuteSqlRaw($"CREATE TRIGGER reject_creation BEFORE INSERT ON {table} WHEN {condition} BEGIN SELECT RAISE(ABORT, 'creation test failure'); END;");
            _manager.RequestCreateCharacterInSlot(_client, Request());
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<UserCreationFailedPacket>().Count());
            Assert.IsFalse(packets.OfType<CharacterCreateSuccessPacket>().Any());
            using var reloaded = Context();
            Assert.AreEqual("", reloaded.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual(0, reloaded.CharacterEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterAppearanceEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterSkillsEntries.Count());
            Assert.AreEqual(0, reloaded.ItemEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterInventoryEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterLockboxEntries.Count());
        }

        private SqliteCharContext Context() => WeaponReloadPersistenceTests.Context(_connection);
        private static RequestCreateCharacterInSlotPacket Request(byte slot = 1, string name = "First")
            => new() { SlotNum = slot, CharacterName = name, FamilyName = "Fixture", Scale = 1, Gender = 0, RaceId = Race.Human };
        private List<ServerPythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call) packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
    }
}
