using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Memory;
using Rasa.Packets.Protocol;
using Rasa.Packets.Login.Client;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class GameProtocolFramingTests
    {
        private Client _client;
        private NonContiguousMemoryStream _incoming;
        private Logger.LoggerConfig _previousLogger;

        [TestInitialize]
        public void Initialize()
        {
            _previousLogger = Logger.Config;
            if (_previousLogger == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
            _client = new Client(null, new ClientPacketHandler()) { State = ClientState.Connected };
            _incoming = (NonContiguousMemoryStream)typeof(Client)
                .GetField("_incomingDataQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_client);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _incoming.Dispose();
            typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, _previousLogger);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void InvalidOrTruncatedProtocolHeaderClosesOnlyItsClient(int size)
        {
            _incoming.CopyFromArray(new byte[] { (byte)size, 0, 0, 0 });
            _client.Update(100);
            Assert.AreEqual(ClientState.Disconnected, _client.State);
        }

        [TestMethod]
        public void FragmentedValidPacketWaitsAndDecodesWithoutChangingItsPayload()
        {
            var packet = Ping(false);
            _incoming.CopyFromArray(packet, 0, 3);
            Assert.IsNull(Decode());
            Assert.AreEqual(0L, _incoming.Position);
            _incoming.CopyFromArray(packet, 3, packet.Length - 3);
            var decoded = Decode();
            Assert.AreEqual(0x12345678u, ((PingMessage)decoded.Message).ClientTime);
            Assert.AreEqual(0L, _incoming.Length);
        }

        [TestMethod]
        public void CoalescedPacketsDecodeIndependently()
        {
            _incoming.CopyFromArray(Ping(false));
            _incoming.CopyFromArray(Ping(false));
            Assert.IsNotNull(Decode());
            Assert.IsNotNull(Decode());
            Assert.IsNull(Decode());
        }

        [TestMethod]
        public void ShortFirstPacketCannotReadFromTheFollowingPacket()
        {
            _incoming.CopyFromArray(new byte[] { 4, 0, 0, 0 });
            _incoming.CopyFromArray(Ping(false));
            _client.Update(100);
            Assert.AreEqual(ClientState.Disconnected, _client.State);
        }

        [TestMethod]
        public void CompressedPacketRoundTripsItsActualPayload()
        {
            using var reader = new BinaryReader(new MemoryStream(Ping(true)));
            var packet = new ProtocolPacket();
            packet.Read(reader);
            Assert.AreEqual(0x12345678u, ((PingMessage)packet.Message).ClientTime);
            Assert.AreEqual(reader.BaseStream.Length, reader.BaseStream.Position);
        }

        [TestMethod]
        public void InflatedLengthClaimDoesNotAllocateTheClaimedBuffer()
        {
            var data = Ping(true);
            // Four-byte framing, flags/type/checksum, compression marker, then declared size.
            Array.Copy(BitConverter.GetBytes(4 * 1024 * 1024), 0, data, 9, 4);
            using var reader = new BinaryReader(new MemoryStream(data));
            var before = GC.GetAllocatedBytesForCurrentThread();
            Assert.ThrowsException<InvalidDataException>(() => new ProtocolPacket().Read(reader));
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsTrue(allocated < 128 * 1024, $"A small compressed frame caused {allocated} allocated bytes");
        }

        [TestMethod]
        public void OversizedStringCountIsRejectedBeforeAllocation()
        {
            using var stream = new MemoryStream();
            using (var writer = new ProtocolBufferWriter(new BinaryWriter(stream), ProtocolBufferFlags.DontFragment))
                writer.WriteCount(4 * 1024 * 1024);
            stream.Position = 0;
            using var reader = new ProtocolBufferReader(new BinaryReader(stream), ProtocolBufferFlags.DontFragment);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Assert.ThrowsException<InvalidDataException>(() => reader.ReadString());
            Assert.IsTrue(GC.GetAllocatedBytesForCurrentThread() - before < 128 * 1024);
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(65)]
        [DataRow(int.MaxValue)]
        public void InvalidHandshakeKeyLengthIsMalformedData(int length)
        {
            using var reader = new BinaryReader(new MemoryStream(BitConverter.GetBytes(length)));
            Assert.ThrowsException<InvalidDataException>(() => new ClientKeyPacket().Read(reader));
        }

        [TestMethod]
        public void TruncatedHandshakeKeyDoesNotReachBigIntegerParsing()
        {
            using var reader = new BinaryReader(new MemoryStream(BitConverter.GetBytes(64)));
            Assert.ThrowsException<EndOfStreamException>(() => new ClientKeyPacket().Read(reader));
        }

        private ProtocolPacket Decode()
            => (ProtocolPacket)typeof(Client).GetMethod("DecodeNextPacket", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_client, null);

        private sealed class CompressedPing : PingMessage, IClientMessage
        {
            ClientMessageSubtypeFlag IClientMessage.SubtypeFlags => ClientMessageSubtypeFlag.Compress;
        }

        private static byte[] Ping(bool compressed)
        {
            IClientMessage message = compressed
                ? new CompressedPing { ClientTime = 0x12345678 }
                : new PingMessage { ClientTime = 0x12345678 };
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            new ProtocolPacket(message, ClientMessageOpcode.Ping, compressed, 0).Write(writer);
            return stream.ToArray();
        }
    }
}
