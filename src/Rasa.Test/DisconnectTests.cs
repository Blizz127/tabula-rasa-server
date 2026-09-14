using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.Game.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class DisconnectTests
    {
        private SqliteConnection _connection;
        private TestFactory _factory;
        private MapChannelManager _maps;
        private MapChannel _map;
        private Client _client;
        private DisconnectedClientQueue _disconnected;
        private List<Client> _clients;
        private object _previousCharacterManager;
        private Logger.LoggerConfig _previousLoggerConfig;
        private long _now;

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

        private sealed class TestFactory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public int OpenedUnits { get; private set; }
            public TestFactory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar()
            {
                OpenedUnits++;
                var context = Context(_connection);
                return new CharUnitOfWork(context, null, null, new CharacterRepository(context),
                    null, null, null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null);
            }
            public IWorldUnitOfWork CreateWorld() => throw new InvalidOperationException();
        }

        [TestInitialize]
        public void Initialize()
        {
            _previousLoggerConfig = Logger.Config;
            if (_previousLoggerConfig == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            using (var context = Context(_connection))
            {
                context.Database.EnsureCreated();
                context.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Name = "One", Email = "one@example.invalid" });
                context.CharacterEntries.Add(new CharacterEntry { Id = 101, AccountId = 10, Slot = 1, Name = "First", CoordX = 5 });
                context.SaveChanges();
            }
            _factory = new TestFactory(_connection);
            var field = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _previousCharacterManager = field.GetValue(null);
            field.SetValue(null, new CharacterManager(_factory));
            _maps = new MapChannelManager(_factory, () => _now);
            _map = new MapChannel { ClientList = new List<Client>() };
            _maps.MapChannelArray.Add(1, _map);
            _map.MapCellInfo.Cells.Add(0, new MapCell());
            _client = new Client(_factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = 10 });
            _client.Player.Id = 101;
            _client.Player.MapChannel = _map;
            _client.Player.Position = new Vector3(10, 20, 30);
            _client.Player.LoginTime = DateTime.Now;
            _client.Player.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 40, 40, 40, 0, 0));
            _client.Player.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0));
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[0].ClientList.Add(_client);
            EntityManager.Instance.RegisterEntity(_client.Player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(_client.Player.EntityId, _client.Player);
            EntityManager.Instance.RegisterActor(_client.Player.EntityId, _client.Player);
            _disconnected = new DisconnectedClientQueue();
            _clients = new List<Client> { _client };
        }

        [TestCleanup]
        public void Cleanup()
        {
            EntityManager.Instance.UnregisterEntity(_client.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(_client.Player.EntityId);
            EntityManager.Instance.UnregisterActor(_client.Player.EntityId);
            typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, _previousCharacterManager);
            _connection.Dispose();
            typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, _previousLoggerConfig);
        }

        [TestMethod]
        public void QuitNowKeepsOriginalDeadlineAndCombatThenSavesOnce()
        {
            _maps.RequestLogout(_client);
            _now = 4000;
            CloseAndEnqueue();
            _disconnected.Process(_clients, _maps);
            AssertRetained();
            Assert.AreEqual(0, _factory.OpenedUnits);
            Assert.AreEqual(5d, ReadPosition());
            Assert.IsTrue(Server.OccupiesAccount(_client, 10));
            Assert.IsFalse(Server.OccupiesAccount(_client, 20));

            var attack = new ActionData(new Actor(), ActionId.WeaponMelee, 1, _client.Player.EntityId, 0L);
            MissileManager.Instance.MissileLaunch(_map, attack, 60);
            _maps.MapChannelWorker(100);
            Assert.AreEqual(0, _client.Player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(80, _client.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(100L, _map.MapChannelElapsed);

            _now = 9999;
            _disconnected.Process(_clients, _maps);
            AssertRetained();
            _client.Player.Position = new Vector3(42, 20, 30);
            _now = 10000;
            _disconnected.Process(_clients, _maps);
            AssertRemoved();
            Assert.IsFalse(Server.OccupiesAccount(_client, 10));
            Assert.AreEqual(42d, ReadPosition());
            Assert.AreEqual(2, _factory.OpenedUnits); // one final snapshot and one existing login-metadata update

            _client.Player.Position = new Vector3(99, 20, 30);
            _disconnected.Enqueue(_client);
            _disconnected.Process(_clients, _maps);
            _maps.RemovePlayer(_client, false);
            Assert.AreEqual(42d, ReadPosition());
            Assert.AreEqual(2, _factory.OpenedUnits);
        }

        [TestMethod]
        public void SocketLossAfterAcceptedNormalLogoutCannotSaveOrRemoveTwice()
        {
            _maps.RequestLogout(_client);
            _now = 10000;
            _maps.CharacterLogout(_client);
            CloseAndEnqueue();
            _maps.MapChannelWorker(100);
            _disconnected.Process(_clients, _maps);
            AssertRemoved();
            Assert.AreEqual(ClientState.Disconnected, _client.State);
            Assert.AreEqual(2, _factory.OpenedUnits);
        }

        [TestMethod]
        public void AbruptLossWithoutPendingRequestPerformsIntendedCleanup()
        {
            _maps.RequestLogout(_client);
            _maps.CancelLogoutRequest(_client);
            CloseAndEnqueue();
            _disconnected.Process(_clients, _maps);
            AssertRemoved();
            Assert.AreEqual(10d, ReadPosition());
        }

        [TestMethod]
        public void DisconnectedInputCannotCancelRetentionOrQueueOutput()
        {
            _maps.RequestLogout(_client);
            CloseAndEnqueue();
            _client.State = ClientState.LoggedIn; // a late handler cannot reopen a closed connection
            Assert.AreEqual(ClientState.Disconnected, _client.State);
            _maps.CancelLogoutRequest(_client);
            _maps.CharacterLogout(_client);
            _maps.RequestLogout(_client);
            _client.CallMethod(SysEntity.ClientMethodId, new LogoutTimeRemainingPacket());
            _client.Update(100);
            Assert.IsNull(PacketQueue().PopOutgoing());
            _disconnected.Process(_clients, _maps);
            AssertRetained();
        }

        [TestMethod]
        public void CharacterSelectionRequestCannotReplaceAnActorAwaitingNormalRemoval()
        {
            _maps.RequestLogout(_client);
            _now = 10000;
            _maps.CharacterLogout(_client);
            var player = _client.Player;
            new CharacterManager(_factory).RequestSwitchToCharacterInSlot(_client,
                new RequestSwitchToCharacterInSlotPacket { SlotNum = 1 });
            Assert.AreSame(player, _client.Player);
            Assert.AreEqual(0, _factory.OpenedUnits);
        }

        [TestMethod]
        public void SocketLossDuringMapLoadingRemovesQueuedAndAdmittedReferences()
        {
            EntityManager.Instance.UnregisterEntity(_client.Player.EntityId);
            EntityManager.Instance.UnregisterPlayer(_client.Player.EntityId);
            EntityManager.Instance.UnregisterActor(_client.Player.EntityId);
            _map.MapCellInfo.Cells.Clear();
            _map.QueuedClients.Enqueue(_client);
            _client.State = ClientState.Loading;
            CloseAndEnqueue();
            _maps.Timer.Add("CellUpdateVisibility", 100, true, null);
            _maps.Timer.Add("CheckForMapTriggers", 100, true, null);
            _maps.MapChannelWorker(100);
            _disconnected.Process(_clients, _maps);
            Assert.AreEqual(0, _map.QueuedClients.Count);
            Assert.AreEqual(0, _map.ClientList.Count);
            Assert.AreEqual(0, _clients.Count);
            Assert.AreEqual(1, _factory.OpenedUnits);
        }

        [TestMethod]
        public void DisconnectBeforeSelectingCharacterDoesNotSaveDefaultId()
        {
            var client = new Client(_factory, new ClientPacketHandler());
            client.Close();
            var clients = new List<Client> { client };
            _disconnected.Enqueue(client);
            _disconnected.Process(clients, _maps);
            Assert.AreEqual(0, clients.Count);
            Assert.AreEqual(0, _factory.OpenedUnits);
            Assert.IsFalse(Server.OccupiesAccount(client, 10));
        }

        [TestMethod]
        public void MapChangeLeavesTheOldMapWithoutEndingTheSession()
        {
            var destination = new MapChannel { MapInfo = new MapInfo(1148, "Destination", 1, 0), ClientList = new List<Client>() };
            _maps.MapChannelArray.Add(1148, destination);
            Assert.IsTrue(_maps.ChangeMap(_client, 1148, new Vector3(1, 2, 3), 0));
            Assert.IsFalse(_map.ClientList.Contains(_client));
            Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(_client.Player.EntityId));
            Assert.AreSame(destination, _client.Player.MapChannel);
            Assert.IsTrue(destination.ClientList.Contains(_client));
            // Disconected is the terminal flag the map triggers, links and weapon checks skip on.
            Assert.IsFalse(_client.Player.Disconected);
            Assert.IsFalse(_client.Player.RemoveFromMap);
            Assert.AreEqual(ClientState.Loading, _client.State);
        }

        private void CloseAndEnqueue()
        {
            _client.Close();
            _client.Close();
            _disconnected.Enqueue(_client);
            _disconnected.Enqueue(_client);
        }

        private double ReadPosition()
        {
            using var context = Context(_connection);
            return context.CharacterEntries.Find(101u).CoordX;
        }

        private PacketQueue PacketQueue()
            => (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);

        private void AssertRetained()
        {
            Assert.IsTrue(EntityManager.Instance.Players.ContainsKey(_client.Player.EntityId));
            Assert.IsTrue(EntityManager.Instance.Actors.ContainsKey(_client.Player.EntityId));
            Assert.IsTrue(_map.MapCellInfo.Cells[0].ClientList.Contains(_client));
            Assert.IsTrue(_map.ClientList.Contains(_client));
            Assert.IsTrue(_clients.Contains(_client));
            Assert.IsFalse(_client.Player.Disconected);
        }

        private void AssertRemoved()
        {
            Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(_client.Player.EntityId));
            Assert.IsFalse(EntityManager.Instance.Actors.ContainsKey(_client.Player.EntityId));
            Assert.IsFalse(_map.MapCellInfo.Cells[0].ClientList.Contains(_client));
            Assert.IsFalse(_map.ClientList.Contains(_client));
            Assert.IsFalse(_clients.Contains(_client));
            Assert.IsTrue(_client.Player.Disconected);
        }
    }
}
