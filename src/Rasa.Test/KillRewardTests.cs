using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.ClientMethod.Server;
using Rasa.Packets.Communicator.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class KillRewardTests
    {
        private long _now;
        private readonly List<(uint Experience, uint Base, int StreakMod)> _experience = new();
        private readonly List<(CharacterUpdate Update, object Value)> _updates = new();

        [TestMethod]
        public void RulesReproduceEveryRecordedFootageObservation()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("kill-rewards.json")));
            var observations = document.RootElement.GetProperty("observations").EnumerateArray().ToList();
            Assert.IsTrue(observations.Count >= 9);

            foreach (var observation in observations)
            {
                var level = observation.GetProperty("creature_level").GetInt32();
                var streakMod = observation.GetProperty("streak_mod").GetInt32();
                var name = observation.GetProperty("event").GetString();
                Assert.AreEqual(observation.GetProperty("xp").GetUInt32(), KillRewardRules.Experience(level, streakMod), name);
                Assert.AreEqual(observation.GetProperty("credits").GetInt32(), KillRewardRules.Credits(level), name);
                if (observation.TryGetProperty("base", out var baseXp))
                    Assert.AreEqual(baseXp.GetUInt32(), KillRewardRules.BaseExperience(level), name);
            }
        }

        [DataTestMethod]
        [DataRow(1, 1)]
        [DataRow(9, 1)]
        [DataRow(10, 1)]
        [DataRow(11, 2)]
        [DataRow(41, 5)]
        [DataRow(50, 5)]
        public void MaximumStreakFollowsTheLevelBasis(int playerLevel, int expected)
            => Assert.AreEqual(expected, KillRewardRules.MaxStreak(playerLevel));

        [TestMethod]
        public void FinalWeekCaveFightStreakTimelineIsReproduced()
        {
            // A3-081 .. A4-28: level 2 player, level 2 Thrax Infantry Initiates.
            var (manager, client) = Arrange(2);
            var creature = new Creature { Level = 2 };
            var kills = new[] { 391533L, 396133L, 401533L, 406200L, 415200L, 427733L, 443867L };
            var messages = new List<List<CallMethodMessage>>();

            foreach (var time in kills)
            {
                _now = time;
                manager.ExpireStreaks(Map(client));
                Drain(client);
                manager.AwardKill(client, creature);
                messages.Add(Drain(client));
            }

            CollectionAssert.AreEqual(new uint[] { 71, 71, 143, 143, 143, 143, 71 }, _experience.Select(entry => entry.Experience).ToArray());
            Assert.IsTrue(_experience.All(entry => entry.Base == 71));
            Assert.AreEqual(1, _updates.Count(update => update.Update == CharacterUpdate.Prestige));
            Assert.AreEqual(7, _updates.Count(update => update.Update == CharacterUpdate.Credits && (int)update.Value == 10));

            var third = messages[2];
            var streak = third.FindIndex(message => message.MethodId == GameOpcode.SetKillStreak);
            var prestige = third.FindIndex(message => message.MethodId == GameOpcode.DisplayClientMessage);
            var credits = third.FindIndex(message => message.MethodId == GameOpcode.GotLoot);
            Assert.IsTrue(streak >= 0 && streak < prestige && prestige < credits);
            Assert.AreEqual(1, ((SetKillStreakPacket)third[streak].Packet).Count);
            Assert.AreEqual(PlayerMessage.PmPrestigePointsReceivedKillstreakmax, ((DisplayClientMessagePacket)third[prestige].Packet).MsgId);
            foreach (var later in messages.Skip(3).Take(3))
                Assert.IsFalse(later.Any(message => message.MethodId == GameOpcode.SetKillStreak || message.MethodId == GameOpcode.DisplayClientMessage));
        }

        [TestMethod]
        public void StreakExpiryClearsTheHudOnceAndAHigherLevelNeedsTwoSteps()
        {
            var (manager, client) = Arrange(11);
            var creature = new Creature { Level = 10 };
            for (var kill = 1; kill <= 6; kill++)
            {
                _now = kill * 1000;
                manager.AwardKill(client, creature);
            }

            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 2, 3 }, _experience.Select(entry => entry.StreakMod).ToArray());
            Assert.AreEqual(1, _updates.Count(update => update.Update == CharacterUpdate.Prestige));
            Drain(client);

            _now = 6000 + KillRewardRules.StreakWindowMs;
            manager.ExpireStreaks(Map(client));
            Assert.AreEqual(0, Drain(client).Count);

            _now++;
            manager.ExpireStreaks(Map(client));
            manager.ExpireStreaks(Map(client));
            var cleared = Drain(client);
            Assert.AreEqual(0, ((SetKillStreakPacket)cleared.Single().Packet).Count);
            Assert.AreEqual(0, client.Player.KillStreak.Kills);
        }

        [TestMethod]
        public void CreditsOnlyLootCarriesTheCreatureAndAmount()
        {
            using var stream = new MemoryStream();
            using (var writer = new Rasa.Memory.PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                new GotLootPacket(0x100000005UL, 45).Write(writer);
            stream.Position = 0;
            using var reader = new Rasa.Memory.PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(0x100000005UL, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(45, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private (KillRewardManager, Client) Arrange(byte playerLevel)
        {
            _experience.Clear();
            _updates.Clear();
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player.Level = playerLevel;
            var manager = new KillRewardManager(() => _now,
                (_, experience, baseGained, streakMod) => _experience.Add((experience, baseGained, streakMod)),
                (_, update, value) => _updates.Add((update, value)));
            return (manager, client);
        }

        private static MapChannel Map(Client client) => new MapChannel { ClientList = new List<Client> { client } };

        private static List<CallMethodMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<CallMethodMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    messages.Add(call);
            return messages;
        }
    }
}
