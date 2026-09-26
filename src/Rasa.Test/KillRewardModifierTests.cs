using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// The kill-experience modifiers beyond the solo fit: the squad share, the danger penalty and the
    /// crit-kill chunk (docs/evidence/kill-rewards.json xp.group, xp.group.recipients, xp.danger, xp.crit).
    /// </summary>
    [TestClass]
    public class KillRewardModifierTests
    {
        private long _now;
        private readonly List<(Client Client, KillExperience Reward)> _experience = new();
        private readonly List<(Client Client, CharacterUpdate Update, object Value)> _updates = new();

        /// <summary>
        /// experiencebarwindow.OnXPEntered (lines 220-223): curXP = 100 - (n - 1) * XP_MOD_PER_PARTY_MEMBER * 100
        /// and groupBonus = int(100 * (curXP / normalXP - 1)) with normalXP = 100 / n.
        /// </summary>
        [DataTestMethod]
        [DataRow(1, 100, 0)]
        [DataRow(2, 92, 84)]
        [DataRow(3, 84, 152)]
        [DataRow(4, 76, 204)]
        [DataRow(5, 68, 240)]
        [DataRow(6, 60, 260)]
        public void SquadShareIsTheClientsExperienceBarArithmetic(int squadSize, int sharePercent, int groupBonus)
        {
            Assert.AreEqual(sharePercent, KillRewardRules.SquadSharePercent(squadSize));
            // What Recv_ExperienceChanged prints: int(groupMod * 100) - 100.
            Assert.AreEqual(groupBonus, (int)(KillRewardRules.GroupMod(squadSize) * 100) - 100);
        }

        /// <summary>
        /// TaRapedia's table (Experience rev 34776, 2008-10-06) agrees for two, four, five and six members and
        /// with its four-member example (3000 / 4 * 3.04 = 2280); its three-member +154% (and 2540) is two
        /// points off the client's own arithmetic, which is what the server follows.
        /// </summary>
        [TestMethod]
        public void TaRapediaSquadTableAgreesExceptItsThreeMemberRow()
        {
            var tarapedia = new Dictionary<int, int> { [2] = 84, [3] = 154, [4] = 204, [5] = 240, [6] = 260 };
            foreach (var (size, bonus) in tarapedia)
            {
                var client = (int)(KillRewardRules.GroupMod(size) * 100) - 100;
                if (size == 3)
                    Assert.AreEqual(bonus - 2, client);
                else
                    Assert.AreEqual(bonus, client, $"{size} members");
            }

            Assert.AreEqual(2280, 3000 * KillRewardRules.SquadSharePercent(4) / 100);
        }

        [TestMethod]
        public void SquadKillPaysEveryMemberInRangeTheirShareAndTheKillerTheCredits()
        {
            var killer = Player(2);
            var mate = Player(2);
            var manager = Manager();
            var creature = new Creature { Level = 2 };

            manager.AwardKill(killer, creature, new[] { mate }, 2);

            // Level 2: base 71.7, split to 35.85, truncated to 35, x1.84 = 64.4.
            Assert.AreEqual(2, _experience.Count);
            foreach (var (client, reward) in _experience)
            {
                Assert.AreEqual(64u, reward.Gained);
                Assert.AreEqual(35u, reward.BaseGained);
                Assert.AreEqual(1.84, reward.GroupMod, 1e-12);
                Assert.AreEqual(1, reward.StreakMod);
                Assert.IsFalse(reward.WasCritKill);
            }
            CollectionAssert.AreEquivalent(new[] { killer, mate }, _experience.Select(entry => entry.Client).ToArray());

            var credits = _updates.Where(update => update.Update == CharacterUpdate.Credits).ToList();
            Assert.AreEqual(1, credits.Count);
            Assert.AreSame(killer, credits[0].Client);
            Assert.AreEqual(10, credits[0].Value);
            Assert.AreEqual(0, mate.Player.KillStreak.Kills);
        }

        [TestMethod]
        public void OnlyTheKillersStreakMultipliesTheirShare()
        {
            var killer = Player(5);
            var mate = Player(5);
            var manager = Manager();
            var creature = new Creature { Level = 4 };

            for (var kill = 0; kill < 3; kill++)
            {
                _now = kill * 1000;
                manager.AwardKill(killer, creature, new[] { mate }, 3);
            }

            // Level 4: base10 825; a third is 27.5 -> 27, x2.52 = 68.04. Third kill: int(27.5 * 2) = 55, x2.52 = 138.6.
            var killerRewards = _experience.Where(entry => entry.Client == killer).Select(entry => entry.Reward).ToList();
            var mateRewards = _experience.Where(entry => entry.Client == mate).Select(entry => entry.Reward).ToList();
            CollectionAssert.AreEqual(new uint[] { 68, 68, 138 }, killerRewards.Select(reward => reward.Gained).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 1, 2 }, killerRewards.Select(reward => reward.StreakMod).ToArray());
            CollectionAssert.AreEqual(new uint[] { 68, 68, 68 }, mateRewards.Select(reward => reward.Gained).ToArray());
            Assert.IsTrue(mateRewards.All(reward => reward.StreakMod == 1 && reward.BaseGained == 27));
        }

        [DataTestMethod]
        [DataRow(10, 12, 100)] // below the creature
        [DataRow(10, 10, 100)]
        [DataRow(10, 6, 100)]  // four above
        [DataRow(10, 5, 100)]  // DANGER_PENALTY_LEVELDIFF_MIN: the ramp starts here
        [DataRow(11, 5, 80)]
        [DataRow(12, 5, 60)]
        [DataRow(13, 5, 40)]
        [DataRow(14, 5, 20)]   // DANGER_PENALTY_LEVELDIFF_MAX
        [DataRow(15, 5, 0)]    // TaRapedia: ten or more levels above, no experience
        [DataRow(50, 1, 0)]
        public void DangerPenaltyRampsFromTheMinimumToNothingPastTheMaximum(int playerLevel, int creatureLevel, int percent)
            => Assert.AreEqual(percent, KillRewardRules.DangerPercent(playerLevel, creatureLevel));

        [TestMethod]
        public void AnOutlevelledKillPaysItsDangerShareAndBeyondTheRampNothing()
        {
            var manager = Manager();
            var creature = new Creature { Level = 3 };

            // Seven above: base 76.9 -> 76, x60% = 45.6. The base line still shows the creature's base.
            var seven = Player(10);
            manager.AwardKill(seven, creature);
            Assert.AreEqual(45u, _experience.Single().Reward.Gained);
            Assert.AreEqual(76u, _experience.Single().Reward.BaseGained);

            // Ten above: no experience line at all (Recv_ExperienceChanged prints nothing for 0), credits still paid.
            _experience.Clear();
            _updates.Clear();
            var ten = Player(13);
            manager.AwardKill(ten, creature);
            Assert.AreEqual(0, _experience.Count);
            Assert.AreEqual(15, _updates.Single(update => update.Update == CharacterUpdate.Credits).Value);

            // Each squadmate's own level decides their penalty.
            _experience.Clear();
            var low = Player(3);
            var high = Player(13);
            manager.AwardKill(low, creature, new[] { high }, 2);
            Assert.AreEqual(1, _experience.Count);
            Assert.AreSame(low, _experience.Single().Client);
        }

        /// <summary>
        /// B1-028 (final week, level-1 creature): "You gained 66 experience points by Crit Killing." then
        /// "You gained 66 experience points." then one "You received 5 credits.". TaRapedia: the crit kill
        /// counts as two kills in the chain.
        /// </summary>
        [TestMethod]
        public void ACritKillPaysACritChunkThenThePlainKillAndCountsTwiceTowardTheStreak()
        {
            var killer = Player(2);
            var manager = Manager();
            var creature = new Creature { Level = 1 };

            manager.AwardKill(killer, creature, critKill: true);

            CollectionAssert.AreEqual(new uint[] { 66, 66 }, _experience.Select(entry => entry.Reward.Gained).ToArray());
            CollectionAssert.AreEqual(new[] { true, false }, _experience.Select(entry => entry.Reward.WasCritKill).ToArray());
            Assert.AreEqual(5, _updates.Single(update => update.Update == CharacterUpdate.Credits).Value);
            Assert.AreEqual(2, killer.Player.KillStreak.Kills);

            // The next kill is the third in the chain and doubles.
            _experience.Clear();
            _now = 1000;
            manager.AwardKill(killer, creature);
            Assert.AreEqual(133u, _experience.Single().Reward.Gained);
        }

        [TestMethod]
        public void SquadShareCountsMembersInTheWorldAndPaysThoseOnTheChannelInRange()
        {
            var killer = Player(5, new Vector3(0, 0, 0));
            var near = Player(5, new Vector3(60, 0, 80));      // exactly 100 m
            var far = Player(5, new Vector3(0, 0, 100.5f));
            var stranger = Player(5, new Vector3(1, 0, 1));
            var creature = new Creature { Level = 5, Position = new Vector3(0, 0, 0) };

            var party = new Party(7, 1, new List<PartyMember>
            {
                Member(1, killer),
                Member(2, near),
                Member(3, far),
                new PartyMember(4, "Away", 0, 5, false), // spot held, not in the world
            });
            var map = new MapChannel { ClientList = new List<Client> { killer, near, far, stranger } };

            var (size, squadmates) = KillRewardManager.SquadShare(map, killer, creature, party);

            Assert.AreEqual(3, size);
            CollectionAssert.AreEqual(new[] { near }, squadmates);

            var (soloSize, none) = KillRewardManager.SquadShare(map, killer, creature, null);
            Assert.AreEqual(1, soloSize);
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public void ExperienceChangedCarriesGroupModAtFullPrecisionAndTheCritFlag()
        {
            var info = new XPInfo(1000, 138, 27) { GroupMod = KillRewardRules.GroupMod(3), StreakMod = 2, WasCritKill = true };

            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
                info.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(8, reader.ReadTuple());
            Assert.AreEqual(1000u, reader.ReadUInt());
            Assert.AreEqual(138u, reader.ReadUInt());
            Assert.AreEqual(27u, reader.ReadUInt());
            var groupMod = reader.ReadDouble();
            Assert.AreEqual(2.52, groupMod);
            // Narrowed to single precision 2.52 would print "+151% Group Bonus".
            Assert.AreEqual(152, (int)(groupMod * 100) - 100);
            Assert.AreEqual(2, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.IsTrue(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        /// <summary>The evidence file states every rule these tests exercise, with its tier.</summary>
        [TestMethod]
        public void EvidenceRecordsTheModifierRulesAndTheirTiers()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("kill-rewards.json")));
            var rules = document.RootElement.GetProperty("rules").EnumerateArray()
                .ToDictionary(rule => rule.GetProperty("id").GetString(), rule => rule.GetProperty("tier").GetString());

            Assert.AreEqual("original", rules["xp.group"]);
            Assert.AreEqual("inferred", rules["xp.group.recipients"]);
            Assert.AreEqual("inferred", rules["xp.danger"]);
            Assert.AreEqual("observed", rules["xp.crit"]);
            Assert.AreEqual("inferred", rules["xp.modifier_order"]);

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToList();
            CollectionAssert.IsSubsetOf(new[] { "GAP-XP-DANGER-SHAPE", "GAP-XP-SQUAD-RANGE", "GAP-XP-SQUAD-STREAK", "GAP-XP-PARTIAL",
                "GAP-CRIT-DEATH-FINISH", "GAP-XP-MODIFIER-ORDER", "GAP-KILL-CREDITS-MODIFIERS" }, gaps);
            CollectionAssert.DoesNotContain(gaps, "GAP-XP-LEVELDIFF");
            CollectionAssert.DoesNotContain(gaps, "GAP-XP-GROUP");
        }

        private KillRewardManager Manager()
        {
            _experience.Clear();
            _updates.Clear();
            return new KillRewardManager(() => _now,
                (client, reward) => _experience.Add((client, reward)),
                (client, update, value) => _updates.Add((client, update, value)));
        }

        private static Client Player(byte level, Vector3 position = default)
        {
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
            client.Player.Level = level;
            client.Player.Position = position;
            return client;
        }

        private static PartyMember Member(uint userId, Client client)
            => new PartyMember(userId, "Member" + userId, 0, client.Player.Level, false) { EntityId = client.Player.EntityId };
    }
}
