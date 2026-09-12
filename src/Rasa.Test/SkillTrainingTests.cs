using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class SkillTrainingTests
    {
        private static Manifestation Recruit(byte level = 2)
        {
            var player = new Manifestation { Level = level, Class = 1 };
            foreach (var id in new[] { SkillId.Firearms, SkillId.HandToHand, SkillId.MotorAssistArmor, SkillId.Lightning, SkillId.Sprint })
                player.Skills.Add(id, new SkillsData(id, -1, 1));
            return player;
        }

        [TestMethod]
        public void FirearmsUpgradeUsesTwoPointsAndDoesNotMutateBeforePersistence()
        {
            var player = Recruit();
            Assert.IsTrue(SkillTraining.TryPlan(player, new[] { 1 }, new[] { 2 }, out var changes));
            Assert.AreEqual(2, changes[SkillId.Firearms].SkillLevel);
            Assert.AreEqual(-1, changes[SkillId.Firearms].AbilityId);
            Assert.AreEqual(1, player.Skills[SkillId.Firearms].SkillLevel);
            Assert.AreEqual(2, SkillTraining.GetAvailablePoints(player));
            player.Skills[SkillId.Firearms] = changes[SkillId.Firearms];
            Assert.AreEqual(0, SkillTraining.GetAvailablePoints(player));
            Assert.IsTrue(SkillTraining.TryPlan(player, new[] { 1 }, new[] { 2 }, out changes));
            Assert.AreEqual(0, changes.Count);
        }

        [TestMethod]
        public void UnaffordableBatchDoesNotAddOrUpgradeAnySkill()
        {
            var player = Recruit();
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1, 8 }, new[] { 2, 2 }, out var changes));
            Assert.AreEqual(0, changes.Count);
            Assert.AreEqual(1, player.Skills[SkillId.Firearms].SkillLevel);
            Assert.AreEqual(1, player.Skills[SkillId.HandToHand].SkillLevel);
            player.Class = 2;
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 21, 22 }, new[] { 1, 3 }, out changes));
            Assert.AreEqual(0, changes.Count);
            Assert.AreEqual(5, player.Skills.Count);
        }

        [DataTestMethod]
        [DataRow(-1, 1)]
        [DataRow(0, 1)]
        [DataRow(2, 1)]
        [DataRow(200, 1)]
        [DataRow(int.MaxValue, 1)]
        [DataRow(1, -1)]
        [DataRow(1, 0)]
        [DataRow(1, 6)]
        [DataRow(1, int.MaxValue)]
        public void RejectsUnknownIdsDowngradesAndInvalidRanks(int id, int rank)
        {
            var player = Recruit();
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { id }, new[] { rank }, out var changes));
            Assert.AreEqual(0, changes.Count);
            Assert.AreEqual(5, player.Skills.Count);
        }

        [TestMethod]
        public void RejectsDuplicateAndMalformedBatches()
        {
            var player = Recruit(50);
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1, 1 }, new[] { 2, 3 }, out _));
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1 }, new int[0], out _));
            Assert.IsFalse(SkillTraining.TryPlan(player, null, new[] { 2 }, out _));
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1 }, null, out _));
            Assert.IsTrue(SkillTraining.TryPlan(player, new int[0], new int[0], out var changes));
            Assert.AreEqual(0, changes.Count);
        }

        [DataTestMethod]
        [DataRow(1, 0)]
        [DataRow(2, 2)]
        [DataRow(5, 10)]
        [DataRow(15, 32)]
        [DataRow(30, 64)]
        [DataRow(50, 108)]
        public void RetainsExistingPointAwards(int level, int expected)
        {
            Assert.AreEqual(expected, SkillTraining.GetAvailablePoints(Recruit((byte)level)));
        }

        [TestMethod]
        public void InvalidSavedRankCannotGrantExtraTraining()
        {
            var player = Recruit(50);
            player.Skills[SkillId.Firearms].SkillLevel = int.MaxValue;
            Assert.AreEqual(0, SkillTraining.GetAvailablePoints(player));
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 8 }, new[] { 2 }, out _));
        }

        [TestMethod]
        public void ClassTrainingPreservesAncestorsAndRejectsOtherBranches()
        {
            var player = Recruit(50);
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 22 }, new[] { 1 }, out _));
            player.Class = 11; // Spy: Recruit -> Soldier -> Ranger -> Spy.
            Assert.IsTrue(SkillTraining.TryPlan(player, new[] { 1, 22, 162, 161 }, new[] { 2, 1, 1, 1 }, out var changes));
            Assert.AreEqual(4, changes.Count);
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 50 }, new[] { 1 }, out _)); // Sniper only.
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 14 }, new[] { 1 }, out _)); // Specialist branch.
            player.Class = 5; // Polarity Field moved from Ranger to Spy in Deployment 1.7.
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 161 }, new[] { 1 }, out _));
            Assert.IsTrue(SkillTraining.TryPlan(player, new[] { 54 }, new[] { 1 }, out _));
        }

        [TestMethod]
        public void UnknownClassCannotPurchaseSkills()
        {
            var player = Recruit(50);
            player.Class = uint.MaxValue;
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1 }, new[] { 2 }, out _));
            player.Class = 0;
            Assert.IsFalse(SkillTraining.TryPlan(player, new[] { 1 }, new[] { 2 }, out _));
        }

        [TestMethod]
        public void RankJumpChargesEveryIntermediateRank()
        {
            var player = Recruit(8); // Sixteen available; rank one to five costs fourteen.
            Assert.IsTrue(SkillTraining.TryPlan(player, new[] { 49 }, new[] { 5 }, out var changes));
            Assert.AreEqual(194, changes[SkillId.Lightning].AbilityId);
            player.Skills[SkillId.Lightning] = changes[SkillId.Lightning];
            Assert.AreEqual(2, SkillTraining.GetAvailablePoints(player)); // Includes level-five bonus.
        }

        [TestMethod]
        public void QueuedSkillPacketsRetainTheirOwnCharacterAndRankSnapshot()
        {
            var first = Recruit();
            var packet = new SkillsPacket(first.Skills);
            first.Skills[SkillId.Firearms].SkillLevel = 5;
            first.Skills.Clear();
            var other = new SkillsPacket(new Dictionary<SkillId, SkillsData>
            {
                { SkillId.Sprint, new SkillsData(SkillId.Sprint, 401, 4) }
            });

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            using var pw = new PythonWriter(writer);
            packet.Write(pw);
            other.Write(pw);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            using var pr = new PythonReader(reader);
            Assert.AreEqual(1, pr.ReadTuple());
            Assert.AreEqual(5, pr.ReadList());
            var ranks = new Dictionary<int, int>();
            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(2, pr.ReadTuple());
                ranks.Add(pr.ReadInt(), pr.ReadInt());
            }
            Assert.AreEqual(1, ranks[1]);
            Assert.AreEqual(1, ranks[165]);
            Assert.AreEqual(1, pr.ReadTuple());
            Assert.AreEqual(1, pr.ReadList());
            Assert.AreEqual(2, pr.ReadTuple());
            Assert.AreEqual(165, pr.ReadInt());
            Assert.AreEqual(4, pr.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
