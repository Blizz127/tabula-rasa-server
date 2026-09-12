using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class LightningEffectTests
    {
        [DataTestMethod]
        [DataRow(1, -1, -1, -1, -1, -1)]
        [DataRow(2, 100, 12, 210, -1, -1)]
        [DataRow(3, 100, 18, 90, 50, -1)]
        [DataRow(4, 100, 24, 210, 50, 50)]
        [DataRow(5, 100, 30, 210, 50, 50)]
        public void TrainableRowsPreserveTheirDistinctOptionalProperties(int rank, int chance,
            int radius, int damage, int extraPercent, int stunChance)
        {
            var data = LightningEffectData.ForRank((uint)rank, 1);
            Assert.AreEqual(chance, data.PercentageChance.GetValueOrDefault(-1));
            Assert.AreEqual(radius, data.ArcRadius.GetValueOrDefault(-1));
            Assert.AreEqual(damage, data.ArcDamage.GetValueOrDefault(-1));
            Assert.AreEqual(extraPercent, data.ExtraDamagePercent.GetValueOrDefault(-1));
            Assert.AreEqual(rank >= 3 ? (DamageType?)DamageType.Sonic : null, data.ExtraDamageType);
            Assert.AreEqual(stunChance, data.StunChance.GetValueOrDefault(-1));
            Assert.AreEqual(rank >= 4 ? (int?)3 : null, data.StunDurationSeconds);
            Assert.AreEqual(rank == 5 ? (int?)6000 : null, data.StormDurationMilliseconds);
            Assert.AreEqual(rank == 5 ? (int?)2000 : null, data.StormIntervalMilliseconds);
            Assert.AreEqual(rank == 5 ? (int?)60 : null, data.StormDamageMinimum);
            Assert.AreEqual(rank == 5 ? (int?)90 : null, data.StormDamageMaximum);
        }

        [DataTestMethod]
        [DataRow(3, 9, 180, -1, -1)]
        [DataRow(5, 9, 420, 120, 180)]
        [DataRow(5, 17, 840, 240, 360)]
        public void ArcAndStormDamageUseTheOriginalActorScaling(int rank, int level,
            int arcDamage, int stormMinimum, int stormMaximum)
        {
            var data = LightningEffectData.ForRank((uint)rank, level);
            Assert.AreEqual(arcDamage, data.ArcDamage);
            Assert.AreEqual(stormMinimum, data.StormDamageMinimum.GetValueOrDefault(-1));
            Assert.AreEqual(stormMaximum, data.StormDamageMaximum.GetValueOrDefault(-1));
        }

        [TestMethod]
        public void EffectDataRejectsUntrainableVariantsAndInvalidLevels()
        {
            foreach (var rank in new uint[] { 0, 6, 7, uint.MaxValue })
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => LightningEffectData.ForRank(rank, 1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => LightningEffectData.ForRank(1, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => LightningEffectData.ForRank(5, -1));
            Assert.AreEqual(95, LightningEffectData.ArcEffectTypeId);
            Assert.AreEqual(100, LightningEffectData.StormEffectTypeId);
            Assert.AreEqual(86, LightningEffectData.StunEffectTypeId);
        }

        [TestMethod]
        public void RecoveryPreservesResolvedDamageAndPerTargetArcsInQueuedSnapshot()
        {
            var primary = CreateHit(1, DamageType.Sonic);
            var arcDamage = CreateHit(2, DamageType.Electrical);
            var arcs = new List<LightningArcHit>
            {
                new LightningArcHit(0x100000003UL, arcDamage),
                new LightningArcHit(0x100000004UL, CreateHit(3, DamageType.Electrical))
            };
            primary.LightningArcs = arcs;
            var second = CreateHit(4, DamageType.Electrical);
            var missile = new Missile
            {
                ActionId = ActionId.AaRecruitLightning, ActionArgId = 5,
                DamageA = 999, Args = new MissileArgs()
            };
            missile.Args.HitEntities.AddRange(new[] { 0x100000001UL, 0x100000002UL });
            missile.Args.HitData.AddRange(new[] { primary, second });
            var packet = new LightningRecovery(missile);

            primary.FinalAmt = 0;
            primary.DamageType = DamageType.Physical;
            primary.TargetEffectIds.Clear();
            primary.SourceEffectIds.Clear();
            arcDamage.FinalAmt = 0;
            arcDamage.TargetEffectIds.Clear();
            arcs.Clear();
            missile.ActionArgId = 1;
            missile.Args.HitEntities.Clear();
            missile.Args.HitData.Clear();

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(194U, reader.ReadUInt());
            Assert.AreEqual(5U, reader.ReadUInt());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(0x100000001UL, reader.ReadULong());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(2, reader.ReadTuple());
            AssertDamage(reader, 1, DamageType.Sonic);
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            AssertArc(reader, 0x100000003UL, 2);
            AssertArc(reader, 0x100000004UL, 3);
            Assert.AreEqual(2, reader.ReadTuple());
            AssertDamage(reader, 4, DamageType.Electrical);
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void EmptyDamageRetainsZeroAmountAndWritesRequiredEmptyEffectLists()
        {
            var damage = DamageInfoData.FromHit(new HitData { DamageType = DamageType.Electrical });
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            damage.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual(13U, reader.ReadUInt());
            for (var i = 0; i < 4; i++)
                Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0L, reader.ReadLong());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0U, reader.ReadUInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void StormTickCarriesEffectInstanceAndSeparateDamageAndArcSnapshots()
        {
            var damage = new List<LightningArcHit>
            {
                new LightningArcHit(0x100000007UL, CreateHit(7, DamageType.Electrical))
            };
            var arcs = new List<LightningArcHit>
            {
                new LightningArcHit(0x100000008UL, CreateHit(8, DamageType.Electrical))
            };
            var packet = new LightningStormTickPacket(9123, damage, arcs);
            damage.Clear();
            arcs.Clear();
            Assert.AreEqual(GameOpcode.GameEffectTick, packet.Opcode);
            Assert.AreEqual(278, (int)packet.Opcode);
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(9123, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadList());
            AssertArc(reader, 0x100000007UL, 7);
            Assert.AreEqual(1, reader.ReadList());
            AssertArc(reader, 0x100000008UL, 8);
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void StormTickRetainsBothListsWhenNoDamageWasReported()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new LightningStormTickPacket(10, Array.Empty<LightningArcHit>(),
                Array.Empty<LightningArcHit>()).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(10, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static HitData CreateHit(int seed, DamageType type) => new HitData
        {
            DamageType = type, Reflected = (uint)(10 + seed), Filtered = (uint)(20 + seed),
            Absorbed = (uint)(30 + seed), Resisted = (uint)(40 + seed), FinalAmt = 0x100000000L + seed,
            IsCritical = 1, DeathBlow = 0, CoverModifier = (uint)(50 + seed), WasImune = 1,
            TargetEffectIds = new List<uint> { (uint)(100 + seed), (uint)(200 + seed) },
            SourceEffectIds = new List<uint> { (uint)(300 + seed) }
        };

        private static void AssertArc(PythonReader reader, ulong targetId, int seed)
        {
            Assert.AreEqual(2, reader.ReadTuple());
            Assert.AreEqual(targetId, reader.ReadULong());
            AssertDamage(reader, seed, DamageType.Electrical);
        }

        private static void AssertDamage(PythonReader reader, int seed, DamageType type)
        {
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual((uint)type, reader.ReadUInt());
            Assert.AreEqual((uint)(10 + seed), reader.ReadUInt());
            Assert.AreEqual((uint)(20 + seed), reader.ReadUInt());
            Assert.AreEqual((uint)(30 + seed), reader.ReadUInt());
            Assert.AreEqual((uint)(40 + seed), reader.ReadUInt());
            Assert.AreEqual(0x100000000L + seed, reader.ReadLong());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual((uint)(50 + seed), reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual((uint)(100 + seed), reader.ReadUInt());
            Assert.AreEqual((uint)(200 + seed), reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual((uint)(300 + seed), reader.ReadUInt());
        }
    }
}
