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
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Party.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.World;
using Rasa.Repositories.UnitOfWork;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Test.Reconstruction;
using Rasa.Migrations.WildernessData;

namespace Rasa.Test
{
    /// <summary>
    /// Per-squad instances of the final client's MISSIONCONTEXT maps and numbered copies of shared maps
    /// (docs/retail-accuracy.md, 2026-09-27 instancing): creation, isolation, squad joining, the documented
    /// invite quirk, exits, destruction, the instance chooser and the packets the client unpacks.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadInstanceTests
    {
        private const uint Wilderness = 1220;
        private const uint Pravus = 1430;
        private const uint Bootcamp = 1985;

        private SqliteConnection _connection;
        private MapChannelManager _maps;
        private MapChannel _wilderness;
        private Client _client;
        private long _now;
        private object _previousCharacterManager;
        private object _previousMaps;
        private Logger.LoggerConfig _previousLoggerConfig;
        private readonly List<Creature> _creatures = new();

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
            public TestFactory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar()
            {
                var context = Context(_connection);
                return new CharUnitOfWork(context, null, null, new CharacterRepository(context),
                    null, null, null, null, null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null);
            }
            public IWorldUnitOfWork CreateWorld() => throw new InvalidOperationException();
        }

        private static readonly FieldInfo MapsField = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo CharactersField = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

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
                context.CharacterEntries.Add(new CharacterEntry { Id = 101, AccountId = 10, Slot = 1, Name = "First" });
                context.SaveChanges();
            }

            var factory = new TestFactory(_connection);
            _previousCharacterManager = CharactersField.GetValue(null);
            CharactersField.SetValue(null, new CharacterManager(factory));

            _maps = new MapChannelManager(factory, () => _now)
            {
                IsPerCharacterContext = contextId => contextId == Bootcamp,
                IsSquadContext = contextId => contextId == Pravus,
                PopulateInstance = Populate,
                PopulateContextCopy = Populate
            };
            _previousMaps = MapsField.GetValue(null);
            MapsField.SetValue(null, _maps);

            _wilderness = new MapChannel { MapInfo = new MapInfo(Wilderness, "adv_foreas_concordia_wilderness", 1556, 0), ClientList = new List<Client>() };
            _maps.MapChannelArray.Add(Wilderness, _wilderness);
            _maps.MapChannelArray.Add(Pravus, new MapChannel { MapInfo = new MapInfo(Pravus, "adv_foreas_concordia_wilderness_pravusresearch", 555, 0), ClientList = new List<Client>() });
            _maps.MapChannelArray.Add(Bootcamp, new MapChannel { MapInfo = new MapInfo(Bootcamp, "adv_bootcamp", 783, 4), ClientList = new List<Client>() });

            _client = new Client(factory, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(_client, new GameAccountEntry { Id = 10 });
            _client.Player.Id = 101;
            _client.Player.MapChannel = _wilderness;
            _client.Player.MapContextId = Wilderness;
            _client.Player.Position = new Vector3(-100, 200, 300);
            _client.Player.Rotation = 1.5;
            _client.Player.LoginTime = DateTime.Now;
            _wilderness.ClientList.Add(_client);
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }

            MapsField.SetValue(null, _previousMaps);
            CharactersField.SetValue(null, _previousCharacterManager);
            _connection.Dispose();
        }

        private void Populate(MapChannel channel)
        {
            var creature = new Creature { MapContextId = channel.MapInfo.MapContextId, Position = new Vector3(10, 0, 10), Npc = new Npc() };
            _creatures.Add(creature);
            CellManager.Instance.AddToWorld(channel, creature);
        }

        private List<ServerPythonPacket> Sent()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage { Packet: ServerPythonPacket packet })
                    packets.Add(packet);
            return packets;
        }

        private static byte[] Serialize(ServerPythonPacket packet)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            return stream.ToArray();
        }

        /// <summary>Puts the test client inside a channel as MapLoaded would have left it.</summary>
        private void Arrive()
        {
            _client.State = ClientState.Ingame;
            Sent();
        }

        [TestMethod]
        public void EachSquadGetsOneCopyAndAPlayerOutsideASquadGetsTheirOwn()
        {
            var first = _maps.ChannelForEntry(101, Pravus, 7);
            var squadmate = _maps.ChannelForEntry(102, Pravus, 7);
            var otherSquad = _maps.ChannelForEntry(103, Pravus, 8);
            var solo = _maps.ChannelForEntry(104, Pravus);

            Assert.AreSame(first, squadmate, "a squadmate joins the squad's copy");
            Assert.AreNotSame(first, otherSquad);
            Assert.AreNotSame(first, solo);
            Assert.AreNotSame(_maps.MapChannelArray[Pravus], first, "the primary channel is only the template");
            Assert.IsTrue(first.IsSquadInstance && first.IsInstance && !first.IsPrivateInstance);
            Assert.AreEqual(7u, first.OwnerPartyId);
            Assert.AreEqual(104u, solo.OwnerSoloCharacterId);
            Assert.IsNull(solo.OwnerPartyId);
            CollectionAssert.AreEqual(new uint[] { 1, 2, 3 }, new[] { first.Ordinal, otherSquad.Ordinal, solo.Ordinal });
            Assert.AreEqual(MapChannel.InstanceMapIdBase + first.InstanceId, first.MapInstanceId);
            Assert.AreSame(first, _maps.ChannelByMapInstanceId(first.MapInstanceId));
            Assert.AreSame(_wilderness, _maps.ChannelByMapInstanceId(Wilderness));
            Assert.AreEqual(3, _creatures.Count, "each copy is populated once");

            // Shared and per-character contexts keep their rules.
            Assert.AreSame(_wilderness, _maps.ChannelForEntry(101, Wilderness, 7));
            var camp = _maps.ChannelForEntry(101, Bootcamp, 7);
            Assert.IsTrue(camp.IsPrivateInstance && !camp.IsSquadInstance);
            Assert.AreEqual(camp.InstanceId, camp.Ordinal, "OD-2: the boot camp keeps its monotonic id on the loading screen");
        }

        [TestMethod]
        public void ACopyIsGivenItsOwnCopyOfEverythingItsContextWasLoadedWith()
        {
            var maps = new MapChannelManager(null, () => _now) { IsSquadContext = contextId => contextId == Pravus };
            var primary = new MapChannel { MapInfo = new MapInfo(Pravus, "adv_foreas_concordia_wilderness_pravusresearch", 555, 0), ClientList = new List<Client>() };
            maps.MapChannelArray.Add(Pravus, primary);

            var waypoint = new WaypointInfo(900, false, WaypointType.LocalTeleporter);
            primary.Teleporters.Add(900, new DynamicObject { MapContextId = Pravus, Position = new Vector3(1, 2, 3), ObjectData = waypoint, DynamicObjectType = DynamicObjectType.LocalTeleporter });
            primary.DynamicObjects.Add(new Logos(new Structures.World.LogosEntry { Id = 23, ClassId = 7302, MapContextId = Pravus, Name = "Power" }));
            var pool = new SpawnPool { DbId = 99_999_901, MapContextId = Pravus, RespawnTime = 5000, SpawnSlot = new List<SpawnPoolSlot>(), AliveCreatures = 3 };
            SpawnPoolManager.Instance.LoadedSpawnPools.Add(pool.DbId, pool);

            try
            {
                var copy = maps.ChannelForEntry(101, Pravus, 7);

                var pad = copy.Teleporters[900];
                Assert.AreNotSame(primary.Teleporters[900], pad);
                Assert.AreNotEqual(primary.Teleporters[900].EntityId, pad.EntityId, "its own entity");
                Assert.AreSame(waypoint, pad.ObjectData);
                Assert.AreEqual((new Vector3(1, 2, 3), DynamicObjectType.LocalTeleporter), (pad.Position, pad.DynamicObjectType));

                var shrine = copy.DynamicObjects.OfType<Logos>().Single();
                Assert.AreNotSame(primary.DynamicObjects[0], shrine);
                Assert.AreEqual((23u, "Power"), (shrine.Id, shrine.Name));

                var ownPool = copy.SpawnPools.Single();
                Assert.AreNotSame(pool, ownPool);
                Assert.AreSame(copy, ownPool.MapChannel);
                Assert.AreEqual((pool.DbId, 0, 5000L), (ownPool.DbId, ownPool.AliveCreatures, ownPool.UpdateTimer), "fresh counters, due to spawn");

                maps.ReleaseInstanceIfEmpty(copy);
                maps.SquadRetired(7);
                maps.DestroyQueuedInstances();
                Assert.IsFalse(maps.InstanceChannels.ContainsKey(copy.InstanceId));
                Assert.AreEqual(0, copy.Teleporters.Count + copy.DynamicObjects.Count + copy.SpawnPools.Count);
                Assert.AreEqual(1, primary.Teleporters.Count, "the context's own objects stay");
            }
            finally
            {
                SpawnPoolManager.Instance.LoadedSpawnPools.Remove(pool.DbId);
            }
        }

        [TestMethod]
        public void CopiesAreIsolatedAtTheSameSpot()
        {
            var first = _maps.ChannelForEntry(101, Pravus, 7);
            var second = _maps.ChannelForEntry(103, Pravus, 8);
            var player = new Manifestation { MapChannel = first, MapContextId = Pravus, Position = new Vector3(10, 0, 10) };

            Assert.IsTrue(MapChannelManager.IsOnChannel(_creatures[0], first));
            Assert.IsFalse(MapChannelManager.IsOnChannel(_creatures[1], first));
            Assert.IsTrue(MissionManager.IsInConversationRange(player, _creatures[0]));
            Assert.IsFalse(MissionManager.IsInConversationRange(player, _creatures[1]));
            Assert.AreSame(second, MapChannelManager.ChannelOf(_creatures[1]));
        }

        [TestMethod]
        public void ASoloCopyStaysWithItsCreatorAfterASquadFormsAsTheLiveKnownIssueDescribes()
        {
            // D10/D13 known issue: the leader invites while inside; the invitee is not placed with him, and he joins
            // the invitee only after leaving and re-entering.
            var leaderAlone = _maps.ChannelForEntry(101, Pravus);
            var invitee = _maps.ChannelForEntry(102, Pravus, 7);
            Assert.AreNotSame(leaderAlone, invitee);

            var leaderAgain = _maps.ChannelForEntry(101, Pravus, 7);
            Assert.AreSame(invitee, leaderAgain);
        }

        [TestMethod]
        public void AnEmptiedCopyWaitsOutTheLingerThenGoesWithItsCreatures()
        {
            _maps.SquadInstanceEmptyLingerMs = 600_000;
            var copy = _maps.ChannelForEntry(101, Pravus, 7);
            var creature = _creatures.Single();

            _now = 1_000;
            _maps.ReleaseInstanceIfEmpty(copy);
            _now = 500_000;
            _maps.DestroyQueuedInstances();
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(copy.InstanceId), "re-enterable before it resets");

            // Re-entering in time finds the same copy, with what was there still there.
            Assert.AreSame(copy, _maps.ChannelForEntry(102, Pravus, 7));
            Assert.AreEqual(0, copy.EmptySince);
            _now = 2_000_000;
            _maps.DestroyQueuedInstances();
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(copy.InstanceId), "taken off the queue by the re-entry");

            _maps.ReleaseInstanceIfEmpty(copy);
            _now += 600_000;
            _maps.DestroyQueuedInstances();
            Assert.IsFalse(_maps.InstanceChannels.ContainsKey(copy.InstanceId));
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(creature.EntityId));
            Assert.AreNotSame(copy, _maps.ChannelForEntry(101, Pravus, 7), "after the reset a new copy");
        }

        [TestMethod]
        public void ADisbandedSquadsCopyIsNotFoundByARecycledIdAndGoesOnceEmpty()
        {
            var copy = _maps.ChannelForEntry(101, Pravus, 7);
            _maps.SquadDisbanded(7, new List<Client>());

            Assert.IsNull(copy.OwnerPartyId);
            Assert.AreNotSame(copy, _maps.ChannelForEntry(105, Pravus, 7), "squad ids are recycled");

            _maps.ReleaseInstanceIfEmpty(copy);
            _maps.DestroyQueuedInstances();
            Assert.IsFalse(_maps.InstanceChannels.ContainsKey(copy.InstanceId), "nobody can re-enter it, so no linger");
        }

        [TestMethod]
        public void EnteringACopyRemembersTheWayBackAndLeavingTheSquadSendsThePlayerThere()
        {
            Assert.IsTrue(_maps.ChangeMap(_client, Pravus, new Vector3(1, 2, 3), 0.5f));
            var copy = _client.Player.MapChannel;
            Assert.IsTrue(copy.IsSquadInstance);
            Assert.AreEqual(101u, copy.OwnerSoloCharacterId, "not in a squad: the character's own copy");
            Assert.AreEqual((Wilderness, new Vector3(-100, 200, 300), 1.5f), _client.Player.InstanceReturn);

            var wonkavate = Sent().OfType<WonkavatePacket>().Single();
            Assert.AreEqual(Pravus, wonkavate.MapContextId);
            Assert.AreEqual(copy.Ordinal, wonkavate.MapInstanceId, "the loading screen shows Name(ordinal)");

            Arrive();
            // Only a copy of the squad the player left sends them out.
            _maps.SquadMemberRemoved(_client, 9);
            Assert.AreSame(copy, _client.Player.MapChannel);

            copy.OwnerSoloCharacterId = null;
            copy.OwnerPartyId = 9;
            _maps.SquadMemberRemoved(_client, 9);
            Assert.AreSame(_wilderness, _client.Player.MapChannel);
            Assert.AreEqual(new Vector3(-100, 200, 300), _client.Player.Position);
            Assert.IsNull(_client.Player.InstanceReturn);
            var sent = Sent();
            Assert.AreEqual(1, sent.OfType<WonkavatePacket>().Count(packet => packet.MapContextId == Wilderness));
            Assert.AreEqual(PlayerMessage.PmBootedFromMap, sent.OfType<DisplayClientMessagePacket>().Single().MsgId);

            // Empty now, and its squad can still come back to it for a while.
            Assert.IsTrue(copy.EmptySince > 0 || _now == 0);
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(copy.InstanceId));
        }

        [TestMethod]
        public void TheWaypointWindowInsideACopyOffersOnlyLeaveCurrentAdventure()
        {
            Assert.IsTrue(_maps.ChangeMap(_client, Pravus, new Vector3(1, 2, 3), 0));
            Arrive();
            var copy = _client.Player.MapChannel;

            var list = DynamicObjectManager.Instance.CreateListOfWaypoints(_client, WaypointType.Waypoint);
            var entry = list.Single().Value;
            Assert.AreEqual(Pravus, entry.GameGontextId);
            Assert.IsNull(entry.Waypoints, "None: the client lists PM 315 'Leave current adventure' as waypoint 0");
            var row = entry.MapInstanceList.Single();
            Assert.AreEqual(copy.Ordinal, row.Ordinal);
            Assert.AreEqual(copy.MapInstanceId, row.MapId);

            // (currentMapId, gameContextId, [(ctx, [(ordinal, mapId, status)], None)], None, type, current waypoint)
            var packet = new EnteredWaypointPacket(copy.MapInstanceId, Pravus, list, WaypointType.Waypoint, 77);
            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Serialize(packet)))))
            {
                Assert.AreEqual(6, reader.ReadTuple());
                Assert.AreEqual(copy.MapInstanceId, reader.ReadUInt());
                Assert.AreEqual(Pravus, reader.ReadUInt());
                Assert.AreEqual(1, reader.ReadList());
                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(Pravus, reader.ReadUInt());
                Assert.AreEqual(1, reader.ReadList());
                Assert.AreEqual(3, reader.ReadTuple());
                Assert.AreEqual(copy.Ordinal, reader.ReadUInt());
                Assert.AreEqual(copy.MapInstanceId, reader.ReadUInt());
                Assert.AreEqual((uint)MapInstanceStatus.Low, reader.ReadUInt());
                reader.ReadNoneStruct();
                reader.ReadNoneStruct();
                Assert.AreEqual((int)WaypointType.Waypoint, reader.ReadInt());
                Assert.AreEqual(77u, reader.ReadUInt());
            }

            // SelectWaypoint(mapId, 0) from the pad takes the player back out; away from a pad it does nothing.
            var abort = new SelectWaypointPacket { MapInstanceId = copy.MapInstanceId, WaypointId = 0 };
            DynamicObjectManager.Instance.SelectWaypoint(_client, abort);
            Assert.AreSame(copy, _client.Player.MapChannel);

            var pad = new DynamicObject { MapContextId = Pravus, ObjectData = new WaypointInfo(900, false, WaypointType.Waypoint) };
            pad.TriggeredByPlayers.Add(_client);
            copy.Teleporters.Add(900, pad);
            DynamicObjectManager.Instance.SelectWaypoint(_client, abort);
            Assert.AreSame(_wilderness, _client.Player.MapChannel);
            Assert.IsFalse(Sent().OfType<DisplayClientMessagePacket>().Any(), "a chosen exit is not a squad removal");
        }

        [TestMethod]
        public void AFullSharedMapOpensANumberedCopyAndTheClientChoosesBetweenThem()
        {
            const uint Divide = 1148;
            var divide = new MapChannel { MapInfo = new MapInfo(Divide, "adv_foreas_concordia_divide", 1584, 10), ClientList = new List<Client>(), PlayerLimit = 128 };
            _maps.MapChannelArray.Add(Divide, divide);

            // No capacity (the default, OD-127): one channel and no chooser.
            Assert.AreEqual(1, _maps.EntryCandidates(Divide).Count);

            _maps.SharedCopyCapacity = contextId => contextId == Divide ? 1 : null;
            divide.ClientList.Add(new Client(null, new ClientPacketHandler()));
            Assert.AreEqual(MapInstanceStatus.Full, _maps.StatusOf(divide));

            Assert.IsTrue(_maps.ChangeMap(_client, Divide, new Vector3(4, 5, 6), 0));
            Assert.AreSame(_wilderness, _client.Player.MapChannel, "the move waits for the choice");
            Assert.AreEqual((Divide, new Vector3(4, 5, 6), 0f), _client.PendingInstanceChoice);
            var copy = _maps.CopiesOf(Divide).Last();
            Assert.IsTrue(copy.IsSharedCopy);
            Assert.AreEqual(2u, copy.Ordinal);

            var choose = Sent().OfType<ChooseInstanceListPacket>().Single();
            Assert.AreEqual(685, (int)choose.Opcode);
            using (var reader = new PythonReader(new BinaryReader(new MemoryStream(Serialize(choose)))))
            {
                // ([(ordinal, instanceId, mapTemplateId, startGroup, overloadedStatus), ...],)
                Assert.AreEqual(1, reader.ReadTuple());
                Assert.AreEqual(2, reader.ReadList());
                Assert.AreEqual(5, reader.ReadTuple());
                Assert.AreEqual(1u, reader.ReadUInt());
                Assert.AreEqual(Divide, reader.ReadUInt());
                Assert.AreEqual(1306u, reader.ReadUInt(), "gamecontext.pyo: 1148 is map template 1306");
                reader.ReadNoneStruct();
                Assert.AreEqual((uint)MapInstanceStatus.Full, reader.ReadUInt());
                Assert.AreEqual(5, reader.ReadTuple());
                Assert.AreEqual(2u, reader.ReadUInt());
                Assert.AreEqual(copy.MapInstanceId, reader.ReadUInt());
                Assert.AreEqual(1306u, reader.ReadUInt());
                reader.ReadNoneStruct();
                Assert.AreEqual((uint)MapInstanceStatus.Low, reader.ReadUInt());
            }

            // Choosing the full one is answered with PM 934; the choice is spent and the player stays.
            _maps.SelectInstance(_client, Divide);
            Assert.AreSame(_wilderness, _client.Player.MapChannel);
            Assert.AreEqual(PlayerMessage.PmYouCannotGoToThatMapAtThisTimeRetry, Sent().OfType<DisplayClientMessagePacket>().Single().MsgId);

            // Cancelling drops a pending choice.
            Assert.IsTrue(_maps.ChangeMap(_client, Divide, new Vector3(4, 5, 6), 0));
            _maps.SelectInstanceCancel(_client);
            Assert.IsNull(_client.PendingInstanceChoice);
            _maps.SelectInstance(_client, copy.MapInstanceId);
            Assert.AreSame(_wilderness, _client.Player.MapChannel, "nothing pending, nothing to select");

            // Choosing the open copy goes there, and the loading screen reads "(2)".
            Assert.IsTrue(_maps.ChangeMap(_client, Divide, new Vector3(4, 5, 6), 0));
            Sent();
            _maps.SelectInstance(_client, copy.MapInstanceId);
            Assert.AreSame(copy, _client.Player.MapChannel);
            Assert.AreEqual(2u, Sent().OfType<WonkavatePacket>().Single().MapInstanceId);

            // A copy nobody is in goes at once.
            Arrive();
            Assert.IsTrue(_maps.ChangeMap(_client, Wilderness, new Vector3(0, 0, 0), 0));
            _maps.DestroyQueuedInstances();
            Assert.IsFalse(_maps.InstanceChannels.ContainsKey(copy.InstanceId));
        }

        [TestMethod]
        public void TheChooserPacketsReadAsTheClientSendsThem()
        {
            foreach (var (startGroup, expected) in new[] { ((object)null, (string)null), ("default", "default") })
            {
                using var stream = new MemoryStream();
                using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                {
                    writer.WriteTuple(2);
                    writer.WriteUInt(MapChannel.InstanceMapIdBase + 5);
                    if (startGroup == null)
                        writer.WriteNoneStruct();
                    else
                        writer.WriteString((string)startGroup);
                }

                stream.Position = 0;
                var packet = new SelectInstancePacket();
                using var reader = new PythonReader(new BinaryReader(stream));
                packet.Read(reader);
                Assert.AreEqual(687, (int)packet.Opcode);
                Assert.AreEqual(MapChannel.InstanceMapIdBase + 5, packet.MapInstanceId);
                Assert.AreEqual(expected, packet.StartGroup);
                Assert.AreEqual(stream.Length, stream.Position);
            }

            Assert.AreEqual(688, (int)new SelectInstanceCancelPacket().Opcode);
        }

        [TestMethod]
        public void SquadMemberListTellsTheClientWhenItsMapIsSquadExclusive()
        {
            foreach (var exclusive in new[] { false, true })
            {
                var packet = new SquadMemberListPacket(new List<(uint, ulong)> { (10, 5000) }, exclusive);
                using var reader = new PythonReader(new BinaryReader(new MemoryStream(Serialize(packet))));
                Assert.AreEqual(2, reader.ReadTuple());
                Assert.AreEqual(1, reader.ReadList());
                Assert.AreEqual(2, reader.ReadTuple());
                Assert.AreEqual(10u, reader.ReadUInt());
                Assert.AreEqual(5000ul, reader.ReadULong());
                Assert.AreEqual(exclusive, reader.ReadBool());
            }
        }

        [TestMethod]
        public void EverySquadContextRowIsInTheManifestWithTheDecisionsAndGapsItRestsOn()
        {
            var rows = MissionContextSquadInstancingRows.Rows;
            Assert.AreEqual(53, rows.Length);
            Assert.AreEqual(rows.Length, rows.Select(row => row.MapContextId).Distinct().Count());
            Assert.IsFalse(rows.Any(row => row.MapContextId is 1985 or 2375 or 1220 or 1148 or 1244), "boot camp, the endgame shared map and battlefields stay");
            Assert.IsTrue(rows.All(row => row.Comment.Length <= 50), "content_map_setting.comment is varchar(50)");
            // Every row's context has a client map template (it is a final-client context).
            Assert.IsTrue(rows.All(row => ClientMapTemplates.ByContext.ContainsKey(row.MapContextId)));

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = manifest.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == MissionContextSquadInstancingRows.Migration).ToList();
            Assert.AreEqual(rows.Length, evidence.Count, "every inserted row needs exactly one manifest row");

            foreach (var row in rows)
            {
                var entry = evidence.Single(e => e.GetProperty("key").GetProperty("map_context_id").GetUInt32() == row.MapContextId);
                var instancing = entry.GetProperty("fields").GetProperty("instancing");
                Assert.AreEqual(MissionContextSquadInstancingRows.PerSquad, instancing.GetProperty("value").GetByte());
                Assert.AreEqual("original", instancing.GetProperty("tier").GetString());
                StringAssert.Contains(instancing.GetProperty("citations")[0].GetProperty("locator").GetString(), "5 MISSIONCONTEXT");
                Assert.AreEqual(row.Comment, entry.GetProperty("storage_fields").GetProperty("comment").GetProperty("value").GetString());
            }

            var decisions = root.GetProperty("owner_decisions").EnumerateArray().ToDictionary(d => d.GetProperty("id").GetString());
            foreach (var id in new[] { "OD-125", "OD-126", "OD-127", "OD-128", "OD-129" })
                Assert.AreEqual("approved-by-agent-pending-owner-review", decisions[id].GetProperty("review_status").GetString(), id);
            var gaps = root.GetProperty("gaps").EnumerateArray().Select(g => g.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-INSTANCE-RESET-TIMER", "GAP-SHARED-COPY-CAPACITY", "GAP-SQUAD-INVITE-QUIRK-FINAL-STATE",
                         "GAP-LAST-STAND-COPIES", "GAP-INSTANCE-MISSION-RESET", "GAP-INSTANCE-DEATH-EXIT", "GAP-INSTANCE-START-GROUPS" })
                Assert.IsTrue(gaps.Contains(gap), gap);

            // The defaults the decisions describe are the ones the server runs with.
            var maps = new MapChannelManager(null);
            Assert.AreEqual(600_000L, maps.SquadInstanceEmptyLingerMs);
            Assert.IsNull(maps.SharedCopyCapacity(2375));
        }
    }
}
