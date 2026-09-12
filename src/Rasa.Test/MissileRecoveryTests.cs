using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class MissileRecoveryTests
    {
        private readonly List<ulong> _entityIds = new List<ulong>();
        private MapChannel _map;
        private Actor _source;

        [TestInitialize]
        public void Initialize()
        {
            _map = new MapChannel();
            _map.MapCellInfo.Cells.Add(0, new MapCell());
            _source = new Actor();
        }

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var entityId in _entityIds)
            {
                EntityManager.Instance.UnregisterEntity(entityId);
                EntityManager.Instance.UnregisterCreature(entityId);
                EntityManager.Instance.UnregisterPlayer(entityId);
                EntityManager.Instance.UnregisterActor(entityId);
            }
        }

        [DataTestMethod]
        [DataRow(ActionId.WeaponAttack)]
        [DataRow(ActionId.WeaponMelee)]
        public void AttackWithoutTargetHasEmptyRecoveryHitLists(ActionId actionId)
        {
            var missile = Launch(actionId, 0);

            MissileManager.Instance.DoWork(_map, 100);

            AssertEmptyRecovery(missile);
            Assert.AreEqual(0, _map.QueuedMissiles.Count);
        }

        [TestMethod]
        public void DespawnedTargetIsNotReportedAsHit()
        {
            var target = RegisterCreature();
            var missile = Launch(ActionId.WeaponMelee, target.EntityId);
            EntityManager.Instance.UnregisterCreature(target.EntityId);
            EntityManager.Instance.UnregisterEntity(target.EntityId);

            MissileManager.Instance.DoWork(_map, 100);

            AssertEmptyRecovery(missile);
            Assert.AreEqual(40, target.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(100, target.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void MissingActorWithRemainingRegistrationDoesNotCrashRecovery()
        {
            var target = RegisterCreature();
            var missile = Launch(ActionId.WeaponMelee, target.EntityId);
            EntityManager.Instance.UnregisterCreature(target.EntityId);

            MissileManager.Instance.DoWork(_map, 100);

            AssertEmptyRecovery(missile);
        }

        [TestMethod]
        public void TargetKilledDuringWindupIsNotReportedAsHit()
        {
            var target = RegisterCreature();
            var missile = Launch(ActionId.WeaponMelee, target.EntityId);
            target.State = CharacterState.Dead;
            target.Attributes[Attributes.Armor].Current = 0;
            target.Attributes[Attributes.Health].Current = 0;

            MissileManager.Instance.DoWork(_map, 100);

            AssertEmptyRecovery(missile);
            Assert.AreEqual(0, target.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ReusedEntityIdDoesNotRedirectPendingDamageToNewCreature()
        {
            var target = RegisterCreature();
            var missile = Launch(ActionId.WeaponMelee, target.EntityId);
            EntityManager.Instance.UnregisterCreature(target.EntityId);
            var replacement = new Creature { EntityId = target.EntityId };
            SetAttributes(replacement);
            EntityManager.Instance.RegisterCreature(replacement);

            MissileManager.Instance.DoWork(_map, 100);

            AssertEmptyRecovery(missile);
            Assert.AreEqual(40, replacement.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(100, replacement.Attributes[Attributes.Health].Current);
        }

        [DataTestMethod]
        [DataRow(ActionId.WeaponAttack)]
        [DataRow(ActionId.WeaponMelee)]
        public void ExistingCreatureReceivesDamageAndKeepsOriginalActionInRecovery(ActionId actionId)
        {
            var target = RegisterCreature();
            var missile = Launch(actionId, target.EntityId);

            MissileManager.Instance.DoWork(_map, 100);

            Assert.AreEqual(0, target.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(80, target.Attributes[Attributes.Health].Current);
            CollectionAssert.AreEqual(new[] { target.EntityId }, missile.Args.HitEntities);
            Assert.AreEqual(1, missile.Args.HitData.Count);
            Assert.AreEqual(target.EntityId, missile.Args.HitData[0].EntityId);
            Assert.AreEqual(60L, missile.Args.HitData[0].FinalAmt);

            using var stream = SerializeRecovery(missile);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)actionId, reader.ReadUInt());
            Assert.AreEqual(1U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(target.EntityId, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(target.EntityId, reader.ReadULong());
        }

        [TestMethod]
        public void ExistingPlayerStillReceivesDamage()
        {
            var player = new Manifestation();
            SetAttributes(player);
            _entityIds.Add(player.EntityId);
            EntityManager.Instance.RegisterEntity(player.EntityId, EntityType.Character);
            EntityManager.Instance.RegisterPlayer(player.EntityId, player);
            EntityManager.Instance.RegisterActor(player.EntityId, player);
            var missile = Launch(ActionId.WeaponMelee, player.EntityId);

            MissileManager.Instance.DoWork(_map, 100);

            Assert.AreEqual(0, player.Attributes[Attributes.Armor].Current);
            Assert.AreEqual(80, player.Attributes[Attributes.Health].Current);
            CollectionAssert.AreEqual(new[] { player.EntityId }, missile.Args.HitEntities);
        }

        private Creature RegisterCreature()
        {
            var target = new Creature();
            SetAttributes(target);
            _entityIds.Add(target.EntityId);
            EntityManager.Instance.RegisterEntity(target.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(target);
            return target;
        }

        private static void SetAttributes(Actor actor)
        {
            actor.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 40, 40, 40, 0, 0));
            actor.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0));
        }

        private Missile Launch(ActionId actionId, ulong targetId)
        {
            MissileManager.Instance.MissileLaunch(_map, new ActionData(_source, actionId, 1, targetId, 0), 60);
            Assert.AreEqual(1, _map.QueuedMissiles.Count);
            return _map.QueuedMissiles[0];
        }

        private static MemoryStream SerializeRecovery(Missile missile)
        {
            var stream = new MemoryStream();
            var writer = new PythonWriter(new BinaryWriter(stream));
            new WeaponAttackRecovery(missile).Write(writer);
            stream.Position = 0;
            return stream;
        }

        private static void AssertEmptyRecovery(Missile missile)
        {
            Assert.AreEqual(0, missile.Args.HitEntities.Count);
            Assert.AreEqual(0, missile.Args.HitData.Count);

            using var stream = SerializeRecovery(missile);
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)missile.ActionId, reader.ReadUInt());
            Assert.AreEqual(missile.ActionArgId, reader.ReadUInt());
            Assert.AreEqual(0, reader.ReadList()); // hits
            Assert.AreEqual(0, reader.ReadList()); // misses
            Assert.AreEqual(0, reader.ReadList()); // miss data
            Assert.AreEqual(0, reader.ReadList()); // hit data
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
