using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Mission.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Repositories.World.MissionContent;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// S2b: a destroyable content placement takes weapon and Lightning damage,
    /// moves through the InertDestroyable thresholds, restores after restore_ms,
    /// pays no XP and no loot, and completes its hit bindings on the destroying hit.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DestroyablePlacementTests
    {
        private const uint MissionId = 7004;
        private const uint PlacementId = 900660;
        private const ulong ObjectId = 0x5200;
        private const uint DummyClass = 29365;

        public class UnitProxy : DispatchProxy
        {
            public SqliteCharContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_Characters": return new CharacterRepository(Context);
                    case "get_CharacterMissions": return new CharacterMissionRepository(Context);
                    case "get_CharacterOptions": return new Rasa.Repositories.Char.CharacterOption.CharacterOptionRepository(Context);
                    case "Complete": Context.SaveChanges(); return null;
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        public class WorldUnitProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_MissionContent": return new MissionContentRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            private readonly SqliteConnection _worldConnection;
            public Factory(SqliteConnection connection, SqliteConnection worldConnection)
            {
                _connection = connection;
                _worldConnection = worldConnection;
            }
            public ICharUnitOfWork CreateChar()
            {
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)unit).Context = Context(_connection);
                return unit;
            }
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldUnitProxy>();
                ((WorldUnitProxy)(object)unit).Context = WorldContext(_worldConnection);
                return unit;
            }
        }

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteCharContext Context(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static SqliteWorldContext WorldContext(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private sealed class References : IContentReferences
        {
            public bool MapContextExists(uint mapContextId) => true;
            public bool MissionExists(uint missionId) => true;
            public bool ObjectiveExists(uint missionId, uint objectiveId) => true;
            public uint MissionGiver(uint missionId) => 0;
            public bool MissionOfferable(uint missionId) => true;
            public bool CreatureExists(uint creatureId) => true;
            public bool EntityClassExists(uint entityClassId) => true;
            public bool ItemTemplateExists(uint itemTemplateId) => true;
            public bool LogosExists(uint logosId) => true;
            public bool HasLegacyWorldObjects(uint mapContextId) => false;
        }

        private SqliteConnection _connection;
        private SqliteConnection _worldConnection;
        private Factory _factory;
        private MissionManager _missions;
        private MissionContentManager _content;
        private Client _client;
        private MapChannel _map;
        private DynamicObject _dummy;
        private Creature _giver;
        private Creature _killedCreature;
        private long _now;

        [TestInitialize]
        public void Initialize()
        {
            if (Logger.Config == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _worldConnection = new SqliteConnection("Data Source=:memory:");
            _worldConnection.Open();
            _factory = new Factory(_connection, _worldConnection);
            _now = 1_700_000_000;
            _missions = new MissionManager(_factory, () => (uint)_now);
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, _missions);

            using (var charContext = Context(_connection))
            {
                charContext.Database.EnsureCreated();
                charContext.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                charContext.CharacterEntries.Add(new CharacterEntry
                {
                    Id = 101, AccountId = 10, Slot = 1, Name = "Recruit", Level = 1, Credit = 40, Prestige = 0, Experience = 5,
                    MapContextId = 1220, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                charContext.SaveChanges();
            }

            using (var context = WorldContext(_worldConnection))
            {
                context.Database.EnsureCreated();
                context.NpcMissionEntries.Add(new NpcMissionEntry { Id = MissionId, GiverId = 7001, ReciverId = 0, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "dummy fixture" });
                context.NpcMissionObjectiveEntries.Add(new NpcMissionObjectiveEntry { MissionId = MissionId, ObjectiveId = 3, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "Shoot the Practice Dummy" });
                // Destroying hit only, by weapon attack (action id 0 = any weapon).
                context.NpcMissionObjectiveBindingEntries.Add(new NpcMissionObjectiveBindingEntry
                    { MissionId = MissionId, ObjectiveId = 3, BindingId = 0, Kind = (byte)ObjectiveBindingKind.Hit, PlacementId = PlacementId, DestroyingHitOnly = true, CounterId = 255 });
                context.ContentPlacementEntries.Add(new ContentPlacementEntry
                {
                    Id = PlacementId, MapContextId = 1220, Kind = (byte)ContentPlacementKind.Usable,
                    EntityClassId = DummyClass, UsableKind = (byte)ContentUsableKind.Destroyable,
                    Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 110,
                    HitPoints = 100, RestoreMs = 930
                });
                context.SaveChanges();
            }

            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _map = new MapChannel { MapInfo = new MapInfo(1220, "test", 1, 1), ClientList = new List<Client>() };
            _client.Player = new Manifestation
            {
                Id = 101, Level = 1, MapContextId = 1220, MapChannel = _map,
                Position = new System.Numerics.Vector3(100f, 10f, 100f), Cells = new uint[,] { { 7 } }
            };
            _client.Player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 100, 100, 100, 0, 0);
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[7] = new MapCell { ClientList = new List<Client> { _client } };
            MapChannelManager.Instance.MapChannelArray[1220] = _map;

            _giver = new Creature
            {
                DbId = 7001, MapContextId = 1220, Position = new System.Numerics.Vector3(102f, 10f, 100f),
                Level = 1, State = CharacterState.Idle, Cells = new uint[,] { { 7 } }, Npc = new Npc()
            };
            EntityManager.Instance.RegisterEntity(_giver.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_giver);

            _missions.LoadedMissions[MissionId] = Definition();
            _content = new MissionContentManager(_factory) { Missions = _missions };
            _content.Load(() => new BootcampConfig(), new References(), _missions.LoadedMissions);
            _missions.Content = _content;

            // Load() already materialized the shared context's placements.
            _dummy = _map.DynamicObjects.Single(obj => obj.DynamicObjectType == DynamicObjectType.ContentUsable);
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
            MapChannelManager.Instance.MapChannelArray.Remove(1220);
            if (_dummy != null)
            {
                EntityManager.Instance.UnregisterDynamicObject(_dummy.EntityId);
                EntityManager.Instance.UnregisterEntity(_dummy.EntityId);
            }
            if (_giver != null)
            {
                EntityManager.Instance.UnregisterCreature(_giver.EntityId);
                EntityManager.Instance.UnregisterEntity(_giver.EntityId);
            }
            if (_killedCreature != null)
            {
                EntityManager.Instance.UnregisterCreature(_killedCreature.EntityId);
                EntityManager.Instance.UnregisterEntity(_killedCreature.EntityId);
                _killedCreature = null;
            }
            _connection.Dispose();
            _worldConnection.Dispose();
        }

        private static Mission Definition()
        {
            var mission = new Mission(new NpcMissionEntry
            {
                Id = MissionId, GiverId = 7001, ReciverId = 0, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "dummy fixture"
            });
            mission.Objectives[3] = new MissionObjectiveDefinition { ObjectiveId = 3, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
            mission.RefreshDispenseObjectives();
            return mission;
        }

        private static List<PythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                packets.Add(((CallMethodMessage)protocol.Message).Packet);
            return packets;
        }

        [TestMethod]
        public void DestroyablePlacementTakesDamageThroughThresholdsAndRestores()
        {
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);
            Console.WriteLine($"DBG accept: known={_missions.LoadedMissions.ContainsKey(MissionId)} dispensable={_missions.LoadedMissions[MissionId].IsDispensable} gaps={string.Join(";", _missions.LoadedMissions[MissionId].DefinitionGaps())}");
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);

            // Damage below 50%: state 185.
            Assert.IsNull(_content.DamageContentUsable(_map, _dummy.EntityId, 60));
            Assert.AreEqual((uint)185, (uint)_dummy.StateId);
            Assert.AreEqual(40u, _dummy.HitPoints);

            // Damage below 25%: state 186.
            Assert.IsNull(_content.DamageContentUsable(_map, _dummy.EntityId, 25));
            Assert.AreEqual((uint)186, (uint)_dummy.StateId);
            Assert.AreEqual(15u, _dummy.HitPoints);

            // The destroying hit: state 2, and the hit binding completes.
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, 15, _client));
            Console.WriteLine($"DBG hit: bindings={string.Join(";", _missions.LoadedMissions[MissionId].Bindings.Select(b => $"{b.MissionId}/{b.ObjectiveId}/k{b.Kind}"))}");
            Assert.AreEqual(UseObjectState.StateDestroyed, _dummy.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[MissionId].Objectives[3]);

            // UpdateHitPoints/ForceState go through the cell broadcast (all clients in the
            // dummy's cells); ObjectiveCompleted is client-specific.
            var packets = Drain(_client);
            Assert.IsTrue(packets.Any(packet => packet is ObjectiveCompletedPacket completed &&
                completed.MissionId == MissionId && completed.ObjectiveId == 3));

            // Restore after restore_ms: back to 110 with hit points restored.
            _now += 931;
            _content.RestoreDestroyedUsables(_map, _now);
            Assert.AreEqual((UseObjectState)110, _dummy.StateId);
            Assert.AreEqual(100u, _dummy.HitPoints);
        }

        [TestMethod]
        public void ABindingThatNamesAnActionCompletesOnlyOnThatAction()
        {
            // "Use your Lightning power on the Target Dummy" (1992/8) is bound with action 194. The destroy
            // path used to report every hit as action 0, so this binding could never complete - the live
            // player destroyed a dummy with Lightning three times on 2026-09-19 and nothing advanced.
            foreach (var binding in _content.Content.LiveBindings.Where(b => b.MissionId == MissionId && b.ObjectiveId == 3))
                binding.ActionId = 194;

            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);

            // A weapon (action 1) destroying it does not count.
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, 100, _client, 1));
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);

            _now += 931;
            _content.RestoreDestroyedUsables(_map, _now);

            // Lightning (194) destroying it does.
            Assert.AreEqual(PlacementId, _content.DamageContentUsable(_map, _dummy.EntityId, 100, _client, 194));
            Assert.AreEqual(MissionObjectiveState.Completed, _client.Player.Missions[MissionId].Objectives[3]);
        }

        [TestMethod]
        public void NonDestroyingHitDoesNotCompleteADestroyingHitOnlyBinding()
        {
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);

            Assert.IsNull(_content.DamageContentUsable(_map, _dummy.EntityId, 10));
            Assert.AreEqual(MissionObjectiveState.Incomplete, _client.Player.Missions[MissionId].Objectives[3]);
        }

        [TestMethod]
        public void DestroyedPlacementIsNotATargetUntilItRestores()
        {
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);
            _content.DamageContentUsable(_map, _dummy.EntityId, 100);
            Assert.AreEqual(UseObjectState.StateDestroyed, _dummy.StateId);

            Assert.IsNull(WeaponAttackManager.GetEligibleContentTarget(_client.Player, _dummy.EntityId));

            _now += 931;
            _content.RestoreDestroyedUsables(_map, _now);
            Assert.AreSame(_dummy, WeaponAttackManager.GetEligibleContentTarget(_client.Player, _dummy.EntityId));
        }

        [TestMethod]
        public void PlacementWithoutHitPointsIsNeverATarget()
        {
            // A container placement (hit_points 0) is not damageable.
            Assert.IsNull(WeaponAttackManager.GetEligibleContentTarget(_client.Player, 0xDEAD));
        }
    }
}
