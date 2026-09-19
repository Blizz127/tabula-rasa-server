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
                    case "get_CharacterAbilityDrawers": return new Rasa.Repositories.Char.CharacterAbilityDrawer.CharacterAbilityDrawerRepository(Context);
                    case "get_Items": return new ItemRepository(Context);
                    case "get_CharacterInventories": return new CharacterInventoryRepository(Context);
                    case "get_CharacterLockboxes": return new CharacterLockboxRepository(Context);
                    case "get_CharacterLogoses": return new Rasa.Repositories.Char.CharacterLogos.CharacterLogosRepository(Context);
                    case "get_CharacterTeleporters": return new Rasa.Repositories.Char.CharacterTeleporter.CharacterTeleporterRepository(Context);
                    case "get_CharacterMissions": return new Rasa.Repositories.Char.CharacterMission.CharacterMissionRepository(Context);
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

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CreationPersistsRecruitRanksAndUsableGearBeforePublishingSuccess(bool firstFamily)
        {
            _manager.RequestCreateCharacterInSlot(_client, firstFamily ? FirstRequest() : Request());
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<CharacterCreateSuccessPacket>().Count());
            Assert.IsFalse(packets.OfType<UserCreationFailedPacket>().Any());
            using var context = Context();
            var character = context.CharacterEntries.Single();
            Assert.AreEqual("Fixture", context.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual(3, context.CharacterAppearanceEntries.Count());
            Assert.IsTrue(context.CharacterAppearanceEntries.All(a => a.Color == 0xffffffffu));
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
            // And both are on the tray from the start, Lightning first - footage A2-011 shows it in slot 1
            // on the first HUD frame, and without it "Use your Lightning power" names a power the player
            // cannot find.
            var tray = new Rasa.Repositories.Char.CharacterAbilityDrawer.CharacterAbilityDrawerRepository(context).GetCharacterAbilities(character.Id);
            CollectionAssert.AreEquivalent(new[] { (0, 194), (1, 401) }, tray.Select(t => (t.AbilitySlot, t.AbilityId)).ToArray());
            Assert.AreEqual(0, context.CharacterLogosEntries.Count());
            // This asserted an empty tray, which was the gap docs/new-character-client-evidence.md left open
            // ("does not ... assign a drawer slot"). The footage fills it: two slots, nothing else.
            Assert.AreEqual(2, context.CharacterAbilityDrawerEntries.Count());
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

        [TestMethod]
        public void FirstFamilyRequestCannotBeReplayedUsingStaleAccountState()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            _client.AccountEntry.FamilyName = "";
            _client.AccountEntry.Characters.Clear();
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<CharacterCreateSuccessPacket>().Count());
            Assert.AreEqual(CreateCharacterResult.InvalidCharacterName, packets.OfType<UserCreationFailedPacket>().Single().Result);
            using var context = Context();
            Assert.AreEqual(1, context.CharacterEntries.Count());
            Assert.AreEqual("Fixture", context.GameAccountEntries.Single().FamilyName);
            Assert.AreEqual(5, context.CharacterSkillsEntries.Count());
            Assert.AreEqual(5, context.ItemEntries.Count());
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(17)]
        public void InvalidOrOccupiedSlotCannotCreateAnotherCharacter(int slot)
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            _manager.RequestCreateCharacterInSlot(_client, Request((byte)slot, "Second"));
            var packets = Drain();
            Assert.AreEqual(1, packets.OfType<CharacterCreateSuccessPacket>().Count());
            Assert.AreEqual(CreateCharacterResult.CharacterSlotInUse, packets.OfType<UserCreationFailedPacket>().Single().Result);
            using var context = Context();
            Assert.AreEqual(1, context.CharacterEntries.Count());
            Assert.AreEqual(5, context.CharacterSkillsEntries.Count());
            Assert.AreEqual(5, context.ItemEntries.Count());
        }

        [TestMethod]
        public void CreationOutsideCharacterSelectionDoesNotWriteState()
        {
            _client.State = ClientState.LoggedIn;
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            Assert.AreEqual(0, Drain().Count);
            using var context = Context();
            Assert.AreEqual(0, context.CharacterEntries.Count());
            Assert.AreEqual("", context.GameAccountEntries.Single().FamilyName);
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

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(2)]
        [DataRow(17)]
        public void SelectingAnUnavailableSlotPreservesSavedAndSessionState(int slot)
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            Drain();
            Dictionary<uint, DateTime?> lastLogins;
            using (var context = Context())
            {
                context.GameAccountEntries.Add(new GameAccountEntry
                    { Id = 20, Name = "Other", FamilyName = "Other", Email = "other@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry { AccountId = 20, Slot = 2, Name = "Other" });
                context.SaveChanges();
                lastLogins = context.CharacterEntries.ToDictionary(c => c.Id, c => c.LastLogin);
            }
            var selected = _client.AccountEntry.SelectedSlot;
            var player = _client.Player;
            _manager.RequestSwitchToCharacterInSlot(_client, new RequestSwitchToCharacterInSlotPacket
                { SlotNum = (byte)slot, SkipBootcamp = true });
            using var reloaded = Context();
            Assert.AreEqual(selected, _client.AccountEntry.SelectedSlot);
            Assert.AreEqual(selected, reloaded.GameAccountEntries.Single(a => a.Id == 10).SelectedSlot);
            foreach (var character in reloaded.CharacterEntries)
                Assert.AreEqual(lastLogins[character.Id], character.LastLogin);
            Assert.AreSame(player, _client.Player);
            Assert.AreEqual(ClientState.CharacterSelection, _client.State);
            Assert.AreEqual(0, Drain().Count);
        }

        [TestMethod]
        public void FailedSelectionSaveDoesNotPublishTheSelectedSlotOrLogin()
        {
            _manager.RequestCreateCharacterInSlot(_client, Request());
            Drain();
            DateTime? lastLogin;
            using (var context = Context())
            {
                lastLogin = context.CharacterEntries.Single().LastLogin;
                context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_login BEFORE UPDATE ON character BEGIN SELECT RAISE(ABORT, 'login test failure'); END;");
            }
            var selected = _client.AccountEntry.SelectedSlot;
            var player = _client.Player;
            Assert.ThrowsException<DbUpdateException>(() => _manager.RequestSwitchToCharacterInSlot(_client,
                new RequestSwitchToCharacterInSlotPacket { SlotNum = 1 }));
            using var reloaded = Context();
            Assert.AreEqual(selected, _client.AccountEntry.SelectedSlot);
            Assert.AreEqual(selected, reloaded.GameAccountEntries.Single().SelectedSlot);
            Assert.AreEqual(lastLogin, reloaded.CharacterEntries.Single().LastLogin);
            Assert.AreSame(player, _client.Player);
            Assert.AreEqual(ClientState.CharacterSelection, _client.State);
            Assert.AreEqual(0, Drain().Count);
        }

        private SqliteCharContext Context() => WeaponReloadPersistenceTests.Context(_connection);
        private static RequestCreateCharacterInSlotPacket Request(byte slot = 1, string name = "First")
            => new() { SlotNum = slot, CharacterName = name, FamilyName = "Fixture", Scale = 1, Gender = 0, RaceId = Race.Human };
        [TestMethod]
        public void CloningSpendsACreditAndKeepsProgressionButResetsPointsGearAndMoney()
        {
            _manager.RequestCreateCharacterInSlot(_client, FirstRequest());
            uint sourceId;
            using (var context = Context())
            {
                var source = context.CharacterEntries.Single();
                sourceId = source.Id;
                source.Level = 4; source.Experience = 44_000; source.Class = 1; source.CloneCredits = 1; source.Credit = 500;
                source.MapContextId = 1220; source.CoordX = 765; source.CoordY = 294; source.CoordZ = 386; source.NumLogins = 3; source.Body = 6;
                context.CharacterSkillsEntries.Single(s => s.SkillId == 1).SkillLevel = 3;
                context.CharacterLogosEntries.Add(new CharacterLogosEntry { CharacterId = sourceId, LogosId = 23 });
                context.CharacterTeleporterEntries.Add(new CharacterTeleporterEntry(sourceId, 103, 5));
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(sourceId, 1995, (uint)MissionState.Completed, 7));
                context.CharacterMissionObjectiveEntries.Add(new CharacterMissionObjectiveEntry(sourceId, 1995, 4, (uint)MissionObjectiveState.Completed));
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(sourceId, 1526, (uint)MissionState.Active, 8));
                context.SaveChanges();
            }
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountRepository(Context()).Get(10));
            Drain();

            RequestCloneCharacterToSlotPacket Clone(byte source, byte slot, string name) =>
                new() { CloneSlotNum = source, SlotNum = slot, CharacterName = name, Scale = 1, Gender = 1, RaceId = Race.Human };

            // No source, an occupied slot: refused without writing.
            _manager.RequestCloneCharacterToSlot(_client, Clone(9, 2, "Second"));
            _manager.RequestCloneCharacterToSlot(_client, Clone(1, 1, "Second"));
            Assert.AreEqual(0, Drain().OfType<CharacterCreateSuccessPacket>().Count());

            _manager.RequestCloneCharacterToSlot(_client, Clone(1, 2, "Second"));
            Assert.AreEqual(1, Drain().OfType<CharacterCreateSuccessPacket>().Count());

            using (var context = Context())
            {
                var source = context.CharacterEntries.Single(c => c.Id == sourceId);
                var clone = context.CharacterEntries.Single(c => c.Id != sourceId);
                Assert.AreEqual(0u, source.CloneCredits);
                Assert.AreEqual((2, "Second", (byte)1), (clone.Slot, clone.Name, clone.Gender));
                Assert.AreEqual((1u, (byte)4, 44_000u, 1220u), (clone.Class, clone.Level, clone.Experience, clone.MapContextId));
                Assert.AreEqual((765.0, 294.0, 386.0), (clone.CoordX, clone.CoordY, clone.CoordZ));
                Assert.AreEqual((0, 0, 0u), (clone.Credit, clone.Body, clone.CloneCredits));
                Assert.IsTrue(clone.NumLogins > 0, "a clone never gets the first-login boot-camp choice");
                Assert.AreEqual(1, (int)context.CharacterSkillsEntries.Single(s => s.CharacterId == clone.Id && s.SkillId == 1).SkillLevel);
                CollectionAssert.AreEqual(new uint[] { 23 }, context.CharacterLogosEntries.Where(l => l.CharacterId == clone.Id).Select(l => l.LogosId).ToArray());
                Assert.AreEqual(103u, context.CharacterTeleporterEntries.Single(t => t.CharacterId == clone.Id).WaypointId);
                CollectionAssert.AreEqual(new uint[] { 1995 }, context.CharacterMissionEntries.Where(m => m.CharacterId == clone.Id).Select(m => m.MissionId).ToArray());
                Assert.AreEqual(1, context.CharacterMissionObjectiveEntries.Count(o => o.CharacterId == clone.Id));
                Assert.AreEqual(5, context.CharacterInventoryEntries.Count(i => i.CharacterId == clone.Id));
            }

            // The credit is spent.
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountRepository(Context()).Get(10));
            _manager.RequestCloneCharacterToSlot(_client, Clone(1, 3, "Third"));
            Assert.AreEqual(0, Drain().OfType<CharacterCreateSuccessPacket>().Count());
            using (var context = Context())
                Assert.AreEqual(2, context.CharacterEntries.Count());
        }

        private static CreateCharacterPacket FirstRequest()
            => new() { CharacterName = "First", FamilyName = "Fixture", Scale = 1, Gender = 0, RaceId = Race.Human };
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
