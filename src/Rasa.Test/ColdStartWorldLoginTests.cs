using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Cryptography;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Login;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets.Protocol;

namespace Rasa.Test
{
    /// <summary>
    /// The world login a cold-started Game server sees first (2026-09-28, 19:03:21 UTC): the first
    /// connection after the process started, whose key exchange hands the socket to the world client
    /// while the main loop holds the client list for a whole tick. Everything here runs the server's
    /// own classes over loopback - LengthedSocket, LoginManager, LoginClient, Client.RegisterAtServer -
    /// against a scripted 1.16.5.0 client that speaks the protocol byte for byte as the server reads it
    /// (ServerKey, ClientKey, the encrypted "ENC OK", then an encrypted Login ProtocolPacket).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ColdStartWorldLoginTests
    {
        private static readonly BigInteger Prime = new BigInteger(DHKeyExchange.ConstantPrime, true, true);

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            // The same sizes NetworkFramingTests uses: both are process-wide and whichever runs first wins.
            BufferManager.Initialize(65536, 4, 4);
            LengthedSocket.InitializeEventArgsPool(16);
        }

        /// <summary>A Game-shaped listener: accept, key exchange, then Server.OnLogin's handoff under the Clients lock.</summary>
        private sealed class WorldPort : IDisposable
        {
            public readonly List<Client> Clients = new();
            public readonly LoginManager Logins = new();
            public readonly LengthedSocket Listener = new(SizeType.Dword, false);
            public int Port => ((IPEndPoint) Listener.Socket.LocalEndPoint).Port;
            public readonly ManualResetEventSlim Registered = new();

            public WorldPort()
            {
                Logins.OnLogin += login =>
                {
                    // Server.OnLogin: the client list is the main loop's for the length of a tick.
                    lock (Clients)
                    {
                        var client = new Client(null, new ClientPacketHandler());
                        client.RegisterAtServer(null, login.Socket, login.Data);
                        Clients.Add(client);
                    }

                    Registered.Set();
                };

                Listener.OnAccept += socket =>
                {
                    Listener.AcceptAsync();
                    Logins.LoginSocket(socket);
                };

                Listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Listener.Listen(8);
                Listener.AcceptAsync();
            }

            public void Dispose()
            {
                lock (Clients)
                    foreach (var client in Clients)
                        client.Close(false, "test over");

                Listener.Close();
            }
        }

        /// <summary>The client end of the world port, as the 1.16.5.0 client frames and encrypts it.</summary>
        private sealed class ScriptedClient : IDisposable
        {
            public readonly Socket Socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            public readonly ClientCryptData Crypt = new();
            public byte[] ServerKeyBody;

            public ScriptedClient(int port)
            {
                Socket.ReceiveTimeout = 10000;
                Socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
            }

            public byte[] ReadFrame()
            {
                var header = ReadExactly(4);
                return ReadExactly((int) BitConverter.ToUInt32(header, 0));
            }

            private byte[] ReadExactly(int count)
            {
                var data = new byte[count];
                for (var read = 0; read < count;)
                {
                    var got = Socket.Receive(data, read, count - read, SocketFlags.None);
                    if (got == 0)
                        throw new EndOfStreamException($"The server closed the connection after {read} of {count} bytes.");
                    read += got;
                }

                return data;
            }

            private static byte[] Frame(byte[] body)
            {
                var frame = new byte[4 + body.Length];
                BitConverter.GetBytes((uint) body.Length).CopyTo(frame, 0);
                body.CopyTo(frame, 4);
                return frame;
            }

            /// <summary>ServerKey in, ClientKey out; returns the ClientKey frame, sent by the caller.</summary>
            public byte[] AnswerServerKey()
            {
                ServerKeyBody = ReadFrame();

                using var reader = new BinaryReader(new MemoryStream(ServerKeyBody));
                var aLength = reader.ReadInt32();
                var pLength = reader.ReadInt32();
                var gLength = reader.ReadInt32();
                var a = new BigInteger(reader.ReadBytes(aLength), true, true);
                var p = new BigInteger(reader.ReadBytes(pLength), true, true);
                var g = new BigInteger(reader.ReadBytes(gLength), true, true);
                Assert.AreEqual(Prime, p);

                var secret = new byte[64];
                new Random(1790622201).NextBytes(secret);
                var b = new BigInteger(secret, true, true) % p;

                var k = To64(BigInteger.ModPow(a, b, p));
                GameCryptManager.Initialize(Crypt, k);

                using var body = new MemoryStream();
                using (var writer = new BinaryWriter(body, Encoding.UTF8, true))
                {
                    writer.Write(64);
                    writer.Write(To64(BigInteger.ModPow(g, b, p)));
                }

                return Frame(body.ToArray());
            }

