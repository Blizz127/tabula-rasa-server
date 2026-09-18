using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
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
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
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
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The D11 opening: new-character start at <c>spawn.first_login</c>, the first-login Wonkavate,
    /// the 1990 radio offer, the two Eloh approach greetings, and McAllister's follow-me walk.
    /// Drives the shipped creation, login, and content-runtime entry points against the migrated seed.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BootcampOpeningTests
    {
        private const uint CharacterId = 101;
        private const uint AccountId = 10;
        private const uint Camp = 1985;
        private const uint FirstLoginLocation = 19851;
        private const uint McAllisterPlacement = 198650;
        private const uint McAllisterCreature = 198500;
        private const uint McAllisterDestination = 19853;
        private const uint Initiation = 1990;
        private const uint MapVersion = 783;

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

        private sealed class References : IContentReferences
        {
            private readonly IReadOnlyDictionary<uint, Mission> _missions;
            public References(IReadOnlyDictionary<uint, Mission> missions) => _missions = missions;
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
        }

        [ClassCleanup]
        public static void DisposeWorld() => _worldConnection?.Dispose();

        private SqliteConnection _charConnection;
        private Factory _factory;
        private MissionManager _missions;
        private MissionContentManager _content;
        private Client _client;
        private MapChannel _instance;
        private Creature _mcallister;
        private readonly List<Creature> _npcs = new();

        [TestInitialize]
        public void Initialize()
        {
            _charConnection = new SqliteConnection("Data Source=:memory:");
            _charConnection.Open();
            _factory = new Factory(_charConnection, _worldConnection);

            using (var context = CharContext(_charConnection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = AccountId, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = CharacterId, AccountId = AccountId, Slot = 1, Name = "Recruit", Level = 2,
                    MapContextId = Camp, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            _missions = new MissionManager(_factory) { NowMs = () => 1_700_000_000_000 };
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, _missions);
            _missions.LoadMissions();

            _content = new MissionContentManager(_factory) { Missions = _missions, TickNow = () => 50_000 };
            _content.Load(() => new BootcampConfig { EntryMode = BootcampEntryMode.AllNewCharacters },
                new References(_missions.LoadedMissions), _missions.LoadedMissions);
            Assert.IsFalse(_content.Content.WithheldLocations.Contains(FirstLoginLocation),
                string.Join(" | ", _content.Content.Gaps));
            Assert.IsTrue(_content.Content.LiveRules.Any(rule => rule.Id == 1985000),
                "1990 radio-offer rule is withheld: " + string.Join(" | ", _content.Content.Gaps));
            _missions.Content = _content;
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
            if (_instance != null)
            {
                foreach (var obj in _instance.DynamicObjects.ToList())
                {
                    EntityManager.Instance.UnregisterDynamicObject(obj.EntityId);
                    EntityManager.Instance.UnregisterEntity(obj.EntityId);
                }
            }
            foreach (var npc in _npcs)
            {
                EntityManager.Instance.UnregisterCreature(npc.EntityId);
                EntityManager.Instance.UnregisterEntity(npc.EntityId);
            }
            _charConnection?.Dispose();
        }

        [TestMethod]
        public void CreationPersistsTheEvidenceFirstLoginWhenEntryIsOnAndWildernessWhenOff()
        {
            var evidence = EvidenceSpawn();
            var live = _content.Content.Catalog.Locations[FirstLoginLocation];
            Assert.AreEqual((byte)ContentLocationPurpose.NewCharacterStart, live.Purpose);
            Assert.AreEqual(Camp, live.MapContextId);
            Assert.AreEqual(evidence.X, live.PosX);
            Assert.AreEqual(evidence.Y, live.PosY);
            Assert.AreEqual(evidence.Z, live.PosZ);
            Assert.AreEqual(evidence.Rotation, live.Rotation);

            var on = BootcampEntryGate.StartLocation(
                new BootcampConfig { EntryMode = BootcampEntryMode.AllNewCharacters }, AccountId, _content.Content);
            Assert.AreSame(live, on);

            using var context = CharContext(_charConnection);
            var characters = new CharacterRepository(context);
            var account = context.GameAccountEntries.Single(a => a.Id == AccountId);

            var placed = characters.Create(account, 2, "Camp", 1, 1.0, 0, _content.NewCharacterStart(account.Id));
            Assert.AreEqual((Camp, live.PosX, live.PosY, live.PosZ, live.Rotation),
                (placed.MapContextId, placed.CoordX, placed.CoordY, placed.CoordZ, placed.Rotation));

            var disabled = new MissionContentManager(_factory);
            disabled.Load(() => new BootcampConfig(), new References(_missions.LoadedMissions), _missions.LoadedMissions);
            var wilderness = characters.Create(account, 3, "Wild", 1, 1.0, 0, disabled.NewCharacterStart(account.Id));
            Assert.AreEqual((1220u, 894.9d, 307.9d, 347.1d, 0d),
                (wilderness.MapContextId, wilderness.CoordX, wilderness.CoordY, wilderness.CoordZ, wilderness.Rotation));
        }

        [TestMethod]
        public void FirstLoginWonkavateUsesThePersistedBootcampStart()
        {
            var live = _content.Content.Catalog.Locations[FirstLoginLocation];
            using var context = CharContext(_charConnection);
            var characters = new CharacterRepository(context);
            var account = context.GameAccountEntries.Single(a => a.Id == AccountId);
            var character = characters.Create(account, 2, "Camp", 1, 1.0, 0, live);

            var maps = new MapChannelManager(null, () => 0)
            {
                IsPerCharacterContext = contextId => contextId == Camp,
                PopulateInstance = _ => { }
            };
            maps.MapChannelArray.Add(Camp, new MapChannel { MapInfo = new MapInfo(Camp, "adv_bootcamp", MapVersion, 4), ClientList = new List<Client>() });
            maps.MapChannelArray.Add(1220, new MapChannel { MapInfo = new MapInfo(1220, "wilderness", 1556, 0), ClientList = new List<Client>() });

            var client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.CharacterSelection };
            client.Player = new Manifestation(character, new Dictionary<EquipmentData, AppearanceData>());
            client.Player.MapChannel = maps.ChannelForEntry(character.Id, client.Player.MapContextId);

            maps.PassClientToMapInstance(client);
            var packets = Drain(client);
            Assert.AreEqual(1, packets.OfType<PreWonkavatePacket>().Count());
            var wonkavate = packets.OfType<WonkavatePacket>().Single();
            Assert.AreEqual(Camp, wonkavate.MapContextId);
            Assert.AreEqual(client.Player.MapChannel.InstanceId, wonkavate.MapInstanceId);
            Assert.AreEqual(MapVersion, wonkavate.MapVersion);
            Assert.AreEqual((float)live.PosX, wonkavate.Position.X, 0.001);
            Assert.AreEqual((float)live.PosY, wonkavate.Position.Y, 0.001);
            Assert.AreEqual((float)live.PosZ, wonkavate.Position.Z, 0.001);
            Assert.AreEqual((float)live.Rotation, wonkavate.Orientation, 0.001);
            Assert.IsTrue(wonkavate.MapInstanceId > 1);
            Assert.IsFalse(packets.Any(packet => packet.Opcode == GameOpcode.RunCameraScript),
                "map camera script 1 is not a proven first-login server trigger");
        }

        [TestMethod]
        public void EnteredMapOffersInitiationAndTheElohApproachesForceConverseThenMcAllisterWalks()
        {
            var start = _content.Content.Catalog.Locations[FirstLoginLocation];
            var area1 = _content.Content.Catalog.Areas[198600];
            var area2 = _content.Content.Catalog.Areas[198601];
            var walk = _content.Content.Catalog.Locations[McAllisterDestination];

            _instance = new MapChannel
            {
                MapInfo = new MapInfo(Camp, "adv_bootcamp", MapVersion, 4),
                OwnerCharacterId = CharacterId,
                InstanceId = 7,
                ClientList = new List<Client>()
            };

            _mcallister = PlaceMcAllister(_instance);

            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = AccountId });
            _client.Player = new Manifestation
            {
                Id = CharacterId, Level = 2, MapContextId = Camp, MapChannel = _instance,
                Position = new Vector3((float)start.PosX, (float)start.PosY, (float)start.PosZ),
                Rotation = (float)start.Rotation
            };
            _client.Player.Credits[CurencyType.Credits] = 0;
            _client.Player.Credits[CurencyType.Prestige] = 0;
            _client.Player.Inventory.PersonalInventory.AddRange(new ulong[250]);
            _instance.ClientList.Add(_client);

            Drain(_client);
            _content.OnPlayerEnteredMap(_client);
            var entered = Drain(_client);
            var offer = entered.OfType<DispenseRadioMissionPacket>().Single();
            Assert.AreEqual((Initiation, true), (offer.MissionId, offer.Forced));
            Assert.IsFalse(entered.Any(packet => packet.Opcode == GameOpcode.RunCameraScript),
                "the CG intro movie and map camera script 1 stay unfired as server triggers");

            _missions.AssignRadioMission(_client, Initiation);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[Initiation].State);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[Initiation].Objectives[1]);
            Drain(_client);

            StandIn(area1, start.PosY);
            _content.DoWork(_instance);
            var firstVision = Drain(_client).OfType<ForceConversePacket>().Single();
            Assert.AreEqual((1634u, 10598u), (firstVision.GreetingId, firstVision.NpcNameId));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[Initiation].Objectives[1]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[Initiation].Objectives[2]);

            StandIn(area2, start.PosY);
            _content.DoWork(_instance);
            var secondVision = Drain(_client).OfType<ForceConversePacket>().Single();
            Assert.AreEqual((1635u, 10598u), (secondVision.GreetingId, secondVision.NpcNameId));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[Initiation].Objectives[2]);

            _client.Player.Position = _mcallister.Position + new Vector3(1f, 0f, 0f);
            _missions.CompleteNpcMission(_client, _mcallister.EntityId, Initiation, null);
            Assert.AreEqual(MissionState.Completed, _client.Player.Missions[Initiation].State);

            Assert.AreEqual(BehaviorManager.BehaviorActionFollowingPath, _mcallister.Controller.CurrentAction);
            var node = _mcallister.Controller.AiPathFollowing.GeneralPath.PathNodeList.Single().Pos;
            Assert.AreEqual((float)walk.PosX, node[0], 0.001);
            Assert.AreEqual((float)walk.PosY, node[1], 0.001);
            Assert.AreEqual((float)walk.PosZ, node[2], 0.001);
        }

        private Creature PlaceMcAllister(MapChannel channel)
        {
            var placement = _content.Content.Catalog.Placements[McAllisterPlacement];
            Assert.AreEqual(McAllisterCreature, placement.CreatureId);
            var creature = new Creature
            {
                DbId = McAllisterCreature,
                ContentPlacementId = McAllisterPlacement,
                MapContextId = Camp,
                MapChannel = channel,
                Position = new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ),
                Level = 10,
                State = CharacterState.Idle,
                Npc = new Npc()
            };
            CellManager.Instance.AddToWorld(channel, creature);
            _npcs.Add(creature);
            return creature;
        }

        private void StandIn(ContentAreaEntry area, double y)
            => _client.Player.Position = new Vector3((float)area.PosX, (float)y, (float)area.PosZ);

        private static (double X, double Y, double Z, double Rotation) EvidenceSpawn()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-positions.json")));
            var spawn = document.RootElement.GetProperty("positions").EnumerateArray()
                .Single(position => position.GetProperty("position_key").GetString() == "spawn.first_login");
            return (spawn.GetProperty("x").GetDouble(), spawn.GetProperty("y").GetDouble(),
                spawn.GetProperty("z").GetDouble(), spawn.GetProperty("rotation").GetDouble());
        }

        private static List<ServerPythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
    }
}
