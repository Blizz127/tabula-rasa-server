using System;
using System.Collections.Generic;
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
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterContentFact;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures.Content;
using Rasa.Repositories.World.MissionContent;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// S5 usables (build plan 1.6): planting a bomb arms it (113 → 114) and disarms the timer of the objective its
    /// detonation completes, together with the placement_state_entered 114 rules; the fuse detonates it (114 → 115)
    /// and the bound objective completes in the same transaction as the 115 rules. A generic-use placement completes
    /// its use_completed binding. Synthetic mission and placements; tick order as in the map worker.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class BombPlacementTests
    {
        private const uint MissionId = 7005;
        private const uint BombPlacementId = 900670;
        private const uint CorpsePlacementId = 900671;
        private const uint WreckPlacementId = 900672;
        private const uint ContextId = 1220;
        private const uint FuseMs = 5300;
        private const uint LimitSeconds = 126;

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
                    case "get_CharacterContentFacts": return new CharacterContentFactRepository(Context);
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
                ((UnitProxy)(object)unit).Context = CharContext(_connection);
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

        private static SqliteCharContext CharContext(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static SqliteWorldContext WorldContext(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

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
        private MissionManager _missions;
        private MissionContentManager _content;
        private Client _client;
        private MapChannel _map;
        private Creature _giver;
        private DynamicObject _bomb;
        private DynamicObject _corpse;
        private long _tick;
        private long _nowMs;

        [TestInitialize]
        public void Initialize()
        {
            if (Logger.Config == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _worldConnection = new SqliteConnection("Data Source=:memory:");
            _worldConnection.Open();
            var factory = new Factory(_connection, _worldConnection);
            _nowMs = 1_700_000_000_000;
            _tick = 50_000;
            _missions = new MissionManager(factory, () => (uint)(_nowMs / 1000)) { NowMs = () => _nowMs };
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, _missions);

            using (var context = CharContext(_connection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Name = "Fixture", FamilyName = "Fixture", Email = "fixture@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry
                {
                    Id = 101, AccountId = 10, Slot = 1, Name = "Recruit", Level = 2, MapContextId = ContextId, CreatedAt = DateTime.UtcNow, LastPvPClan = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            using (var context = WorldContext(_worldConnection))
            {
                context.Database.EnsureCreated();
                context.NpcMissionEntries.Add(new NpcMissionEntry { Id = MissionId, GiverId = 7001, ReciverId = 0, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "bomb fixture" });
                // 3: use the corpse; 1: destroy the dropship before the timer expires (revealed by 3); 4: check in.
                context.NpcMissionObjectiveEntries.AddRange(
                    new NpcMissionObjectiveEntry { MissionId = MissionId, ObjectiveId = 3, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "corpse" },
                    new NpcMissionObjectiveEntry { MissionId = MissionId, ObjectiveId = 1, Ordinal = 2, IsRequired = true, RevealedOnAccept = false, Comment = "bomb" });
                context.NpcMissionObjectiveTransitionEntries.Add(new NpcMissionObjectiveTransitionEntry { MissionId = MissionId, CompletedObjectiveId = 3, RevealedObjectiveId = 1 });
                context.NpcMissionObjectiveBindingEntries.AddRange(
                    new NpcMissionObjectiveBindingEntry { MissionId = MissionId, ObjectiveId = 3, BindingId = 0, Kind = (byte)ObjectiveBindingKind.UseCompleted, PlacementId = CorpsePlacementId, CounterId = 255 },
                    new NpcMissionObjectiveBindingEntry { MissionId = MissionId, ObjectiveId = 1, BindingId = 0, Kind = (byte)ObjectiveBindingKind.PlacementState, PlacementId = BombPlacementId, TargetState = 115, CounterId = 255 });
                context.NpcMissionObjectiveTimerEntries.Add(new NpcMissionObjectiveTimerEntry { MissionId = MissionId, ObjectiveId = 1, LimitSeconds = LimitSeconds, OnExpire = (byte)ObjectiveTimerExpiry.FailObjective });
                context.ContentPlacementEntries.AddRange(
                    new ContentPlacementEntry
                    {
                        Id = BombPlacementId, MapContextId = ContextId, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7870, UsableKind = (byte)ContentUsableKind.Bomb,
                        Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 113, WindupMs = 1450, FuseMs = FuseMs
                    },
                    new ContentPlacementEntry
                    {
                        Id = CorpsePlacementId, MapContextId = ContextId, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 7871, UsableKind = (byte)ContentUsableKind.GenericUse,
                        Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 44, WindupMs = 1000
                    },
                    new ContentPlacementEntry
                    {
                        Id = WreckPlacementId, MapContextId = ContextId, Kind = (byte)ContentPlacementKind.Usable, EntityClassId = 24586, UsableKind = (byte)ContentUsableKind.Structure,
                        Behavior = (byte)ContentPlacementBehavior.Stationary, InitialState = 31
                    });
                context.ContentRuleEntries.AddRange(
                    new ContentRuleEntry { Id = 9101, MapContextId = ContextId, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = BombPlacementId, StateId = 114 },
                    new ContentRuleEntry { Id = 9102, MapContextId = ContextId, Event = (byte)ContentRuleEvent.PlacementStateEntered, PlacementId = BombPlacementId, StateId = 115 });
                context.ContentRuleActionEntries.AddRange(
                    new ContentRuleActionEntry { RuleId = 9101, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "bootcamp.bomb_planted", FactValue = 1 },
                    new ContentRuleActionEntry { RuleId = 9102, Sequence = 0, Action = (byte)ContentRuleAction.SetFact, FactKey = "bootcamp.dropship_destroyed", FactValue = 1 },
                    new ContentRuleActionEntry { RuleId = 9102, Sequence = 1, Action = (byte)ContentRuleAction.ClearFact, FactKey = "bootcamp.bomb_planted" },
                    new ContentRuleActionEntry { RuleId = 9102, Sequence = 2, Action = (byte)ContentRuleAction.SetPlacementState, PlacementId = WreckPlacementId, StateId = 91 });
                context.SaveChanges();
            }

            _client = new Client(factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            _map = new MapChannel { MapInfo = new MapInfo(ContextId, "test", 1, 1), ClientList = new List<Client>() };
            _client.Player = new Manifestation
            {
                Id = 101, Level = 2, MapContextId = ContextId, MapChannel = _map,
                Position = new System.Numerics.Vector3(100f, 10f, 100f), Cells = new uint[,] { { 7 } }
            };
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[7] = new MapCell { ClientList = new List<Client> { _client } };
            MapChannelManager.Instance.MapChannelArray[ContextId] = _map;

            _giver = new Creature
            {
                DbId = 7001, MapContextId = ContextId, Position = new System.Numerics.Vector3(102f, 10f, 100f),
                Level = 1, State = CharacterState.Idle, Cells = new uint[,] { { 7 } }, Npc = new Npc()
            };
            EntityManager.Instance.RegisterEntity(_giver.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_giver);

            using (var unit = factory.CreateWorld())
            {
                var definition = new Mission(new NpcMissionEntry { Id = MissionId, GiverId = 7001, ReciverId = 0, Level = 1, GroupType = 1, CategoryId = 10000032, Comment = "bomb fixture" });
                definition.Objectives[3] = new MissionObjectiveDefinition { ObjectiveId = 3, Ordinal = 1, IsRequired = true, RevealedOnAccept = true };
                definition.Objectives[1] = new MissionObjectiveDefinition { ObjectiveId = 1, Ordinal = 2, IsRequired = true, RevealedOnAccept = false };
                definition.Transitions[3] = new List<uint> { 1 };
                definition.RefreshDispenseObjectives();
                _missions.LoadedMissions[MissionId] = definition;
            }

            _content = new MissionContentManager(factory) { Missions = _missions, TickNow = () => _tick };
            _content.Load(() => new BootcampConfig(), new References(), _missions.LoadedMissions);
            Assert.AreEqual(0, _content.Content.Gaps.Count, string.Join(" | ", _content.Content.Gaps));
            _missions.Content = _content;

            _bomb = Usable(BombPlacementId);
            _corpse = Usable(CorpsePlacementId);
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);
            Assert.AreEqual(MissionState.Active, _client.Player.Missions[MissionId].State);
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(MissionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, null);
            MapChannelManager.Instance.MapChannelArray.Remove(ContextId);
            foreach (var obj in _map.DynamicObjects)
            {
                EntityManager.Instance.UnregisterDynamicObject(obj.EntityId);
                EntityManager.Instance.UnregisterEntity(obj.EntityId);
            }
            EntityManager.Instance.UnregisterCreature(_giver.EntityId);
            EntityManager.Instance.UnregisterEntity(_giver.EntityId);
            _connection.Dispose();
            _worldConnection.Dispose();
        }

        private DynamicObject Usable(uint placementId) =>
            _map.DynamicObjects.Single(obj => _map.ContentUsables.TryGetValue(obj.EntityId, out var id) && id == placementId);

        private void Use(DynamicObject obj)
        {
            _content.RequestUseContentUsable(_client, new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 0, EntityId = obj.EntityId }, obj);
            _content.ContentUsableRecovery(_map, new ActionData(_client.Player, ActionId.UseObject, 0, 0) { SourceId = obj.EntityId });
        }

        private PlayerMission Progress => _client.Player.Missions[MissionId];

        private CharacterMissionObjectiveEntry SavedObjective(uint objectiveId)
        {
            using var context = CharContext(_connection);
            return context.CharacterMissionObjectiveEntries.Single(o => o.CharacterId == 101 && o.MissionId == MissionId && o.ObjectiveId == objectiveId);
        }

        private Dictionary<string, int> SavedFacts()
        {
            using var context = CharContext(_connection);
            return context.CharacterContentFactEntries.Where(f => f.CharacterId == 101).ToDictionary(f => f.FactKey, f => f.Value);
        }

        private List<PythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                packets.Add(((CallMethodMessage)protocol.Message).Packet);
            return packets;
        }

        // One map tick after the recoveries: restores, fuses, then timers (MapChannelManager worker order).
        private void Tick()
        {
            _content.RestoreDestroyedUsables(_map);
            _content.DetonateFuses(_map);
            _missions.ExpireObjectiveTimers(_map);
        }

        [TestMethod]
        public void TheCorpseRevealsTheTimedObjectiveAndABombNeedsItsDisarmedState()
        {
            Use(_corpse);
            Assert.AreEqual(MissionObjectiveState.Completed, Progress.Objectives[3]);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Progress.Objectives[1]);
            Assert.AreEqual(126_000L, SavedObjective(1).TimerRemainingMs);

            // The wreck is scenery: using it does nothing.
            var wreck = Usable(WreckPlacementId);
            Use(wreck);
            Assert.AreEqual((UseObjectState)31, wreck.StateId);

            _bomb.StateId = (UseObjectState)115;
            Use(_bomb);
            Assert.AreEqual((UseObjectState)115, _bomb.StateId);
            Assert.AreEqual(0L, _bomb.FuseAt);
            Assert.IsFalse(SavedObjective(1).TimerDisarmed);
        }

        [TestMethod]
        public void PlantDisarmsTheTimerAndDetonationCompletesTheObjectiveWithItsRulesOnce()
        {
            Use(_corpse);
            Drain();

            Use(_bomb);
            Assert.AreEqual((UseObjectState)114, _bomb.StateId);
            Assert.AreEqual(_tick + FuseMs, _bomb.FuseAt);
            Assert.IsTrue(Progress.Timers[1].Disarmed);
            Assert.IsTrue(SavedObjective(1).TimerDisarmed);
            CollectionAssert.AreEquivalent(new Dictionary<string, int> { { "bootcamp.bomb_planted", 1 } }, SavedFacts());
            Assert.AreEqual(1, _client.Player.ContentFacts[(ContextId, "bootcamp.bomb_planted")]);
            Drain();   // ForceState goes to the clients in the bomb's cells

            // The countdown still reaches its end on the client; nothing fails.
            _nowMs += (LimitSeconds + 10) * 1000L;
            _tick += FuseMs - 1;
            Tick();
            Assert.AreEqual((UseObjectState)114, _bomb.StateId);
            Assert.AreEqual(MissionObjectiveState.Incomplete, Progress.Objectives[1]);
            Assert.IsFalse(Drain().OfType<ObjectiveFailedPacket>().Any());

            // The fuse longer than the time that was left still completes the objective at the detonation.
            _tick += 1;
            Tick();
            Assert.AreEqual((UseObjectState)115, _bomb.StateId);
            Assert.AreEqual(MissionObjectiveState.Completed, Progress.Objectives[1]);
            Assert.AreEqual((UseObjectState)91, Usable(WreckPlacementId).StateId, "the wreck bursts open");
            Assert.AreEqual((uint)MissionObjectiveState.Completed, SavedObjective(1).Status);
            Assert.IsNull(SavedObjective(1).TimerAnchorMs);
            CollectionAssert.AreEquivalent(new Dictionary<string, int> { { "bootcamp.dropship_destroyed", 1 } }, SavedFacts());
            Assert.AreEqual(1, Drain().OfType<ObjectiveCompletedPacket>().Count());

            _tick += 60_000;
            Tick();
            Assert.IsFalse(Drain().OfType<ObjectiveCompletedPacket>().Any());
        }

        [TestMethod]
        public void APlantRecoveredInTheDeadlineTickBeatsTheExpiry()
        {
            Use(_corpse);
            _nowMs += LimitSeconds * 1000L;     // the deadline itself

            Use(_bomb);                          // ActorActionManager runs before the expiry step
            Tick();

            Assert.AreEqual(MissionObjectiveState.Incomplete, Progress.Objectives[1]);
            Assert.AreEqual(MissionState.Active, Progress.State);
            Assert.AreEqual((UseObjectState)114, _bomb.StateId);
        }

        [TestMethod]
        public void WithoutAPlantTheTimerFailsTheObjectiveAndTheBombStaysDisarmed()
        {
            Use(_corpse);
            _nowMs += LimitSeconds * 1000L;
            Tick();

            Assert.AreEqual(MissionObjectiveState.Failed, Progress.Objectives[1]);
            Assert.AreEqual((UseObjectState)113, _bomb.StateId);
            Assert.AreEqual(1, Drain().OfType<ObjectiveFailedPacket>().Count());
        }
    }
}
