using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.Mission.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterContentFact;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.Char.CharacterLogos;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Repositories.Char.GameAccount;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Repositories.World.MissionContent;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Plays the seeded S5/S6 boot-camp content end to end against a world database migrated through
    /// BootcampS5Reinforcements, BootcampS6ExitToAliaDas and BootcampFixRogersTurnIn: Youngblood's 1995, the wounded
    /// soldier, Conrad's corpse, the bomb and its detonation, Van Valkenberg and the exit pad (transfer to location 19852
    /// and the account skip flag), the turn-in at Rogers in Alia Das, the forced Training Day offer on entering Alia Das
    /// and its turn-in at Kincaid with a reward pistol (WildernessArrivalTrainingDay), and the failure path with the 2005
    /// retry and the preserved D13.4 quirk. The NPC placements cannot be spawned without the creature seed, so the conversation NPCs
    /// are registered by hand at their seeded placement positions with the seeded packages; usables are materialized
    /// from the migrated rows.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampReinforcementsScenarioTests
    {
        private const uint CharacterId = 101;
        private const uint AccountId = 10;
        private const uint Context = 1985;
        private const uint CorpsePlacement = 198676;
        private const uint BombPlacement = 198677;
        private const uint WreckPlacement = 198678;
        private const uint AliaDas = 1220;
        private const uint RogersPlacement = 198684;
        private const uint KincaidPlacement = 198685;
        private const uint TrainingDay = 1526;
        private const uint ClassGearSoldier = 2010;
        private const uint ClassGearSpecialist = 2011;
        private const uint Caufield = 132;
        private const uint CaufieldPackage = 133;

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        public class CharProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Characters": return new CharacterRepository(Context);
                    case "get_CharacterMissions": return new CharacterMissionRepository(Context);
                    case "get_CharacterContentFacts": return new CharacterContentFactRepository(Context);
                    case "get_CharacterLogoses": return new CharacterLogosRepository(Context);
                    case "get_GameAccounts": return new GameAccountRepository(Context);
                    case "get_Items": return new ItemRepository(Context);
                    case "get_CharacterInventories": return new CharacterInventoryRepository(Context);
                    case "BeginTransaction": return Context.Database.BeginTransaction();
                    case "Complete": Context.SaveChanges(); return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        public class WorldProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_NpcMissions": return new NpcMissionRepository(Context);
                    case "get_NpcMissionObjectives": return new NpcMissionObjectiveRepository(Context);
                    case "get_NpcMissionRewards": return new NpcMissionRewardRepository(Context);
                    case "get_MissionContent": return new MissionContentRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _char;
            private readonly SqliteConnection _world;
            public Factory(SqliteConnection charConnection, SqliteConnection worldConnection)
            {
                _char = charConnection;
                _world = worldConnection;
            }
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, CharProxy>();
                ((CharProxy)(object)unit).Context = CharContext(_char);
                return unit;
            }
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
                ((WorldProxy)(object)unit).Context = WorldContext(_world);
                return unit;
            }
        }

        /// <summary>The live server's lookups for the rows the migrated world seeds (the full world seed is not replayed).</summary>
        private sealed class References : IContentReferences
        {
            private readonly IReadOnlyDictionary<uint, Mission> _missions;
            public References(IReadOnlyDictionary<uint, Mission> missions) => _missions = missions;
            // 1148 is the Divide and 1244 the Palisades: W3 batch 5 placed four NPCs there from TaRapedia's own
            // zone for each of them.
            public bool MapContextExists(uint mapContextId) => mapContextId is 1985 or 1220 or 1148 or 1244 or 1497 or 1304 or 1454 or 1759 or 1764 or 1761;
            public bool MissionExists(uint missionId) => _missions.ContainsKey(missionId);
            public bool ObjectiveExists(uint missionId, uint objectiveId) => _missions.TryGetValue(missionId, out var m) && m.Objectives.ContainsKey(objectiveId);
            public uint MissionGiver(uint missionId) => _missions.TryGetValue(missionId, out var m) ? m.MissionGiver : 0;
            public bool MissionOfferable(uint missionId) => _missions.TryGetValue(missionId, out var m) && m.IsDispensable;
            public bool CreatureExists(uint creatureId) => true;
            public bool EntityClassExists(uint entityClassId) => true;
            public bool ItemTemplateExists(uint itemTemplateId) => true;
            public bool LogosExists(uint logosId) => true;
            public bool HasLegacyWorldObjects(uint mapContextId) => false;
        }

        private static SqliteCharContext CharContext(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static SqliteWorldContext WorldContext(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static SqliteConnection _worldConnection;

        [ClassInitialize]
        public static void MigrateWorld(TestContext _)
        {
            if (Logger.Config == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _worldConnection = new SqliteConnection("Data Source=:memory:");
            _worldConnection.Open();
            using var context = WorldContext(_worldConnection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, _worldConnection);
            context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4), (1220, 'adv_foreas_concordia_wilderness', 1556, 0), (1148, 'adv_foreas_concordia_divide', 1584, 10), (1244, 'adv_foreas_concordia_palisades', 1584, 10), (1497, 'adv_foreas_valverde_plateau', 1584, 10), (1304, 'adv_foreas_valverde_pools', 1584, 10), (1454, 'adv_foreas_valverde_marshes', 1584, 10), (1759, 'adv_arieki_torden_mires', 1584, 10), (1764, 'adv_arieki_torden_plains', 1584, 10), (1761, 'adv_arieki_torden_incline', 1584, 10)");
            context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");
            context.Database.Migrate();
            RewardItemFixtures.SeedOriginalTemplateRows(_worldConnection);
        }

        [ClassCleanup]
        public static void DisposeWorld() => _worldConnection?.Dispose();

        private SqliteConnection _charConnection;
        private MissionManager _missions;
        private MissionContentManager _content;
        private Client _client;
        private MapChannel _instance;
        private readonly List<Creature> _npcs = new();
        private Creature _youngblood;
        private Creature _woundedSoldier;
        private Creature _vanValkenberg;
        private long _tick;
        private long _nowMs;
        private readonly List<ContentLocationEntry> _transfers = new();
        private RewardItemFixtures _rewardItems;
        private MapChannel _wilderness;
        private CharacterManager _realCharacters;

        [TestInitialize]
        public void Initialize()
        {
            _charConnection = new SqliteConnection("Data Source=:memory:");
            _charConnection.Open();
            var factory = new Factory(_charConnection, _worldConnection);
            _nowMs = 1_700_000_000_000;
            _tick = 50_000;

            using (var context = CharContext(_charConnection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = AccountId, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = CharacterId, AccountId = AccountId, Slot = 1, Name = "Recruit", Level = 2, MapContextId = Context, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(CharacterId, 1994, (uint)MissionState.Completed));
                context.SaveChanges();
            }

            // The Training Day reward pistols must be loaded before the missions build their reward info.
            _rewardItems = new RewardItemFixtures(_worldConnection);

            _missions = new MissionManager(factory, () => (uint)(_nowMs / 1000)) { NowMs = () => _nowMs };
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, _missions);
            _missions.LoadMissions();

            // SelectNewCharacterClass saves the new class through CharacterManager.Instance.
            var characterField = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _realCharacters = (CharacterManager)characterField.GetValue(null);
            characterField.SetValue(null, Activator.CreateInstance(typeof(CharacterManager), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { factory }, null));

            _content = new MissionContentManager(factory) { Missions = _missions, TickNow = () => _tick };
            _content.Load(() => new BootcampConfig(), new References(_missions.LoadedMissions), _missions.LoadedMissions);
            Assert.AreEqual(0, _content.Content.Gaps.Count, string.Join(" | ", _content.Content.Gaps));
            _missions.Content = _content;
            _content.Transfer = (_, location) => _transfers.Add(location);

            _instance = new MapChannel { MapInfo = new MapInfo(Context, "adv_bootcamp", 783, 4), OwnerCharacterId = CharacterId, InstanceId = 7, ClientList = new List<Client>() };
            _client = new Client(factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = AccountId });
            _client.Player = new Manifestation { Id = CharacterId, Level = 2, MapContextId = Context, MapChannel = _instance, Position = Placement(198658) };
            _client.Player.Missions[1994] = new PlayerMission { MissionId = 1994, State = MissionState.Completed };
            _client.Player.Credits[CurencyType.Credits] = 0;
            _client.Player.Credits[CurencyType.Prestige] = 0;
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _instance.ClientList.Add(_client);

            _youngblood = Npc(198505, 198658, 2561);
            _woundedSoldier = Npc(198509, 198675, 2584);
            _vanValkenberg = Npc(198508, 198679, 2564);

            ContentMaterializer.Materialize(_instance, _content.Content);
            ContentMaterializer.RefreshPresence(_client, _content.Content, _tick);
            ContentMaterializer.RefreshFor(_client, _content.Content);
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
            typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _realCharacters);
            foreach (var obj in _instance.DynamicObjects.ToList())
            {
                EntityManager.Instance.UnregisterDynamicObject(obj.EntityId);
                EntityManager.Instance.UnregisterEntity(obj.EntityId);
            }
            foreach (var npc in _npcs)
            {
                EntityManager.Instance.UnregisterCreature(npc.EntityId);
                EntityManager.Instance.UnregisterEntity(npc.EntityId);
            }
            foreach (var entityId in _client.Player.Inventory.PersonalInventory.Where(id => id != 0))
            {
                EntityManager.Instance.UnregisterItem(entityId);
                EntityManager.Instance.UnregisterEntity(entityId);
            }
            _rewardItems.Dispose();
            _charConnection.Dispose();
        }

        private Vector3 Placement(uint placementId)
        {
            var placement = _content.Content.Catalog.Placements[placementId];
            return new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ);
        }

        private Creature Npc(uint creatureId, uint placementId, uint package, MapChannel channel = null)
        {
            var seeded = _content.Content.Catalog.Placements[placementId];
            Assert.AreEqual((creatureId, package), (seeded.CreatureId, seeded.NpcPackageId), $"placement {placementId}");
            channel ??= _instance;
            Assert.AreEqual(channel.MapInfo.MapContextId, seeded.MapContextId, $"placement {placementId}");
            var creature = new Creature
            {
                DbId = creatureId, MapContextId = seeded.MapContextId, MapChannel = channel, Position = Placement(placementId),
                Level = 10, State = CharacterState.Idle, Npc = new Npc { NpcPackageId = package }
            };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            _npcs.Add(creature);
            return creature;
        }

        /// <summary>Registers an NPC the world seed already spawns (no content placement), such as Quartermaster Caufield.</summary>
        private Creature NpcAt(uint creatureId, uint package, Vector3 position, MapChannel channel)
        {
            var creature = new Creature
            {
                DbId = creatureId, MapContextId = channel.MapInfo.MapContextId, MapChannel = channel, Position = position,
                Level = 10, State = CharacterState.Idle, Npc = new Npc { NpcPackageId = package }
            };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            _npcs.Add(creature);
            return creature;
        }

        /// <summary>
        /// Carries the recruit to the Alia Das arrival (the transfer itself is recorded, not performed, by the test hook) and
        /// stands Rogers up from his seeded shared-context placement (BootcampFixRogersTurnIn, GAP-ROGERS).
        /// </summary>
        private Creature ArriveAtAliaDas()
        {
            var live = ContentMaterializer.PlacementsToSpawn(_content.Content, AliaDas).OrderBy(placement => placement.Id).ToList();
            // 199002 (Field Dr. Dawson) and 199003 (Receptive Liaison Brice) joined the shared Wilderness in W3 batch 5:
            // TaRapedia puts both of them in that zone, so W3's Divide missions are handed out by NPCs standing here.
            CollectionAssert.AreEqual(new[]
                {
                    (RogersPlacement, (byte)ContentPlacementBehavior.Stationary, 0u),
                    (KincaidPlacement, (byte)ContentPlacementBehavior.Stationary, 0u),
                    (199002u, (byte)ContentPlacementBehavior.Stationary, 0u),
                    (199003u, (byte)ContentPlacementBehavior.Stationary, 0u),
                    // Ranger Milpas stands at the arrival and walks with whoever takes 1390 (the escort mechanic).
                    (199803u, (byte)ContentPlacementBehavior.Escort, 0u)
                },
                live.Select(placement => (placement.Id, placement.Behavior, placement.PresentConditionId)).ToArray());

            var wilderness = _wilderness = new MapChannel { MapInfo = new MapInfo(AliaDas, "adv_foreas_concordia_wilderness", 1556, 0), ClientList = new List<Client>() };
            var arrival = _content.Content.Catalog.Locations[19852];
            _instance.ClientList.Remove(_client);
            wilderness.ClientList.Add(_client);
            _client.Player.MapContextId = AliaDas;
            _client.Player.MapChannel = wilderness;
            _client.Player.Position = new Vector3((float)arrival.PosX, (float)arrival.PosY, (float)arrival.PosZ);
            return Npc(198514, RogersPlacement, 116, wilderness);
        }

        // The recruit reports in to Rogers: refused from the arrival point, then the completion marker and the turn-in in range.
        private void TurnInAtRogers(Creature rogers, uint missionId)
        {
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[missionId].State);
            _missions.CompleteNpcMission(_client, rogers.EntityId, missionId, null);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[missionId].State, "out of conversation range at the arrival point");

            _client.Player.Position = rogers.Position + new Vector3(1f, 0f, 0f);
            Assert.IsTrue(_missions.TryGetConversationStatus(_client, rogers, out var status, out var ids));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);
            CollectionAssert.AreEqual(new[] { missionId }, ids);

            _missions.CompleteNpcMission(_client, rogers.EntityId, missionId, null);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[missionId].State, $"mission {missionId} was not turned in at Rogers");
            Assert.IsFalse(_missions.TryGetConversationStatus(_client, rogers, out _, out _));
            using var context = CharContext(_charConnection);
            Assert.AreEqual((uint)MissionState.Completed, context.CharacterMissionEntries.Single(m => m.CharacterId == CharacterId && m.MissionId == missionId).MissionState);
        }

        private List<PythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                packets.Add(((CallMethodMessage)packet.Message).Packet);
            return packets;
        }

        /// <summary>
        /// The recruit (re-)enters Alia Das: the entered_map rule 1985011 dispenses Training Day over the radio by force
        /// (Decline greyed) exactly while objective 4 of 1995 or 2005 is completed and the character has no 1526 row.
        /// </summary>
        private void EnterAliaDasAndExpectTheTrainingDayOffer()
        {
            Drain();
            _content.OnPlayerEnteredMap(_client);
            var offer = Drain().OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual((TrainingDay, true), (offer.MissionId, offer.Forced));
            CollectionAssert.AreEqual(new uint[] { 116929, 116930 }, offer.MissionInfo.MissionConstantData.RewardInfo.SelectableReward.Select(item => item.ItemTemplateId).ToArray());
            Assert.IsTrue(_client.Player.PendingRadioOffers.Contains(TrainingDay));
        }

        /// <summary>
        /// Accepts Training Day, reports to Training Officer Kincaid in front of the barracks tent and turns it in with the
        /// chosen pistol: 120 credits and the item land together with the completion, and the offer is never repeated.
        /// </summary>
        private void TrainingDayAtKincaid(int selection, uint expectedTemplate)
        {
            _missions.AssignRadioMission(_client, TrainingDay);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[TrainingDay].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(TrainingDay, 1));
            Assert.IsFalse(_client.Player.PendingRadioOffers.Contains(TrainingDay));

            Drain();
            _content.OnPlayerEnteredMap(_client);
            Assert.IsFalse(Drain().OfType<DispenseRadioMissionPacket>().Any(), "Training Day is not offered again once accepted");

            var kincaid = Npc(198515, KincaidPlacement, 2588, _wilderness);
            Assert.IsTrue(ClassAdvancement.TrainerNpcPackages.Contains(kincaid.Npc.NpcPackageId), "Kincaid is the class trainer");
            TalkTo(kincaid, TrainingDay, 1);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(TrainingDay, 1));
            Assert.IsTrue(_missions.TryGetConversationStatus(_client, kincaid, out var status, out var ids));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);
            CollectionAssert.AreEqual(new[] { TrainingDay }, ids);

            // A choice is required: no selection, or one outside the two offered pistols, is refused.
            var credits = _client.Player.Credits[CurencyType.Credits];
            _missions.CompleteNpcMission(_client, kincaid.EntityId, TrainingDay, null);
            _missions.CompleteNpcMission(_client, kincaid.EntityId, TrainingDay, 2);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[TrainingDay].State);

            Drain();
            _missions.CompleteNpcMission(_client, kincaid.EntityId, TrainingDay, selection);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[TrainingDay].State, "Training Day was not turned in at Kincaid");
            Assert.AreEqual(credits + 120, _client.Player.Credits[CurencyType.Credits]);
            var packets = Drain();
            Assert.AreEqual(0u, packets.OfType<InventoryAddItemPacket>().Single().SlotId, "the pistol goes to the first equipment slot");
            Assert.AreEqual(1, packets.OfType<MissionCompletedPacket>().Count());
            var pistol = EntityManager.Instance.GetItem(_client.Player.Inventory.PersonalInventory[0]);
            Assert.AreEqual((expectedTemplate, 1u), (pistol.ItemTemplateId, pistol.StackSize));
            Assert.IsNotNull(pistol.ItemTemplate.WeaponInfo);

            using (var context = CharContext(_charConnection))
            {
                Assert.AreEqual((uint)MissionState.Completed, context.CharacterMissionEntries.Single(m => m.CharacterId == CharacterId && m.MissionId == TrainingDay).MissionState);
                Assert.AreEqual(credits + 120, context.CharacterEntries.Single(c => c.Id == CharacterId).Credit);
                var row = context.CharacterInventoryEntries.Single(entry => entry.CharacterId == CharacterId);
                Assert.AreEqual((0u, expectedTemplate), (row.SlotId, context.ItemEntries.Single(item => item.ItemId == row.ItemId).ItemTemplateId));
            }

            _content.OnPlayerEnteredMap(_client);
            Assert.IsFalse(Drain().OfType<DispenseRadioMissionPacket>().Any(), "a completed Training Day is not offered again");
        }

        private DynamicObject Usable(uint placementId) =>
            _instance.DynamicObjects.SingleOrDefault(obj => _instance.ContentUsables.TryGetValue(obj.EntityId, out var id) && id == placementId);

        private void Use(uint placementId)
        {
            var obj = Usable(placementId);
            Assert.IsNotNull(obj, $"placement {placementId} is not present");
            _client.Player.Position = obj.Position;
            _content.RequestUseContentUsable(_client, new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 0, EntityId = obj.EntityId }, obj);
            _content.ContentUsableRecovery(_instance, new ActionData(_client.Player, ActionId.UseObject, 0, 0) { SourceId = obj.EntityId });
        }

        private void TalkTo(Creature npc, uint missionId, uint objectiveId)
        {
            _client.Player.Position = npc.Position + new Vector3(1f, 0f, 0f);
            _missions.CompleteNpcObjective(_client, npc.EntityId, missionId, objectiveId, 1);
        }

        private void Accept(uint missionId)
        {
            _client.Player.Position = _youngblood.Position + new Vector3(1f, 0f, 0f);
            _missions.AssignNpcMission(_client, _youngblood.EntityId, missionId);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[missionId].State, $"mission {missionId} was not accepted");
        }

        private void Tick()
        {
            _content.RestoreDestroyedUsables(_instance);
            _content.DetonateFuses(_instance);
            _missions.ExpireObjectiveTimers(_instance);
        }

        private bool Holds(uint conditionId) => new ContentState(_client.Player).Evaluate(_content.Content.Catalog.Conditions[conditionId]);

        private int? Fact(string key) => _client.Player.ContentFacts.TryGetValue((Context, key), out var value) ? value : null;

        private Dictionary<string, int> SavedFacts()
        {
            using var context = CharContext(_charConnection);
            return context.CharacterContentFactEntries.Where(f => f.CharacterId == CharacterId).ToDictionary(f => f.FactKey, f => f.Value);
        }

        private MissionObjectiveState Objective(uint missionId, uint objectiveId) =>
            _client.Player.Missions[missionId].Objectives.TryGetValue(objectiveId, out var state) ? state : (MissionObjectiveState)0;

        // Plants and detonates the bomb for the active bomb objective of missionId, then checks the destroyed dropship.
        private void PlantAndDetonate(uint missionId)
        {
            Assert.IsTrue(Usable(BombPlacement).IsEnabled, "the bomb is usable while the bomb objective is open");
            Use(BombPlacement);
            Assert.AreEqual((UseObjectState)114, Usable(BombPlacement).StateId);
            Assert.AreEqual(_tick + 4930, Usable(BombPlacement).FuseAt);
            Assert.IsTrue(_client.Player.Missions[missionId].Timers[1].Disarmed);
            Assert.AreEqual(1, Fact("bootcamp.bomb_planted"));

            _tick += 4929;
            Tick();
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(missionId, 1));

            _tick += 1;
            Tick();
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(missionId, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(missionId, 4));
            CollectionAssert.AreEquivalent(new Dictionary<string, int> { { "bootcamp.dropship_destroyed", 1 } }, SavedFacts());
            Assert.IsNull(Usable(BombPlacement), "the bomb is gone with the wreck");
            Assert.AreEqual((UseObjectState)91, Usable(WreckPlacement).StateId, "the wreck bursts open");
            Assert.IsTrue(Holds(198907), "Van Valkenberg and the reinforcements are present");
        }

        [TestMethod]
        public void TheRecruitDestroysTheDropshipChecksInAndLeavesForAliaDas()
        {
            // Before acceptance: the wreck burns closed, the bomb waits disarmed and unusable, the reinforcements are absent.
            Assert.AreEqual((UseObjectState)31, Usable(WreckPlacement).StateId);
            Assert.AreEqual((UseObjectState)113, Usable(BombPlacement).StateId);
            Assert.IsFalse(Usable(BombPlacement).IsEnabled);
            Assert.IsFalse(Usable(CorpsePlacement).IsEnabled);
            Assert.IsFalse(Holds(198907));

            Accept(1995);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(1995, 2));
            Assert.AreEqual(1, _client.Player.Missions[1995].Objectives.Count, "only 'Locate the missing AFS soldiers' is revealed");

            TalkTo(_woundedSoldier, 1995, 2);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(1995, 2));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(1995, 3));
            Assert.IsTrue(Usable(CorpsePlacement).IsEnabled);
            Assert.IsFalse(Usable(BombPlacement).IsEnabled, "the bomb needs the one from Conrad's corpse first");

            Use(CorpsePlacement);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(1995, 3));
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(1995, 1));
            Assert.AreEqual(600_000L, _client.Player.Missions[1995].Timers[1].RemainingMs);
            Assert.IsFalse(Usable(CorpsePlacement).IsEnabled);

            // 125 s pass on the way to the pad (B1-044 shows 00:02:06 at the plant); the timer keeps running.
            _nowMs += 125_000;
            Tick();
            PlantAndDetonate(1995);

            // Van Valkenberg stands on the exit pad: before his check-in the pad does nothing.
            _client.Player.Position = _vanValkenberg.Position;
            _content.DoWork(_instance);
            _content.DoWork(_instance);
            Assert.AreEqual(0, _transfers.Count);

            TalkTo(_vanValkenberg, 1995, 4);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(1995, 4));
            Assert.IsTrue(_client.Player.Missions[1995].IsCompleteable(_missions.LoadedMissions[1995]), "turned in to Rogers at Alia Das");

            // Still on the pad when the talk ends: the next sample transfers, once.
            _client.Player.Position = _vanValkenberg.Position;
            _content.DoWork(_instance);
            _content.DoWork(_instance);
            Assert.AreEqual(19852u, _transfers.Single().Id);
            Assert.IsTrue(_client.AccountEntry.CanSkipBootcamp);
            using var context = CharContext(_charConnection);
            Assert.IsTrue(context.GameAccountEntries.Single(a => a.Id == AccountId).CanSkipBootcamp);
            var saved = context.CharacterEntries.Single(c => c.Id == CharacterId);
            Assert.AreEqual((1220u, 884.11, 305.8, 347.81), (saved.MapContextId, Math.Round(saved.CoordX, 2), Math.Round(saved.CoordY, 2), Math.Round(saved.CoordZ, 2)));

            // Van Valkenberg is not the receiver; Rogers at Alia Das takes the turn-in (GAP-ROGERS closed).
            _missions.CompleteNpcMission(_client, _vanValkenberg.EntityId, 1995, null);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[1995].State);
            TurnInAtRogers(ArriveAtAliaDas(), 1995);

            // Entering Alia Das after the turn-in still offers Training Day (objective 4 stays completed); the recruit takes the Pistol.
            EnterAliaDasAndExpectTheTrainingDayOffer();
            TrainingDayAtKincaid(0, 116929);
        }

        [TestMethod]
        public void AFailedBombBringsTheShipBackAndTheRetryCanBeFailedAndRetakenToo()
        {
            Accept(1995);
            TalkTo(_woundedSoldier, 1995, 2);
            Use(CorpsePlacement);

            // D13.4: abandoning after the pad was cleared keeps the destroyed dropship, so a re-accepted 1995 has no bomb.
            _client.Player.ContentFacts[(Context, "bootcamp.dropship_destroyed")] = 1;
            _missions.AbandonMission(_client, 1995);
            Assert.IsFalse(_client.Player.Missions.ContainsKey(1995));
            Assert.AreEqual(1, Fact("bootcamp.dropship_destroyed"), "abandoning clears nothing (D13.4 quirk kept)");
            Assert.IsNull(Usable(BombPlacement), "no bomb while the dropship counts as destroyed");

            Accept(1995);
            TalkTo(_woundedSoldier, 1995, 2);
            Use(CorpsePlacement);
            Assert.IsNull(Usable(BombPlacement));

            // "Fail it normally (let the bomb blow up)": the timer fails the objective and mission and the ship comes back.
            _nowMs += 600_000;
            Tick();
            Assert.AreEqual(MissionObjectiveState.Failed, Objective(1995, 1));
            Assert.AreEqual(MissionState.Failded, _client.Player.Missions[1995].State);
            Assert.IsNull(Fact("bootcamp.dropship_destroyed"));
            Assert.AreEqual((UseObjectState)113, Usable(BombPlacement).StateId, "the bomb is back");
            Assert.IsFalse(Usable(BombPlacement).IsEnabled);

            // 1995 is not offered again; the retry 2005 is.
            Assert.IsFalse(_missions.RetryAllowed(_client.Player, _missions.LoadedMissions[1995]));
            Assert.IsTrue(_missions.PrerequisitesSatisfied(_client.Player, _missions.LoadedMissions[2005]));
            Accept(2005);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Objective(2005, 1));
            Assert.AreEqual(600_000L, _client.Player.Missions[2005].Timers[1].RemainingMs, "Here's another bomb. You've got ten minutes.");

            // The retry fails too and is offered again.
            _nowMs += 600_000;
            Tick();
            Assert.AreEqual(MissionState.Failded, _client.Player.Missions[2005].State);
            Assert.IsTrue(_missions.RetryAllowed(_client.Player, _missions.LoadedMissions[2005]));
            Accept(2005);

            PlantAndDetonate(2005);
            TalkTo(_vanValkenberg, 2005, 4);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(2005, 4));
            Assert.IsTrue(Holds(198909), "the exit pad is armed by the retry's check-in");

            // The observed order: the forced offer is already open on arrival, before the report to Rogers; this recruit takes
            // the Pulse Pistol.
            var rogers = ArriveAtAliaDas();
            EnterAliaDasAndExpectTheTrainingDayOffer();
            TurnInAtRogers(rogers, 2005);
            TrainingDayAtKincaid(1, 116930);
        }

        /// <summary>
        /// The tier-2 class choice answers with the matching class-gear mission (WildernessClassGear): SelectNewCharacterClass
        /// releases level 5, the class_selected rule dispenses 2010/2011 by force, and Caufield (emulator creature 132 with
        /// dialogue package 133) pays the whole six-piece load-out at the turn-in. The offer is never repeated.
        /// </summary>
        [DataTestMethod]
        [DataRow(2u, ClassGearSoldier, 122859u, 122865u, 21)]
        [DataRow(3u, ClassGearSpecialist, 122866u, 122871u, 30)]
        public void ChoosingATierOffersTheClassGearMissionAndCaufieldPaysTheLoadOut(uint classId, uint missionId, uint firstGear, uint lastGear, int armorSkill)
        {
            ArriveAtAliaDas();
            var kincaid = Npc(198515, KincaidPlacement, 2588, _wilderness);

            // A Recruit held at the gate: level 4 with the level-5 threshold reached, standing by the trainer.
            _client.Player.Class = 1;
            _client.Player.Level = 4;
            _client.Player.Experience = 43_000;
            foreach (Attributes attribute in Enum.GetValues(typeof(Attributes)))
                _client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            for (var slot = 0; slot < 22; slot++)
                _client.Player.Inventory.EquippedInventory.Add(0);
            _client.Player.Position = kincaid.Position + new Vector3(1f, 0f, 0f);
            Drain();

            ManifestationManager.Instance.SelectNewCharacterClass(_client, classId);
            Assert.AreEqual((classId, 5), (_client.Player.Class, _client.Player.Level), "the class changes and the withheld level is released");

            var offer = Drain().OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual((missionId, true), (offer.MissionId, offer.Forced));
            var offered = offer.MissionInfo.MissionConstantData.RewardInfo.FixedReward.FixedItems.Select(item => item.ItemTemplateId).ToArray();
            Assert.AreEqual(6, offered.Length);
            Assert.AreEqual((firstGear, lastGear), (offered.First(), offered.Last()));
            Assert.IsTrue(_client.Player.PendingRadioOffers.Contains(missionId));

            // Report to Quartermaster Caufield in the supply tent: the package-133 conversation completes the objective.
            _missions.AssignRadioMission(_client, missionId);
            var caufield = NpcAt(Caufield, CaufieldPackage, new Vector3(756.88f, 293.97f, 407.07f), _wilderness);
            TalkTo(caufield, missionId, 1);
            Assert.AreEqual(MissionObjectiveState.Completed, Objective(missionId, 1));
            Assert.IsTrue(_missions.TryGetConversationStatus(_client, caufield, out var status, out var ids));
            Assert.AreEqual(ConversationStatus.MissionComplete, status);
            CollectionAssert.AreEqual(new[] { missionId }, ids);

            Drain();
            _missions.CompleteNpcMission(_client, caufield.EntityId, missionId, null);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[missionId].State, $"mission {missionId} was not turned in at Caufield");

            var packets = Drain();
            Assert.AreEqual(6, packets.OfType<InventoryAddItemPacket>().Count(), "the whole six-piece load-out is granted");
            Assert.AreEqual(1, packets.OfType<MissionCompletedPacket>().Count());
            var granted = _client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(id => EntityManager.Instance.GetItem(id)).ToList();
            CollectionAssert.AreEqual(offered.OrderBy(id => id).ToArray(),
                granted.Select(item => item.ItemTemplateId).OrderBy(id => id).ToArray());
            Assert.IsTrue(granted.Any(item => item.ItemTemplate?.EquipableInfo?.SkillId == armorSkill), "the load-out carries the class skill requirement");

            using (var context = CharContext(_charConnection))
                Assert.AreEqual((uint)MissionState.Completed, context.CharacterMissionEntries.Single(m => m.CharacterId == CharacterId && m.MissionId == missionId).MissionState);

            // The class rule's MissionAbsent term keeps the offer from repeating.
            _content.OnPlayerEnteredMap(_client);
            Assert.IsFalse(Drain().OfType<DispenseRadioMissionPacket>().Any(), $"a completed {missionId} is not offered again");
        }
    }
}
