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
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// A teleport is three effects - generated/client/teleportertype.teleporterFX[type] is
    /// (pre, post, arrive) - and the client plays all three only for the actor that receives
    /// Teleport. Actor.Recv_Teleport calls Recv_PostTeleport itself and schedules
    /// Recv_TeleportArrival through _TelportMovementCompleted; everyone else is sent PreTeleport
    /// alone, and the effect _StartPreTeleportFX attached is released by nothing but
    /// _PlayTeleportArrivalFX. (client/augmentations/actor.pyo, first=1953/2033/2052/2114/2162.)
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TeleportSequenceTests
    {
        private static readonly Vector3 Destination = new Vector3(10f, 20f, 30f);

        [DataTestMethod]
        [DataRow(TeleportType.Default)]
        [DataRow(TeleportType.TacticalEvasion)]
        public void OnlyTheOnlookersAreSentTheBeatsTheirClientCannotRunItself(TeleportType teleportType)
        {
            var map = new MapChannel();
            var owner = Player(map);
            var onlooker = Player(map);
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { owner, onlooker } };

            PlayerDeathManager.TeleportWithinMap(owner, Destination, teleportType);

            var mine = Drain(owner);
            var theirs = Drain(onlooker);

            // The owner: the fade, the acknowledgement queue, the teleport - and nothing after it.
            // A second Recv_PostTeleport would find tmp_preTeleportType already deleted and replay
            // the middle beat as DEFAULT.
            CollectionAssert.AreEqual(
                new[] { GameOpcode.PreTeleport, GameOpcode.BeginTeleport, GameOpcode.Teleport },
                Calls(mine).ToArray());

            // The onlookers: the two beats their client will not run for itself, straddling the move
            // so the post-teleport flash plays at the departure point (Recv_PostTeleport plays it at
            // body.GetPosition()) and the arrival, which is what stops the pre-teleport effect, after it.
            CollectionAssert.AreEqual(
                new[] { GameOpcode.PreTeleport, GameOpcode.PostTeleport, GameOpcode.TeleportArrival },
                Calls(theirs).ToArray());
            var move = theirs.FindIndex(message => message is MoveObjectMessage);
            Assert.IsTrue(move > 0);
            Assert.IsTrue(IndexOf(theirs, GameOpcode.PostTeleport) < move);
            Assert.IsTrue(move < IndexOf(theirs, GameOpcode.TeleportArrival));

            // Both ends are told which effect set to use. PreTeleport is what stores
            // tmp_preTeleportType, which Recv_PostTeleport then reads on every screen that got it.
            foreach (var messages in new[] { mine, theirs })
                Assert.AreEqual((uint)teleportType, PreTeleportType(Call(messages, GameOpcode.PreTeleport)));
            Assert.AreEqual((uint)teleportType, TeleportedType(Call(mine, GameOpcode.Teleport)));
        }

        [TestMethod]
        public void AWaypointPadKeepsTheDefaultEffectAndALocalTeleporterIsLabelledAsOne()
        {
            // teleporter.type is a WaypointType, not a teleporterFX key: the FX key was the original
            // server's own column and no surviving client table binds a pad to one (usabledata has no
            // row for 25651, 29648, 25408, 28474 or 21768). LOCAL_TELEPORTER's tuple (8555, 8551, 8550)
            // is DEFAULT's tuple, so the one name-for-name match changes no effect anyone can see.
            var fxTypeOf = typeof(DynamicObjectManager).GetMethod("FxTypeOf", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(fxTypeOf);
            Assert.AreEqual(TeleportType.LocalTeleporter, fxTypeOf.Invoke(null, new object[] { WaypointType.LocalTeleporter }));
            foreach (var waypointType in new[] { WaypointType.Waypoint, WaypointType.Wormhole, WaypointType.Dropship, WaypointType.Hospital })
                Assert.AreEqual(TeleportType.Default, fxTypeOf.Invoke(null, new object[] { waypointType }));
        }

        [DataTestMethod]
        [DataRow(TeleportType.Default)]
        [DataRow(TeleportType.LocalTeleporter)]
        [DataRow(TeleportType.TacticalEvasion)]
        public void PreTeleportCarriesTheClientsOwnTeleporterTypeValue(TeleportType teleportType)
        {
            // generated/client/teleportertype.pyo: DEFAULT 0, BRIDGE_TELEPORTER 1, ADVENTURE_LAUNCHER 2,
            // NOFX 3, BANE_TELEPORTER 4, SELF_DESTRUCT 5, LOCAL_TELEPORTER 6, TACTICAL_EVASION 7.
            Assert.AreEqual(0, (int)TeleportType.Default);
            Assert.AreEqual(6, (int)TeleportType.LocalTeleporter);
            Assert.AreEqual(7, (int)TeleportType.TacticalEvasion);

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new PreTeleportPacket(teleportType).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual((uint)teleportType, reader.ReadUInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void PostTeleportAndTeleportArrivalTakeTheArgumentsTheClientDeclares()
        {
            // Actor.Recv_PostTeleport(self) and Recv_TeleportArrival(self, delayMs = 0,
            // delayEffectMs = 0, doFade = 0): None reads as the default on all three.
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new PostTeleportPacket().Write(writer);
            new TeleportArrivalPacket().Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(0, reader.ReadTuple());
            Assert.AreEqual(3, reader.ReadTuple());
            for (var i = 0; i < 3; i++)
                reader.ReadNoneStruct();
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static Client Player(MapChannel map)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player = new Manifestation { MapChannel = map, Cells = new uint[,] { { 0 } } };
            return client;
        }

        private static IEnumerable<GameOpcode> Calls(List<IClientMessage> messages)
            => messages.OfType<CallMethodMessage>().Select(message => message.MethodId);

        private static CallMethodMessage Call(List<IClientMessage> messages, GameOpcode opcode)
            => messages.OfType<CallMethodMessage>().Single(message => message.MethodId == opcode);

        private static int IndexOf(List<IClientMessage> messages, GameOpcode opcode)
            => messages.FindIndex(message => message is CallMethodMessage call && call.MethodId == opcode);

        private static uint PreTeleportType(CallMethodMessage message)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            message.Packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            return reader.ReadUInt();
        }

        private static uint TeleportedType(CallMethodMessage message)
        {
            // Teleport(position, yaw, teleportType, delay, doCancel)
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            message.Packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual(3, reader.ReadTuple());
            for (var i = 0; i < 4; i++)
                reader.ReadDouble();
            return reader.ReadUInt();
        }

        private static List<IClientMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<IClientMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                messages.Add(protocol.Message);
            return messages;
        }
    }
}