            /// <summary>The "ENC OK" acknowledgement, decrypted with this side's own key.</summary>
            public string ReadKeyAcknowledgement()
            {
                var body = ReadFrame();
                Assert.AreEqual(0, body.Length % 8, "The acknowledgement is whole Blowfish blocks.");
                Assert.IsTrue(GameCryptManager.Decrypt(body, 0, body.Length, Crypt));
                Assert.AreEqual(2, BitConverter.ToInt16(body, 0));
                return Encoding.UTF8.GetString(body, 2, body.Length - 2).TrimEnd('Ì', '\0');
            }

            /// <summary>The world Login, framed and encrypted the way Client.OnDecrypt takes it apart.</summary>
            public byte[] LoginFrame(uint accountId, uint oneTimeKey)
            {
                using var packet = new MemoryStream();
                using (var writer = new BinaryWriter(packet, Encoding.UTF8, true))
                    new ProtocolPacket(new LoginMessage
                    {
                        Subtype = LoginMessageSubtype.Type1,
                        AccountId = accountId,
                        OneTimeKey = oneTimeKey,
                        Version = "1.16.5.0"
                    }, ClientMessageOpcode.Login, false, 0).Write(writer);

                var plain = packet.ToArray();
                var padding = 8 - plain.Length % 8;
                var body = new byte[padding + plain.Length];
                body[0] = (byte) padding;
                plain.CopyTo(body, padding);

                var length = body.Length;
                GameCryptManager.Encrypt(body, 0, ref length, body.Length, Crypt);
                return Frame(body);
            }

            private static byte[] To64(BigInteger value)
            {
                var bytes = value.ToByteArray(true, true);
                var padded = new byte[64];
                bytes.CopyTo(padded, 64 - bytes.Length);
                return padded;
            }

            public void Dispose() => Socket.Dispose();
        }

