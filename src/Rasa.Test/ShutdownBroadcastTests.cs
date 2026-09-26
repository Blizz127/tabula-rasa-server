using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Networking;
using Rasa.Packets;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures.Char;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The admin broadcast and the operator's shutdown countdown, against the 1.16.5.0 client's
    /// Recv_AdminMessage / disconnect paths and the two final-night recordings recorded in
    /// docs/evidence/shutdown-broadcast.json.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ShutdownBroadcastTests
    {
        private const int Tick = 100; // Server.MainLoopTime

        private Logger.LoggerConfig _oldLogger;
        private Func<IEnumerable<Client>> _oldWorldClients;

        [TestInitialize]
        public void Initialize()
        {
            _oldLogger = Logger.Config;
            if (_oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            _oldWorldClients = AdminBroadcastManager.Instance.WorldClients;
        }

        [TestCleanup]
        public void Cleanup()
        {
            AdminBroadcastManager.Instance.WorldClients = _oldWorldClients;
            if (_oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
        }

        // --- the packet --------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow("Server Shutting Down in 10...")]
        [DataRow("9")]
        [DataRow("ALERT: PLATEAU IS LOST!")]
        [DataRow("AFS-Außenposten E34")]
        public void AdminMessageIsTheClientsTwoArgumentCallWithTheTextAloneAndTheGmFilter(string text)
        {
            var packet = new AdminMessagePacket(text);
            Assert.AreEqual(GameOpcode.AdminMessage, packet.Opcode);
            Assert.AreEqual(24, (int)packet.Opcode, "methodid.AdminMessage");

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                packet.Write(writer);

            var bytes = stream.ToArray();
            // Recv_AdminMessage(msg, filterId): a 2-tuple, the text as a unicode string (0x5D: one byte
            // length), then the filter as an int.
            Assert.AreEqual(0x5D, bytes[1] & 0xFF, "msg is written as a Python unicode string");

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(text, reader.ReadUnicodeString());
            Assert.AreEqual((uint)MsgFilterId.GameMaster, reader.ReadUInt());
            Assert.AreEqual(10000046u, (uint)MsgFilterId.GameMaster, "clientmessagefilter.SYSTEM_GM");
            Assert.AreEqual(stream.Length, stream.Position);

            // The header is the client's own (uielementlanguage 4146); the server never sends it.
            Assert.IsFalse(text.Contains("ADMIN"), "test data");
            Assert.IsFalse(packet.Message.StartsWith("ADMIN MESSAGE", StringComparison.Ordinal));
        }

        [TestMethod]
        public void ABroadcastReachesEveryClientInTheWorldOnTheCommunicatorEntityOnce()
        {
            var a = WorldClient();
            var b = WorldClient();
            var gone = WorldClient();
            gone.State = ClientState.Disconnected;
            AdminBroadcastManager.Instance.WorldClients = () => new[] { a, b, a, gone, null };

            Assert.AreEqual(2, AdminBroadcastManager.Instance.Broadcast("ALERT: PLATEAU IS LOST!"));

            foreach (var client in new[] { a, b })
            {
                var calls = Drain(client);
                Assert.AreEqual(1, calls.Count);
                Assert.AreEqual((ulong)SysEntity.CommunicatorId, calls[0].EntityId);
                var packet = (AdminMessagePacket)calls[0].Packet;
                Assert.AreEqual("ALERT: PLATEAU IS LOST!", packet.Message);
                Assert.AreEqual((uint)MsgFilterId.GameMaster, packet.FilterId);
            }

            Assert.AreEqual(0, AdminBroadcastManager.Instance.Broadcast("   "), "blank text is not sent");
            Assert.AreEqual(0, Drain(a).Count);
        }

        // --- command gating ----------------------------------------------------------------------

        [TestMethod]
        public void AnnounceIsAGameMasterChatCommandAndTheCountdownIsNotAChatCommandAtAll()
        {
            ChatCommandsManager.Instance.RegisterChatCommands();

            Assert.AreEqual(GmLevel.GameMaster, ChatCommandsManager.Instance.RequiredLevel(".announce"));
            Assert.AreEqual(GmLevel.GameMaster, ChatCommandsManager.Instance.RequiredLevel(".announcemap"));
            Assert.IsNull(ChatCommandsManager.Instance.RequiredLevel(".shutdown"), "the countdown is console-only (OD-121)");
        }

        [DataTestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        public void BelowGameMasterAnnounceSendsNothing(byte level)
        {
            ChatCommandsManager.Instance.RegisterChatCommands();
            var listener = WorldClient();
            var caller = WorldClient(level);
            AdminBroadcastManager.Instance.WorldClients = () => new[] { listener, caller };

            ChatCommandsManager.Instance.ProcessCommand(caller, ".announce Server Shutting Down in 10...");

            Assert.AreEqual(0, Drain(listener).Count);
            var reply = Drain(caller).Single();
            Assert.IsInstanceOfType(reply.Packet, typeof(SystemMessagePacket));
            Assert.AreEqual(level == 0 ? "Unknown command: .announce" : ".announce needs account level 5; yours is 1.",
                ((SystemMessagePacket)reply.Packet).TextMessage);
        }

        [TestMethod]
        public void AGameMasterAnnounceSendsTheTypedTextVerbatimToEveryone()
        {
            ChatCommandsManager.Instance.RegisterChatCommands();
            var listener = WorldClient();
            var caller = WorldClient((byte)GmLevel.GameMaster);
            AdminBroadcastManager.Instance.WorldClients = () => new[] { listener, caller };

            ChatCommandsManager.Instance.ProcessCommand(caller, ".announce ALERT:  PLATEAU IS LOST!");

            foreach (var client in new[] { listener, caller })
                Assert.AreEqual("ALERT:  PLATEAU IS LOST!", ((AdminMessagePacket)Drain(client).Single().Packet).Message);

            ChatCommandsManager.Instance.ProcessCommand(caller, ".announce");
            Assert.AreEqual(0, Drain(listener).Count);
            Assert.AreEqual("usage: .announce <text>", ((SystemMessagePacket)Drain(caller).Single().Packet).TextMessage);
        }

        // --- the countdown -----------------------------------------------------------------------

        [TestMethod]
        public void TheDefaultCountdownIsTheMeasuredFinalNight()
        {
            var schedule = ShutdownCountdown.Observed();
            CollectionAssert.AreEqual(
                new[] { "Server Shutting Down in 10...", "9", "8", "7", "6", "5", "4", "3", "2", "1" },
                schedule.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(
                new long[] { 0, 6000, 10000, 13000, 16000, 19000, 23000, 27000, 31000, 34000 },
                schedule.Lines.Select(line => line.AtMs).ToArray());
            Assert.AreEqual(37100, schedule.DisconnectAtMs);

            // Steps 6, 4, 3, 3, 3, 4, 4, 4, 3 s and 3.1 s to the drop: not one a second.
            var steps = schedule.Lines.Zip(schedule.Lines.Skip(1), (x, y) => y.AtMs - x.AtMs).ToArray();
            CollectionAssert.AreEqual(new long[] { 6000, 4000, 3000, 3000, 3000, 4000, 4000, 4000, 3000 }, steps);
            Assert.AreEqual(3100, schedule.DisconnectAtMs - schedule.Lines[^1].AtMs);
        }

        [TestMethod]
        public void TheCountdownBroadcastsOnCadenceAndThenDisconnectsEveryoneOnce()
        {
            var timer = new Rasa.Timer.Timer();
            var world = new[] { WorldClient(), WorldClient() };
            var loading = new Client(null, new ClientPacketHandler()) { State = ClientState.Loading };
            var everyone = world.Append(loading).ToList();
            AdminBroadcastManager.Instance.WorldClients = () => world;

            var heard = new List<(long At, string Text)>();
            var elapsed = 0L;
            var finished = 0;
            var countdown = new ShutdownCountdown(timer,
                text => { heard.Add((elapsed, text)); AdminBroadcastManager.Instance.Broadcast(text); },
                () => ShutdownCountdown.DisconnectAll(everyone),
                () => finished++);

            Assert.IsTrue(countdown.Start(ShutdownCountdown.Observed()));
            Assert.IsFalse(countdown.Start(ShutdownCountdown.Uniform(5, 1000)), "one countdown at a time");

            // Up to the last number: every line on time, nobody disconnected yet.
            while (elapsed < 34000)
            {
                elapsed += Tick;
                timer.Update(Tick);
            }

            var schedule = ShutdownCountdown.Observed();
            Assert.AreEqual(schedule.Lines.Count, heard.Count);
            for (var i = 0; i < heard.Count; i++)
            {
                Assert.AreEqual(schedule.Lines[i].Text, heard[i].Text);
                Assert.IsTrue(heard[i].At >= schedule.Lines[i].AtMs && heard[i].At <= schedule.Lines[i].AtMs + Tick,
                    $"line {heard[i].Text} at {heard[i].At} ms, scheduled {schedule.Lines[i].AtMs}");
            }

            foreach (var client in world)
                CollectionAssert.AreEqual(schedule.Lines.Select(line => line.Text).ToArray(),
                    Drain(client).Select(call => ((AdminMessagePacket)call.Packet).Message).ToArray());
            Assert.AreEqual(0, Drain(loading).Count, "a client outside the world gets no text (OD-124)");
            Assert.IsTrue(everyone.All(client => client.State != ClientState.Disconnected));

            while (elapsed < 37000)
            {
                elapsed += Tick;
                timer.Update(Tick);
            }

            Assert.IsTrue(everyone.All(client => client.State != ClientState.Disconnected), "not before 37.1 s");
            Assert.IsTrue(countdown.Running);

            elapsed += Tick;
            timer.Update(Tick);

            Assert.IsTrue(everyone.All(client => client.State == ClientState.Disconnected), "everyone, in or out of the world");
            Assert.IsFalse(countdown.Running);
            Assert.AreEqual(1, finished);

            for (var i = 0; i < 600; i++)
                timer.Update(Tick);
            Assert.AreEqual(schedule.Lines.Count, heard.Count);
            Assert.AreEqual(1, finished);
        }

        [TestMethod]
        public void NothingHappensWithoutTheOperatorAndCancelStopsItBeforeTheDrop()
        {
            var timer = new Rasa.Timer.Timer();
            var heard = new List<string>();
            var disconnects = 0;
            var countdown = new ShutdownCountdown(timer, heard.Add, () => disconnects++);

            for (var i = 0; i < 6000; i++) // ten minutes of an idle server
                timer.Update(Tick);
            Assert.AreEqual(0, heard.Count);
            Assert.AreEqual(0, disconnects);
            Assert.IsFalse(countdown.Running);
            Assert.IsFalse(countdown.Cancel());

            Assert.IsTrue(countdown.Start(ShutdownCountdown.Observed()));
            for (var elapsed = 0; elapsed < 12000; elapsed += Tick)
                timer.Update(Tick);
            CollectionAssert.AreEqual(new[] { "Server Shutting Down in 10...", "9", "8" }, heard);

            Assert.IsTrue(countdown.Cancel());
            for (var i = 0; i < 600; i++)
                timer.Update(Tick);
            Assert.AreEqual(3, heard.Count);
            Assert.AreEqual(0, disconnects);

            // A new run starts from the top, and the cancelled run's timers stay silent.
            Assert.IsTrue(countdown.Start(ShutdownCountdown.Uniform(2, 1000)));
            for (var i = 0; i < 30; i++)
                timer.Update(Tick);
            CollectionAssert.AreEqual(new[] { "Server Shutting Down in 10...", "9", "8", "Server Shutting Down in 2...", "1" }, heard);
            Assert.AreEqual(1, disconnects);
        }

        [TestMethod]
        public void AUniformCadenceCanBeAskedForAndBadArgumentsAreRefused()
        {
            Assert.IsTrue(ShutdownCountdown.TryParseStart(new string[0], out var observed, out var stay, out _));
            Assert.IsFalse(stay);
            Assert.AreEqual(37100, observed.DisconnectAtMs);

            Assert.IsTrue(ShutdownCountdown.TryParseStart(new[] { "stay" }, out observed, out stay, out _));
            Assert.IsTrue(stay);
            Assert.AreEqual(10, observed.Lines.Count);

            Assert.IsTrue(ShutdownCountdown.TryParseStart(new[] { "5", "1.5", "stay" }, out var uniform, out stay, out _));
            Assert.IsTrue(stay);
            CollectionAssert.AreEqual(new[] { "Server Shutting Down in 5...", "4", "3", "2", "1" }, uniform.Lines.Select(l => l.Text).ToArray());
            CollectionAssert.AreEqual(new long[] { 0, 1500, 3000, 4500, 6000 }, uniform.Lines.Select(l => l.AtMs).ToArray());
            Assert.AreEqual(7500, uniform.DisconnectAtMs);

            foreach (var bad in new[] { new[] { "10" }, new[] { "x", "1" }, new[] { "0", "1" }, new[] { "101", "1" },
                         new[] { "10", "0.05" }, new[] { "10", "601" }, new[] { "10", "NaN" }, new[] { "10", "1", "2" } })
            {
                Assert.IsFalse(ShutdownCountdown.TryParseStart(bad, out var none, out _, out var error), string.Join(" ", bad));
                Assert.IsNull(none);
                Assert.IsFalse(string.IsNullOrEmpty(error));
            }
        }

        /// <summary>
        /// The client draws its disconnect dialog on a connection it did not ask to end (exitgame.GameOnDisconnect,
        /// g_requestedRestart false). The server's side of that is an orderly end of stream with nothing sent before it.
        /// </summary>
        [TestMethod]
        public void DisconnectingClosesTheConnectionWithAnOrderlyEndOfStream()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            using var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            peer.Connect(listener.LocalEndPoint);
            var accepted = listener.Accept();

            var client = WorldClient();
            typeof(Client).GetProperty(nameof(Client.Socket)).SetValue(client, new LengthedSocket(accepted, SizeType.Dword, false));

            Assert.AreEqual(1, ShutdownCountdown.DisconnectAll(new[] { client, client }));
            Assert.AreEqual(ClientState.Disconnected, client.State);
            Assert.AreEqual(0, ShutdownCountdown.DisconnectAll(new[] { client }), "already gone");

            peer.ReceiveTimeout = 5000;
            var buffer = new byte[16];
            Assert.AreEqual(0, peer.Receive(buffer), "end of stream, no bytes and no reset");
        }

        // --- evidence ------------------------------------------------------------------------------

        [TestMethod]
        public void TheEvidenceFileMatchesTheImplementationAndItsGapsAndDecisionsAreInTheManifest()
        {
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("shutdown-broadcast.json")));
            var root = evidence.RootElement;
            Assert.AreEqual("rasa-shutdown-broadcast/1", root.GetProperty("schema").GetString());

            var admin = root.GetProperty("client_contract").GetProperty("admin_message");
            Assert.AreEqual((int)GameOpcode.AdminMessage, admin.GetProperty("method_id").GetInt32());
            Assert.AreEqual((int)MsgFilterId.GameMaster, admin.GetProperty("filter_id").GetInt32());
            Assert.AreEqual((int)SysEntity.CommunicatorId, 8);
            Assert.AreEqual(4146, admin.GetProperty("header_text_id").GetInt32());
            Assert.AreEqual("ADMIN MESSAGE: ", admin.GetProperty("header_text").GetProperty("english").GetString());
            Assert.AreEqual(9, root.GetProperty("client_contract").GetProperty("disconnect").GetProperty("dialog_text_id").GetInt32());

            var countdown = root.GetProperty("countdown");
            Assert.AreEqual(ShutdownCountdown.FirstLineFormat, countdown.GetProperty("first_line_format").GetProperty("value").GetString());
            Assert.AreEqual(ShutdownCountdown.ObservedFrom, countdown.GetProperty("observed_from").GetInt32());
            var schedule = ShutdownCountdown.Observed();
            var lines = countdown.GetProperty("lines").EnumerateArray().ToList();
            Assert.AreEqual(schedule.Lines.Count, lines.Count);
            for (var i = 0; i < lines.Count; i++)
            {
                Assert.AreEqual(schedule.Lines[i].Text, lines[i].GetProperty("text").GetProperty("value").GetString());
                Assert.AreEqual("observed", lines[i].GetProperty("text").GetProperty("tier").GetString());
                var at = lines[i].GetProperty("at_ms");
                Assert.AreEqual(schedule.Lines[i].AtMs, at.GetProperty("value").GetInt64());
                Assert.AreEqual("measured", at.GetProperty("tier").GetString());
                Assert.IsTrue(at.GetProperty("uncertainty").GetProperty("plus_minus").GetDouble() > 0);
                var fx = at.GetProperty("citations").EnumerateArray()
                    .Single(c => c.GetProperty("source").GetString() == "video:fxAtDpxypSw");
                Assert.AreEqual(schedule.Lines[i].AtMs, (long)Math.Round((fx.GetProperty("t").GetDouble() - 122) * 1000),
                    "each offset is its fxAtDpxypSw arrival minus the first line's");
            }

            var drop = countdown.GetProperty("disconnect_at_ms");
            Assert.AreEqual(schedule.DisconnectAtMs, drop.GetProperty("value").GetInt64());
            Assert.AreEqual("measured", drop.GetProperty("tier").GetString());
            Assert.AreEqual(159.1, drop.GetProperty("citations")[0].GetProperty("t").GetDouble());

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var gaps = manifest.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in root.GetProperty("gaps").EnumerateArray())
                Assert.IsTrue(gaps.Contains(gap.GetString()), gap.GetString());
            Assert.AreEqual(6, root.GetProperty("gaps").GetArrayLength());

            var decisions = manifest.RootElement.GetProperty("owner_decisions").EnumerateArray()
                .ToDictionary(d => d.GetProperty("id").GetString(), d => d);
            foreach (var id in root.GetProperty("decisions").EnumerateArray().Select(d => d.GetString()))
                Assert.IsTrue(decisions.ContainsKey(id), id);

            var source = manifest.RootElement.GetProperty("sources").GetProperty("measurement:shutdown-broadcast");
            Assert.AreEqual("docs/evidence/shutdown-broadcast.json", source.GetProperty("member").GetString());
        }

        // --- helpers -------------------------------------------------------------------------------

        private static Client WorldClient(byte level = 0)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Id = 1, Level = level });
            return client;
        }

        private static List<CallMethodMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var calls = new List<CallMethodMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    calls.Add(call);
            return calls;
        }
    }
}
