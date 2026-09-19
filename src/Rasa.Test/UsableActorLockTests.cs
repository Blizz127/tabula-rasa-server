using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// A Logos shrine being drawn from is locked to that player and plays its channelling effect for everyone near
    /// it; the lock is released when the draw completes or is interrupted (client/augmentations/usable.py
    /// Recv_LockToActor, Recv_UseInterruptible, Recv_UseInterrupted).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class UsableActorLockTests
    {
        private const ulong Player = 0x5eed;

        private static (Logos Shrine, Client Watcher, Client Other) Setup()
        {
            var map = new MapChannel { MapInfo = new MapInfo(1220, "test", 1, 1), ClientList = new List<Client>() };
            var shrine = new Logos(new LogosEntry { Id = 23, ClassId = 7302, MapContextId = 1220 })
            {
                EntityId = 0x10605, MapChannel = map, Position = new System.Numerics.Vector3(100f, 10f, 100f)
            };
            var matrix = CellManager.Instance.CreateCellMatrix(map,
                (uint)(shrine.Position.X / CellManager.CellSize + CellManager.CellBias),
                (uint)(shrine.Position.Z / CellManager.CellSize + CellManager.CellBias));
            var watcher = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            var other = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            map.MapCellInfo.Cells[matrix[2, 2]].ClientList.Add(watcher);
            return (shrine, watcher, other);
        }

        [TestMethod]
        public void ADrawLocksTheShrineAndCompletionReleasesIt()
        {
            var (shrine, watcher, other) = Setup();

            DynamicObjectManager.LockForUse(shrine, Player);
            var started = Drain(watcher);
            Assert.AreEqual(2, started.Count);
            Assert.AreEqual(Player, ((LockToActorPacket)started[0]).ActorId, "the lock comes first: UseInterruptible is ignored for an actor not locked");
            Assert.AreEqual(Player, ((UseInterruptiblePacket)started[1]).ActorId);
            Assert.AreEqual(0, Drain(other).Count, "only clients near the shrine are told");

            DynamicObjectManager.UnlockAfterUse(shrine, Player, interrupted: false);
            var done = Drain(watcher);
            Assert.AreEqual(1, done.Count);
            Assert.AreEqual(0ul, ((LockToActorPacket)done[0]).ActorId);
        }

        [TestMethod]
        public void AnInterruptedDrawEndsTheEffectAndReleasesTheShrine()
        {
            var (shrine, watcher, _) = Setup();
            DynamicObjectManager.LockForUse(shrine, Player);
            Drain(watcher);

            DynamicObjectManager.UnlockAfterUse(shrine, Player, interrupted: true);
            var packets = Drain(watcher);
            Assert.AreEqual(2, packets.Count);
            Assert.AreEqual(Player, ((UseInterruptedPacket)packets[0]).ActorId);
            Assert.AreEqual(0ul, ((LockToActorPacket)packets[1]).ActorId);
        }

        private static List<PythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    packets.Add(call.Packet);
            return packets;
        }
    }
}
