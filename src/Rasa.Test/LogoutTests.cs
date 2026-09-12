using System.IO;
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

namespace Rasa.Test
{
    [TestClass]
    public class LogoutTests
    {
        private long _now;
        private MapChannelManager _manager;
        private Client _client;

        [TestInitialize]
        public void Initialize()
        {
            _now = 12000;
            _manager = new MapChannelManager(null, () => _now);
            _client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
        }

        [TestMethod]
        public void AdvertisesTenSecondsInMilliseconds()
        {
            using var stream = new MemoryStream();
            var writer = new PythonWriter(new BinaryWriter(stream));
            new LogoutTimeRemainingPacket().Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(10000, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void CompletionRequiresRequestedCountdownAndFullTenSeconds()
        {
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();
            _manager.RequestLogout(_client);
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();
            _now += 9999;
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();

            _now++;
            _manager.CharacterLogout(_client);
            Assert.IsTrue(_client.Player.RemoveFromMap);
            Assert.AreEqual(ClientState.LoggedIn, _client.State);
            Assert.IsFalse(_client.Player.LogoutCountdown.TryComplete(_now));
        }

        [TestMethod]
        public void CancellationRejectsStaleCompletionAndNewRequestStartsFresh()
        {
            _manager.RequestLogout(_client);
            _now += 9000;
            _manager.CancelLogoutRequest(_client);
            _now += 1000;
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();

            _manager.RequestLogout(_client);
            _now += 9999;
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();
            _now++;
            _manager.CharacterLogout(_client);
            Assert.IsTrue(_client.Player.RemoveFromMap);
        }

        [TestMethod]
        public void RepeatedStartKeepsExistingDeadline()
        {
            _manager.RequestLogout(_client);
            Assert.AreEqual(10000, PopLogoutTimer().TimeRemainingMilliseconds);
            _now += 4000;
            _manager.RequestLogout(_client);
            Assert.AreEqual(6000, PopLogoutTimer().TimeRemainingMilliseconds);
            _now += 6000;
            _manager.CharacterLogout(_client);
            Assert.IsTrue(_client.Player.RemoveFromMap);

            _manager.RequestLogout(_client);
            _now += 10000;
            Assert.IsFalse(_client.Player.LogoutCountdown.TryComplete(_now));
        }

        [TestMethod]
        public void ReplacingCharacterDoesNotCarryCountdownIntoNewSession()
        {
            _manager.RequestLogout(_client);
            _now += 10000;
            _client.Player = new Manifestation();
            _manager.CharacterLogout(_client);
            AssertRemainsInWorld();
        }

        private void AssertRemainsInWorld()
        {
            Assert.IsFalse(_client.Player.RemoveFromMap);
            Assert.AreEqual(ClientState.Ingame, _client.State);
        }

        private LogoutTimeRemainingPacket PopLogoutTimer()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
            var protocol = (ProtocolPacket)queue.PopOutgoing();
            var call = (CallMethodMessage)protocol.Message;
            Assert.AreEqual((ulong)SysEntity.ClientMethodId, call.EntityId);
            Assert.AreEqual(GameOpcode.LogoutTimeRemaining, call.MethodId);
            return (LogoutTimeRemainingPacket)call.Packet;
        }
    }
}
