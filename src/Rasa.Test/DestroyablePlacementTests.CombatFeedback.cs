using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Structures;

namespace Rasa.Test
{
    public partial class DestroyablePlacementTests
    {
        [DataTestMethod]
        [DataRow(60, false)]
        [DataRow(100, false)]
        [DataRow(150, true)]
        public void WeaponHitOnPracticeDummyIncludesOriginalDamageFeedback(int damage, bool critical)
        {
            _missions.AssignNpcMission(_client, _giver.EntityId, MissionId);
            Drain(_client);
            var shot = new Missile
            {
                Source = _client.Player, TargetEntityId = _dummy.EntityId,
                ActionId = ActionId.WeaponAttack, ActionArgId = 203,
                DamageA = damage, DamageType = DamageType.Physical, IsCritical = critical
            };

            MissileManager.Instance.MissileTrigger(_map, shot);

            Assert.AreEqual(damage < 100 ? (uint)(100 - damage) : 0u, _dummy.HitPoints);
            Assert.AreEqual(damage < 100 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Completed,
                _client.Player.Missions[MissionId].Objectives[3]);
            // Parse exactly what BaseWeaponAttack.DoHits -> Usable.AnnounceDamage consumes.
            var recovery = Drain(_client).OfType<WeaponAttackRecovery>().Single();
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            recovery.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)ActionId.WeaponAttack, reader.ReadUInt());
            Assert.AreEqual(203u, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(_dummy.EntityId, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadList(), "A hit ID without its damage record suppresses original floating damage.");
            Assert.AreEqual(3, reader.ReadTuple());
            Assert.AreEqual(_dummy.EntityId, reader.ReadULong());
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual((uint)DamageType.Physical, reader.ReadUInt());
            for (var i = 0; i < 4; i++) Assert.AreEqual(0u, reader.ReadUInt());
            Assert.AreEqual((long)damage, reader.ReadLong());
            Assert.AreEqual(critical ? 1 : 0, reader.ReadInt());
        }

        [TestMethod]
        public void ShotAtDestroyedDummyHasNoHitUntilItsRestoreDeadline()
        {
            _content.DamageContentUsable(_map, _dummy.EntityId, 100);
            var restoreAt = _dummy.RestoreAt;
            var shot = new Missile
            {
                Source = _client.Player, TargetEntityId = _dummy.EntityId,
                ActionId = ActionId.WeaponAttack, DamageA = 60
            };
            MissileManager.Instance.MissileTrigger(_map, shot);
            Assert.AreEqual(0, shot.Args.HitEntities.Count);
            Assert.AreEqual(0, shot.Args.HitData.Count);
            Assert.AreEqual(restoreAt, _dummy.RestoreAt);
            _content.RestoreDestroyedUsables(_map, restoreAt - 1);
            Assert.AreEqual(0u, _dummy.HitPoints);
            _content.RestoreDestroyedUsables(_map, restoreAt);
            Assert.AreEqual(100u, _dummy.HitPoints);

            MissileManager.Instance.MissileTrigger(_map, shot);
            Assert.AreEqual(40u, _dummy.HitPoints);
            Assert.AreEqual(_dummy.EntityId, shot.Args.HitEntities.Single());
            Assert.AreEqual(60L, shot.Args.HitData.Single().FinalAmt);
        }
    }
}
