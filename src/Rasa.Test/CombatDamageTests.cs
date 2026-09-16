using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class CombatDamageTests
    {
        // Official live Deployment 14's published resistance/mitigation examples.
        [DataTestMethod]
        [DataRow(10, 16.67)]
        [DataRow(25, 33.33)]
        [DataRow(50, 50.0)]
        [DataRow(75, 60.0)]
        [DataRow(100, 66.67)]
        [DataRow(150, 75.0)]
        [DataRow(200, 80.0)]
        [DataRow(250, 83.33)]
        public void ResistanceMatchesPublishedLiveMitigation(int value, double mitigation)
        {
            Assert.AreEqual(mitigation, Math.Round(DamageResistance.GetResistedPercent(value), 2));
        }

        // A landed hit is scaled by the same curve, through the helper the player damage path calls. Before
        // 2026-09-16 nothing called it: the curve was recovered and tested but combat ignored it, so resistance
        // gear changed nothing in a fight.
        [DataTestMethod]
        [DataRow(1000, 0, 1000)]
        [DataRow(1000, 10, 833)]
        [DataRow(1000, 25, 667)]
        [DataRow(1000, 50, 500)]
        [DataRow(1000, 75, 400)]
        [DataRow(1000, 100, 333)]
        [DataRow(1000, 150, 250)]
        [DataRow(1000, 200, 200)]
        [DataRow(1000, 250, 167)]
        [DataRow(20, 250, 3)]
        // A hit that lands always takes a point, however small it is against the resistance.
        [DataRow(1, 250, 1)]
        public void LandedHitsAreScaledByTheResistanceCurve(int damage, int resistance, int expected)
        {
            Assert.AreEqual(expected, DamageResistance.ScaleDamage(damage, resistance));
        }

        [TestMethod]
        public void ResistanceIsSummedPerDamageTypeFromEverythingWorn()
        {
            var resistances = new List<ResistanceData>
            {
                new ResistanceData(DamageType.Laser, 4),
                new ResistanceData(DamageType.Laser, 6),
                new ResistanceData(DamageType.Physical, 3)
            };

            Assert.AreEqual(10, DamageResistance.ResistanceFor(resistances, DamageType.Laser));
            Assert.AreEqual(3, DamageResistance.ResistanceFor(resistances, DamageType.Physical));
            Assert.AreEqual(0, DamageResistance.ResistanceFor(resistances, DamageType.Fire));
            Assert.AreEqual(0, DamageResistance.ResistanceFor(resistances, null));
            Assert.AreEqual(0, DamageResistance.ResistanceFor(new List<ResistanceData>(), DamageType.Laser));
        }

        [DataTestMethod]
        [DataRow(-100.0, 2.0, -100.0)]
        [DataRow(-50.0, 1.5, -50.0)]
        [DataRow(-0.5, 1.005, -0.5)]
        [DataRow(0.0, 1.0, 0.0)]
        [DataRow(12.5, 0.8, 20.0)]
        [DataRow(50.0, 0.5, 50.0)]
        [DataRow(450.0, 0.1, 90.0)]
        public void SharedResistanceMathPreservesNegativeAndFractionalValues(double value,
            double multiplier, double percent)
        {
            Assert.AreEqual(multiplier, DamageResistance.GetDamageMultiplier(value), 1e-12);
            Assert.AreEqual(percent, DamageResistance.GetResistedPercent(value), 1e-12);
        }

        [DataTestMethod]
        [DataRow(DamageType.Fire)]
        [DataRow(DamageType.Sonic)]
        [DataRow(DamageType.Electrical)]
        public void LaunchedWeaponCarriesItsSourceDamageTypeThroughImpact(DamageType type)
        {
            var map = new MapChannel();
            map.MapCellInfo.Cells.Add(0, new MapCell());
            var target = new Creature();
            target.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            target.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(target);
            try
            {
                var source = new Manifestation();
                var action = new ActionData(source, ActionId.WeaponAttack, 1, target.EntityId, 0);
                MissileManager.Instance.MissileLaunch(map, action, 23, type);
                Assert.AreEqual(1, map.QueuedMissiles.Count);
                var missile = map.QueuedMissiles[0];
                Assert.AreEqual(type, missile.DamageType);
                MissileManager.Instance.DoWork(map, 1);
                Assert.AreEqual(1, missile.Args.HitData.Count);
                Assert.AreEqual(type, missile.Args.HitData[0].DamageType);
                Assert.AreEqual(23L, missile.Args.HitData[0].FinalAmt);
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(target.EntityId);
                EntityManager.Instance.UnregisterCreature(target.EntityId);
            }
        }

        [TestMethod]
        public void WeaponRecoveryUsesResolvedDamageAndCopiesAllQueuedHitFields()
        {
            const ulong targetId = 0x100000001UL;
            var hit = new HitData
            {
                EntityId = targetId, DamageType = DamageType.Sonic,
                Reflected = 11, Filtered = 12, Absorbed = 13, Resisted = 14,
                FinalAmt = 0x100000002L, IsCritical = 1, DeathBlow = 1,
                CoverModifier = 2, WasImune = 0,
                TargetEffectIds = new List<uint> { 86, 100 }, SourceEffectIds = new List<uint> { 95 }
            };
            var missile = new Missile { ActionId = ActionId.WeaponAttack, ActionArgId = 78, DamageA = 999 };
            missile.Args.HitEntities.Add(targetId);
            missile.Args.HitData.Add(hit);
            var packet = new WeaponAttackRecovery(missile);
            hit.FinalAmt = 0;
            hit.DamageType = DamageType.Physical;
            hit.DeathBlow = 0;
            hit.TargetEffectIds.Clear();
            hit.SourceEffectIds.Clear();
            missile.Args.HitEntities.Clear();
            missile.Args.HitData.Clear();
            missile.ActionArgId = 0;

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)ActionId.WeaponAttack, reader.ReadUInt());
            Assert.AreEqual(78U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(targetId, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(targetId, reader.ReadULong());
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual(7U, reader.ReadUInt());
            Assert.AreEqual(11U, reader.ReadUInt());
            Assert.AreEqual(12U, reader.ReadUInt());
            Assert.AreEqual(13U, reader.ReadUInt());
            Assert.AreEqual(14U, reader.ReadUInt());
            Assert.AreEqual(0x100000002L, reader.ReadLong());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(2U, reader.ReadUInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(86U, reader.ReadUInt());
            Assert.AreEqual(100U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(95U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(Attributes.Health)]
        [DataRow(Attributes.Armor)]
        public void ResourceUpdatesRetainEachQueuedValueAfterDeathOrFurtherDamage(Attributes type)
        {
            const ulong sourceId = 0x100000003UL;
            var attributes = new ActorAttributes(type, 150, 100, 23, 5, 1);
            ServerPythonPacket CreatePacket() => type == Attributes.Health
                ? new UpdateHealthPacket(attributes, sourceId)
                : new UpdateArmorPacket(attributes, sourceId);
            var first = CreatePacket();
            attributes.Current = 0;
            attributes.RefreshAmount = 0;
            var lethal = CreatePacket();
            attributes.Current = 100;
            attributes.CurrentMax = 200;
            attributes.RefreshAmount = 50;
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            first.Write(writer);
            lethal.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            foreach (var expected in new[] { (Current: 23, Refresh: 5), (Current: 0, Refresh: 0) })
            {
                Assert.AreEqual(4, reader.ReadTuple());
                Assert.AreEqual(expected.Current, reader.ReadInt());
                Assert.AreEqual(100, reader.ReadInt());
                Assert.AreEqual(expected.Refresh, reader.ReadInt());
                Assert.AreEqual(sourceId, reader.ReadULong());
            }
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void InitialAttributeTupleFollowsTheOriginalConstructorAndRetainsDepletedValue()
        {
            var health = new ActorAttributes(Attributes.Health, 150, 100, 23, 5, 1);
            var attributes = new Dictionary<Attributes, ActorAttributes> { [Attributes.Health] = health };
            var packet = new AttributeInfoPacket(attributes);
            health.NormalMax = 999;
            health.CurrentMax = 998;
            health.Current = 997;
            health.RefreshAmount = 996;
            health.RefreshPeriod = 995;
            attributes.Clear();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual((int)Attributes.Health, reader.ReadInt());
            Assert.AreEqual(5, reader.ReadTuple());
            Assert.AreEqual(150, reader.ReadInt()); // normalMax
            Assert.AreEqual(100, reader.ReadInt()); // currentMax
            Assert.AreEqual(23, reader.ReadInt());  // current, not baseline
            Assert.AreEqual(5, reader.ReadInt());
            Assert.AreEqual(1, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [DataTestMethod]
        [DataRow(100, 30, 30, 0, 70, 1000)]
        [DataRow(10, 30, 10, 20, 0, 980)]
        [DataRow(0, 30, 0, 30, 0, 970)]
        public void DamageReportSeparatesActualAbsorptionFromRemainingDamage(int armor, int damage,
            int absorbed, int remaining, int armorAfter, int healthAfter)
        {
            var map = new MapChannel();
            map.MapCellInfo.Cells.Add(0, new MapCell());
            var target = new Creature();
            target.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, armor, armor, armor, 0, 0);
            target.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(target);
            try
            {
                var missile = new Missile
                {
                    ActionId = ActionId.WeaponAttack, Source = new Manifestation(),
                    TargetEntityId = target.EntityId, TargetActor = target, DamageA = damage,
                    DamageType = DamageType.Fire
                };
                MissileManager.Instance.MissileTrigger(map, missile);
                var hit = missile.Args.HitData[0];
                Assert.AreEqual((uint)absorbed, hit.Absorbed);
                Assert.AreEqual((long)remaining, hit.FinalAmt);
                Assert.AreEqual((long)damage, hit.FinalAmt + hit.Absorbed + hit.Resisted);
                Assert.AreEqual(armorAfter, target.Attributes[Attributes.Armor].Current);
                Assert.AreEqual(healthAfter, target.Attributes[Attributes.Health].Current);
            }
            finally
            {
                EntityManager.Instance.UnregisterEntity(target.EntityId);
                EntityManager.Instance.UnregisterCreature(target.EntityId);
            }
        }
    }
}