        private static List<ProtocolPacket> DecodeWhatArrived(Client client)
        {
            typeof(Client).GetMethod("DrainPendingChunks", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(client, null);
            var decode = (IEnumerable<ProtocolPacket>) typeof(Client).GetMethod("DecodeIncomingPackets", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(client, null);
            return decode.ToList();
        }

        private static List<ProtocolPacket> WaitForPackets(Client client, int count)
        {
            var packets = new List<ProtocolPacket>();
            SpinWait.SpinUntil(() =>
            {
                packets.AddRange(DecodeWhatArrived(client));
                return packets.Count >= count || client.State == ClientState.Disconnected;
            }, TimeSpan.FromSeconds(10));
            return packets;
        }

        /// <summary>
        /// The first world connection after start-up, with the main loop holding the client list while
        /// the key exchange finishes (a long first tick) and the client's Login split across two writes
        /// that land while the handoff is still waiting. The connection must come through intact: the
        /// acknowledgement decrypts under the client's key, the world client registers once the tick
        /// ends, and the Login it reads is the one that was sent.
        /// </summary>
        [TestMethod]
        public void FirstWorldLoginAfterStartSurvivesAHandoffHeldByTheMainLoop()
        {
            using var port = new WorldPort();
            using var client = new ScriptedClient(port.Port);

            var clientKey = client.AnswerServerKey();

            Client world;
            // The "main loop" holds the list from before the ClientKey until after the Login is on the wire.
            lock (port.Clients)
            {
                client.Socket.Send(clientKey);
                Assert.AreEqual("ENC OK", client.ReadKeyAcknowledgement());

                var login = client.LoginFrame(4, 0xC0FFEE);
                client.Socket.Send(login, 0, 7, SocketFlags.None);
                Thread.Sleep(50);
                client.Socket.Send(login, 7, login.Length - 7, SocketFlags.None);

                Assert.IsFalse(port.Registered.Wait(200), "The handoff waits for the tick, as Server.OnLogin does.");
            }

            Assert.IsTrue(port.Registered.Wait(TimeSpan.FromSeconds(10)), "The world client registers once the tick lets go.");
            lock (port.Clients)
                world = port.Clients.Single();

            var packets = WaitForPackets(world, 1);
            Assert.AreEqual(ClientState.Connected, world.State, "Nothing closed the first connection.");
            Assert.AreEqual(1, packets.Count);
            var message = packets[0].Message as LoginMessage;
            Assert.IsNotNull(message, $"Expected the Login, got {packets[0].Type}.");
            Assert.AreEqual(4u, message.AccountId);
            Assert.AreEqual(0xC0FFEEu, message.OneTimeKey);
            Assert.AreEqual("1.16.5.0", message.Version);
        }

        /// <summary>
        /// Nothing the client is sent in the key exchange depends on whether it is the first connection
        /// the process has served. A second connection gets the same ServerKey bytes and its own working
        /// acknowledgement; an Login pipelined straight behind the ClientKey, in the same write, is read
        /// by the world client that takes the socket over mid-buffer.
        /// </summary>
        [TestMethod]
        public void TheFirstKeyExchangeIsTheSameAsEveryLaterOne()
        {
            using var port = new WorldPort();

            byte[] first;
            using (var client = new ScriptedClient(port.Port))
            {
                client.Socket.Send(client.AnswerServerKey());
                Assert.AreEqual("ENC OK", client.ReadKeyAcknowledgement());
                first = client.ServerKeyBody;
            }

            using var second = new ScriptedClient(port.Port);
            var clientKey = second.AnswerServerKey();
            CollectionAssert.AreEqual(first, second.ServerKeyBody);

            port.Registered.Reset();
            second.Socket.Send(clientKey);
            Assert.AreEqual("ENC OK", second.ReadKeyAcknowledgement());
            Assert.IsTrue(port.Registered.Wait(TimeSpan.FromSeconds(10)));
            second.Socket.Send(second.LoginFrame(7, 99));

            Client world;
            lock (port.Clients)
                world = port.Clients.Last();

            var packets = WaitForPackets(world, 1);
            Assert.AreEqual(ClientState.Connected, world.State);
            Assert.AreEqual(7u, (packets.Single().Message as LoginMessage)?.AccountId);
        }

        /// <summary>
        /// Every way a world connection ends is logged with its reason. The client going away after its
        /// Login used to leave only "*** Client disconnected! Ip: ..." - the line the cold-start report
        /// had, which could not say who ended it - and one that never finished the key exchange left
        /// nothing at all.
        /// </summary>
        [TestMethod]
        public void EveryWorldDisconnectSaysWhoEndedItAndHowFarItGot()
        {
            var captured = new StringWriter();
            var original = Console.Out;
            Console.SetOut(TextWriter.Synchronized(captured));

            try
            {
                using var port = new WorldPort();

                // A reachability probe: connect and go.
                using (new ScriptedClient(port.Port)) { }
                // Closing with the ServerKey unread resets rather than closes; either is the other side.
                Assert.IsTrue(SpinWait.SpinUntil(() => captured.ToString().Contains("World key exchange with 127.0.0.1 ended before it completed: the other side"),
                    TimeSpan.FromSeconds(10)), captured.ToString());

                // A client that logs in and then closes its end.
                using (var client = new ScriptedClient(port.Port))
                {
                    client.Socket.Send(client.AnswerServerKey());
                    client.ReadKeyAcknowledgement();
                    Assert.IsTrue(port.Registered.Wait(TimeSpan.FromSeconds(10)));
                    client.Socket.Send(client.LoginFrame(4, 1));

                    Client world;
                    lock (port.Clients)
                        world = port.Clients.Single();

                    Assert.AreEqual(ClientMessageOpcode.Login, WaitForPackets(world, 1).Single().Type);
                }

                Assert.IsTrue(SpinWait.SpinUntil(() => captured.ToString().Contains("*** Client disconnected! Ip: 127.0.0.1 - the other side closed the connection (state Connected, no account yet,"),
                    TimeSpan.FromSeconds(10)), captured.ToString());
                // The Login frame arrived; no main loop ran here to handle it.
                StringAssert.Contains(captured.ToString(), "1 frame(s) in, world Login not handled");
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}
