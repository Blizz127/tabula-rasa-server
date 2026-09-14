using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class PrivateInstanceTests
    {
        private MapChannelManager _maps;
        private readonly List<MapChannel> _populated = new();
        private readonly List<Creature> _creatures = new();

        [TestInitialize]
        public void Initialize()
        {
            _maps = new MapChannelManager(null, () => 0)
            {
                IsPerCharacterContext = contextId => contextId == 1985,
                PopulateInstance = channel =>
                {
                    _populated.Add(channel);
                    var creature = new Creature { MapContextId = channel.MapInfo.MapContextId, Position = new Vector3(10, 0, 10), Npc = new Npc() };
                    _creatures.Add(creature);
                    CellManager.Instance.AddToWorld(channel, creature);
                }
            };
            _maps.MapChannelArray.Add(1985, new MapChannel { MapInfo = new MapInfo(1985, "adv_bootcamp", 783, 4), ClientList = new List<Client>() });
            _maps.MapChannelArray.Add(1220, new MapChannel { MapInfo = new MapInfo(1220, "wilderness", 1556, 0), ClientList = new List<Client>() });
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }
        }

        [TestMethod]
        public void EachCharacterGetsItsOwnPopulatedInstanceAndSharedContextsStayShared()
        {
            var first = _maps.ChannelForEntry(101, 1985);
            var second = _maps.ChannelForEntry(102, 1985);

            Assert.AreNotSame(first, second);
            Assert.AreNotSame(_maps.MapChannelArray[1985], first);
            Assert.IsTrue(first.InstanceId > 1 && second.InstanceId > 1 && first.InstanceId != second.InstanceId);
            Assert.AreEqual(101u, first.OwnerCharacterId);
            CollectionAssert.AreEqual(new[] { first, second }, _populated);
            CollectionAssert.AreEquivalent(new[] { first.InstanceId, second.InstanceId }, _maps.InstanceChannels.Keys.ToArray());

            var wilderness = _maps.ChannelForEntry(101, 1220);
            Assert.AreSame(_maps.MapChannelArray[1220], wilderness);
            Assert.AreEqual(1u, wilderness.InstanceId);
            Assert.IsNull(_maps.ChannelForEntry(101, 4242));
        }

        [TestMethod]
        public void CreaturesAtTheSameSpotInTwoInstancesAreIsolated()
        {
            var first = _maps.ChannelForEntry(101, 1985);
            var second = _maps.ChannelForEntry(102, 1985);
            var firstNpc = _creatures[0];
            var secondNpc = _creatures[1];
            var player = new Manifestation { MapChannel = first, MapContextId = 1985, Position = new Vector3(10, 0, 10) };

            Assert.AreSame(first, firstNpc.MapChannel);
            Assert.IsTrue(MapChannelManager.IsOnChannel(firstNpc, first));
            Assert.IsFalse(MapChannelManager.IsOnChannel(secondNpc, first));
            Assert.IsTrue(MissionManager.IsInConversationRange(player, firstNpc));
            Assert.IsFalse(MissionManager.IsInConversationRange(player, secondNpc));
            Assert.AreSame(second, MapChannelManager.ChannelOf(secondNpc));
        }

        [TestMethod]
        public void AnEmptiedInstanceIsDestroyedAfterTheTickWithItsCreatures()
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player.Id = 101;
            var instance = _maps.ChannelForEntry(101, 1985);
            client.Player.MapChannel = instance;
            client.Player.MapContextId = 1985;
            client.Player.Cells = new uint[5, 5];
            instance.ClientList.Add(client);
            var npc = _creatures.Single();

            _maps.ReleaseInstanceIfEmpty(instance);
            _maps.DestroyQueuedInstances();
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(instance.InstanceId), "an occupied instance is kept");

            client.Player.Id = 0; // no character row to save in this test
            _maps.RemovePlayer(client, false);
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(instance.InstanceId), "destroyed only after the tick");
            _maps.DestroyQueuedInstances();

            Assert.IsFalse(_maps.InstanceChannels.ContainsKey(instance.InstanceId));
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(npc.EntityId));
        }

        [TestMethod]
        public void ReenteringReplacesThePreviousInstanceAndLoginSendsItsId()
        {
            var old = _maps.ChannelForEntry(101, 1985);
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.CharacterSelection };
            client.Player.Id = 101;
            client.Player.MapContextId = 1985;
            client.Player.MapChannel = _maps.ChannelForEntry(101, 1985);
            _maps.DestroyQueuedInstances();

            Assert.IsFalse(_maps.InstanceChannels.ContainsKey(old.InstanceId));
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(client.Player.MapChannel.InstanceId));

            _maps.PassClientToMapInstance(client);
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            WonkavatePacket wonkavate = null;
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage { Packet: WonkavatePacket packet })
                    wonkavate = packet;

            Assert.AreEqual(1985u, wonkavate.MapContextId);
            Assert.AreEqual(client.Player.MapChannel.InstanceId, wonkavate.MapInstanceId);
            Assert.AreSame(client, client.Player.MapChannel.QueuedClients.Single());

            // Queued for entry: not destroyed even though its client list is still empty.
            _maps.ReleaseInstanceIfEmpty(client.Player.MapChannel);
            _maps.DestroyQueuedInstances();
            Assert.IsTrue(_maps.InstanceChannels.ContainsKey(client.Player.MapChannel.InstanceId));
        }
    }
}
