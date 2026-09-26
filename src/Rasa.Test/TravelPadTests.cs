using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.CharacterTeleporter;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// Local teleporter pads and dropship pads (2026-09-26 client defects, SEG3-LOCAL-TELEPORTERS and
    /// SEG3-DROPSHIP-WINDOW). The client contract: constant/waypointtype LOCALWAYPOINT 1, MAPWAYPOINT 2, WORMHOLE 3;
    /// manifestation.Recv_WaypointGained posts PM_GAINED_WAYPOINT for type 1; clientmethod.Recv_EnteredWaypoint
    /// hands the list and type to waypointwindow.ShowWaypoints, whose SetupWaypointLocationRows places each location
    /// at the position sent with it. Help text 5697 (uielementlanguage): dropships are gained by walking across the
    /// pad, and the dropship menu lists the available (gained) ones.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TravelPadTests
    {
        private const uint CharacterId = 101;
        private const uint DevilsDen = 1394;
        private const uint Wilderness = 1220;
        private const uint Palisades = 1244;

        public class TeleporterRecorder : DispatchProxy
        {
            public List<CharacterTeleporterEntry> Added;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name == "Add") { Added.Add((CharacterTeleporterEntry)args[0]); return null; }
                throw new NotSupportedException(method.Name);
            }
        }

        public class UnitProxy : DispatchProxy
        {
            public ICharacterTeleporterRepository Teleporters;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_CharacterTeleporters": return Teleporters;
                    case "Complete":
                    case "Dispose": return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            public readonly List<CharacterTeleporterEntry> Added = new();
            public ICharUnitOfWork CreateChar()
            {
                var teleporters = DispatchProxy.Create<ICharacterTeleporterRepository, TeleporterRecorder>();
                ((TeleporterRecorder)(object)teleporters).Added = Added;
                var unit = DispatchProxy.Create<ICharUnitOfWork, UnitProxy>();
                ((UnitProxy)(object)unit).Teleporters = teleporters;
                return unit;
            }
            public IWorldUnitOfWork CreateWorld() => throw new NotSupportedException();
        }

        private Factory _factory;
        private CharacterManager _realCharacters;
        private Func<uint, bool> _realPerCharacter;
        private readonly Dictionary<uint, MapChannel> _replacedMaps = new();
        private readonly List<uint> _addedTeleporters = new();
        private Logger.LoggerConfig _oldLogger;

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _factory = new Factory();
            var field = typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            _realCharacters = (CharacterManager)field.GetValue(null);
            field.SetValue(null, Activator.CreateInstance(typeof(CharacterManager), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] { _factory }, null));
            _realPerCharacter = MapChannelManager.Instance.IsPerCharacterContext;
            MapChannelManager.Instance.IsPerCharacterContext = _ => false;
        }

        [TestCleanup]
        public void Cleanup()
        {
            typeof(CharacterManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _realCharacters);
            MapChannelManager.Instance.IsPerCharacterContext = _realPerCharacter;
            foreach (var entry in _replacedMaps)
                if (entry.Value == null)
                    MapChannelManager.Instance.MapChannelArray.Remove(entry.Key);
                else
                    MapChannelManager.Instance.MapChannelArray[entry.Key] = entry.Value;
            _replacedMaps.Clear();
            foreach (var id in _addedTeleporters)
                DynamicObjectManager.Instance.Teleporters.Remove(id);
            _addedTeleporters.Clear();
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
        }

        [TestMethod]
        public void WalkingOntoALocalTeleporterGainsItAndOpensTheLocalWindowForThisMapOnly()
        {
            var map = Map(DevilsDen);
            // World-seed rows 567 and 568 (Devil's Den), and a local pad on another map that must never be listed here.
            var hatchery = Pad(567, WaypointType.LocalTeleporter, DevilsDen, new Vector3(-49.027344f, 94.796875f, -300.10156f));
            var detention = Pad(568, WaypointType.LocalTeleporter, DevilsDen, new Vector3(80.22266f, 105.6093f, 211.5586f));
            Pad(561, WaypointType.LocalTeleporter, 1773, new Vector3(388.58203f, 208f, -91.97656f));
            var client = Player(map, hatchery.Position + new Vector3(0.5f, 0, 0));
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(CharacterId, 561, (byte)WaypointType.LocalTeleporter));

            DynamicObjectManager.Instance.DynamicObjectProximityWorker(map, hatchery, 0);

            // Gained and persisted as type 1, which is the type the client's gain line (PM_GAINED_WAYPOINT) is for.
            var gained = _factory.Added.Single();
            Assert.AreEqual(567u, gained.WaypointId);
            Assert.AreEqual((byte)WaypointType.LocalTeleporter, gained.WaypointType);
            Assert.IsTrue(hatchery.TriggeredByPlayers.Contains(client));

            var messages = Drain(client);
            var gainedPacket = (WaypointGainedPacket)messages.Single(p => p is WaypointGainedPacket);
            Assert.AreEqual(567u, gainedPacket.WaypointId);
            Assert.AreEqual(WaypointType.LocalTeleporter, gainedPacket.WaypointType);

            var entered = (EnteredWaypointPacket)messages.Single(p => p is EnteredWaypointPacket);
            Assert.AreEqual(WaypointType.LocalTeleporter, ReadEntered(entered).Type);
            // Only this map's gained local pads: 568 is not gained yet, 561 is gained but on another map.
            CollectionAssert.AreEqual(new[] { 567u }, ReadEntered(entered).Waypoints.Select(w => w.Id).ToArray());

            // A second tick while still on the pad neither re-gains nor reopens.
            DynamicObjectManager.Instance.DynamicObjectProximityWorker(map, hatchery, 0);
            Assert.AreEqual(1, _factory.Added.Count);
            Assert.AreEqual(0, Drain(client).Count);

            // Walking to the second pad gains it and lists both, at their own positions.
            client.Player.Position = detention.Position;
            DynamicObjectManager.Instance.DynamicObjectProximityWorker(map, hatchery, 0);   // leaves the first pad
            DynamicObjectManager.Instance.DynamicObjectProximityWorker(map, detention, 0);
            Assert.AreEqual(2, _factory.Added.Count);
            var second = ReadEntered((EnteredWaypointPacket)Drain(client).Single(p => p is EnteredWaypointPacket));
            CollectionAssert.AreEquivalent(new[] { 567u, 568u }, second.Waypoints.Select(w => w.Id).ToArray());
            Assert.AreEqual(detention.Position, second.Waypoints.Single(w => w.Id == 568).Position);
        }

        [TestMethod]
        public void TheDropshipWindowListsOnlyGainedPadsEachAtItsOwnPosition()
        {
            var map = Map(Wilderness);
            // World-seed dropship rows 267 (Twin Pillars) and 250 (Cumbria Research Facility).
            var twinPillars = new Vector3(-60f, 221.269f, -471f);
            var cumbria = new Vector3(-870.14453f, 139.98828f, 656.1367f);
            Pad(267, WaypointType.Dropship, Wilderness, twinPillars);
            Pad(250, WaypointType.Dropship, Palisades, cumbria);
            var client = Player(map, twinPillars);
            var trigger = new MapTrigger(267, "Dropship Transport: Twin Pillars", twinPillars, 0, Wilderness);

            MapTriggerManager.Instance.PlayerEnterTriggerRange(client, trigger);

            // Walking onto the pad gains it, silently: the client has no dropship waypoint type to announce.
            var gained = _factory.Added.Single();
            Assert.AreEqual(267u, gained.WaypointId);
            Assert.AreEqual((byte)WaypointType.Dropship, gained.WaypointType);
            var messages = Drain(client);
            Assert.IsFalse(messages.Any(p => p is WaypointGainedPacket));

            // The Palisades pad has not been gained, so only the pad underfoot is listed - at its own position, not
            // the (-225.353, 99.597, -70.5246) every map's first pad used to be drawn at.
            var list = ReadEntered((EnteredWaypointPacket)messages.Single(p => p is EnteredWaypointPacket));
            Assert.AreEqual(WaypointType.Dropship, list.Type);
            CollectionAssert.AreEqual(new[] { Wilderness }, list.Maps.ToArray());
            Assert.AreEqual(twinPillars, list.Waypoints.Single().Position);

            // Once the Palisades pad has been walked across, both maps are offered, each pad where it stands.
            client.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(CharacterId, 250, (byte)WaypointType.Dropship));
            client.Player.Position = twinPillars + new Vector3(10, 0, 0);
            MapTriggerManager.Instance.PlayerExitTriggerRange(client, trigger);
            client.Player.Position = twinPillars;
            Drain(client);
            MapTriggerManager.Instance.PlayerEnterTriggerRange(client, trigger);
            Assert.AreEqual(1, _factory.Added.Count);
            var both = ReadEntered((EnteredWaypointPacket)Drain(client).Single(p => p is EnteredWaypointPacket));
            CollectionAssert.AreEquivalent(new[] { Wilderness, Palisades }, both.Maps.ToArray());
            Assert.AreEqual(cumbria, both.Waypoints.Single(w => w.Id == 250).Position);
            Assert.AreEqual(twinPillars, both.Waypoints.Single(w => w.Id == 267).Position);
        }

        [TestMethod]
        public void GainedChecksCompareTheWaypointKind()
        {
            var player = new Manifestation();
            player.GainedWaypoints.Add(new CharacterTeleporterEntry(CharacterId, 267, (byte)WaypointType.Dropship));
            Assert.IsTrue(DynamicObjectManager.HasGained(player, 267, WaypointType.Dropship));
            Assert.IsFalse(DynamicObjectManager.HasGained(player, 267, WaypointType.Waypoint));
            Assert.IsFalse(DynamicObjectManager.HasGained(player, 250, WaypointType.Dropship));
        }

        private MapChannel Map(uint contextId)
        {
            var map = new MapChannel { MapInfo = new MapInfo(contextId, "fixture", 1, 0), ClientList = new List<Client>() };
            if (!_replacedMaps.ContainsKey(contextId))
            {
                MapChannelManager.Instance.MapChannelArray.TryGetValue(contextId, out var previous);
                _replacedMaps[contextId] = previous;
            }
            MapChannelManager.Instance.MapChannelArray[contextId] = map;
            return map;
        }

        private DynamicObject Pad(uint id, WaypointType type, uint contextId, Vector3 position)
        {
            var pad = new DynamicObject
            {
                Position = position,
                MapContextId = contextId,
                ObjectData = new WaypointInfo(id, false, type),
                DynamicObjectType = type == WaypointType.LocalTeleporter ? DynamicObjectType.LocalTeleporter : 0
            };
            DynamicObjectManager.Instance.Teleporters[id] = pad;
            _addedTeleporters.Add(id);
            if (MapChannelManager.Instance.MapChannelArray.TryGetValue(contextId, out var map) && _replacedMaps.ContainsKey(contextId))
                map.Teleporters[id] = pad;
            return pad;
        }

        private static Client Player(MapChannel map, Vector3 position)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player = new Manifestation
            {
                Id = CharacterId, MapChannel = map, MapContextId = map.MapInfo.MapContextId, Position = position,
                Cells = new uint[,] { { 0 } }
            };
            map.ClientList.Add(client);
            // The proximity worker reads the clients of the pad's cell; every pad of a fixture shares the player's.
            foreach (var pad in DynamicObjectManager.Instance.Teleporters.Values.Where(p => p.MapContextId == map.MapInfo.MapContextId))
            {
                var seed = CellManager.Instance.GetCellSeed(pad.Position);
                if (!map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    map.MapCellInfo.Cells[seed] = cell = new MapCell { ClientList = new List<Client>() };
                if (!cell.ClientList.Contains(client))
                    cell.ClientList.Add(client);
            }
            return client;
        }

        private sealed class EnteredList
        {
            public WaypointType Type;
            public List<uint> Maps = new();
            public List<(uint Id, Vector3 Position)> Waypoints = new();
        }

        /// <summary>Reads EnteredWaypoint the way clientmethod.Recv_EnteredWaypoint unpacks it.</summary>
        private static EnteredList ReadEntered(EnteredWaypointPacket packet)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var result = new EnteredList();
            Assert.AreEqual(6, reader.ReadTuple());
            reader.ReadUInt();
            reader.ReadUInt();
            var maps = reader.ReadList();
            for (var m = 0; m < maps; m++)
            {
                Assert.AreEqual(3, reader.ReadTuple());
                result.Maps.Add(reader.ReadUInt());
                var instances = reader.ReadList();
                for (var i = 0; i < instances; i++)
                {
                    Assert.AreEqual(3, reader.ReadTuple());
                    reader.ReadUInt(); reader.ReadUInt(); reader.ReadUInt();
                }
                var waypoints = reader.ReadList();
                for (var w = 0; w < waypoints; w++)
                {
                    Assert.AreEqual(3, reader.ReadTuple());
                    var id = reader.ReadUInt();
                    Assert.AreEqual(3, reader.ReadTuple());
                    var position = new Vector3((float)reader.ReadDouble(), (float)reader.ReadDouble(), (float)reader.ReadDouble());
                    reader.ReadBool();
                    result.Waypoints.Add((id, position));
                }
            }
            reader.ReadNoneStruct();
            result.Type = (WaypointType)reader.ReadInt();
            return result;
        }

        private static List<ServerPythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<ServerPythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call) packets.Add((ServerPythonPacket)call.Packet);
            return packets;
        }
    }
}
