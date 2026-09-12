using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    [DoNotParallelize]
    public class CreatureDeathTests
    {
        private Creature _victim;

        [TestCleanup]
        public void Cleanup()
        {
            if (_victim == null)
                return;
            EntityManager.Instance.UnregisterCreature(_victim.EntityId);
            EntityManager.Instance.UnregisterEntity(_victim.EntityId);
            EntityManager.Instance.UnregisterActor(_victim.EntityId);
        }

        [DataTestMethod]
        [DataRow(ActionId.WeaponAttack)]
        [DataRow(ActionId.WeaponMelee)]
        [DataRow(ActionId.AaRecruitLightning)]
        public void KillingRecoveryPrecedesVictimFallbackAcrossVisibilityBoundaries(ActionId actionId)
        {
            var sourceOnly = Observer();
            var both = Observer();
            var victimOnly = Observer();
            var map = new MapChannel();
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { sourceOnly } };
            map.MapCellInfo.Cells[1] = new MapCell { ClientList = new List<Client> { both } };
            map.MapCellInfo.Cells[2] = new MapCell { ClientList = new List<Client> { victimOnly } };
            // A non-player attacker keeps experience and loot/database paths out
            // of this test of the actual kill and cell-notification lifecycle.
            var source = new Actor { Cells = new uint[,] { { 0, 1 } } };
            _victim = new Creature { State = CharacterState.Normal, Cells = new uint[,] { { 1, 2 } } };
            _victim.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            _victim.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10, 10, 10, 0, 0);
            EntityManager.Instance.RegisterEntity(_victim.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(_victim);

            var killingShot = Shot(source, actionId);
            var laterShot = Shot(source, actionId);
            map.QueuedMissiles.Add(killingShot);
            map.QueuedMissiles.Add(laterShot);
            MissileManager.Instance.DoWork(map, 0);

            Assert.AreEqual(CharacterState.Dead, _victim.State);
            Assert.AreEqual(0, _victim.Attributes[Attributes.Health].Current);
            Assert.AreEqual(0, map.QueuedMissiles.Count);
            Assert.AreEqual(1, killingShot.Args.HitData.Single().DeathBlow);
            Assert.AreEqual(0, laterShot.Args.HitData.Count);
            Assert.AreEqual(0, laterShot.Args.HitEntities.Count);

            var sourcePackets = Drain(sourceOnly);
            var bothPackets = Drain(both);
            var victimPackets = Drain(victimOnly);
            foreach (var packets in new[] { sourcePackets, bothPackets, victimPackets })
                Assert.IsFalse(packets.Any(message => message.MethodId == GameOpcode.StateChange));

            Assert.AreEqual(0, sourcePackets.Count(message => message.MethodId == GameOpcode.ActorKilled));
            Assert.AreEqual(0, victimPackets.Count(message => message.MethodId == GameOpcode.PerformRecovery));
            Assert.AreEqual(_victim.EntityId, victimPackets.Single(message => message.MethodId == GameOpcode.ActorKilled).EntityId);
            Assert.AreEqual(_victim.EntityId, bothPackets.Single(message => message.MethodId == GameOpcode.ActorKilled).EntityId);

            var killIndex = bothPackets.FindIndex(message => message.MethodId == GameOpcode.ActorKilled);
            var recoveries = bothPackets.Where(message => message.MethodId == GameOpcode.PerformRecovery).ToList();
            Assert.AreEqual(2, recoveries.Count);
            Assert.IsTrue(bothPackets.IndexOf(recoveries[0]) < killIndex);
            Assert.IsTrue(killIndex < bothPackets.IndexOf(recoveries[1]));
            Assert.AreEqual(source.EntityId, recoveries[0].EntityId);
            AssertKillingDamage(recoveries[0], actionId);
            AssertKillingDamage(sourcePackets.First(message => message.MethodId == GameOpcode.PerformRecovery), actionId);
            AssertEmptyDamage(recoveries[1], actionId);
        }

        private Missile Shot(Actor source, ActionId actionId) => new Missile
        {
            Source = source, ActionId = actionId, ActionArgId = 1,
            TargetEntityId = _victim.EntityId, TargetActor = _victim, DamageA = 20
        };

        private static Client Observer() => new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };

        private static List<CallMethodMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<CallMethodMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                messages.Add((CallMethodMessage)protocol.Message);
            return messages;
        }

        private void AssertKillingDamage(CallMethodMessage message, ActionId actionId)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            message.Packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)actionId, reader.ReadUInt());
            Assert.AreEqual(1U, reader.ReadUInt());
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(_victim.EntityId, reader.ReadULong());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadList());
            var lightning = actionId == ActionId.AaRecruitLightning;
            Assert.AreEqual(lightning ? 2 : 3, reader.ReadTuple());
            if (!lightning)
                Assert.AreEqual(_victim.EntityId, reader.ReadULong());
            Assert.AreEqual(12, reader.ReadTuple());
            Assert.AreEqual((uint)(lightning ? DamageType.Electrical : DamageType.Physical), reader.ReadUInt());
            for (var i = 0; i < 4; i++)
                reader.ReadUInt();
            Assert.AreEqual(20L, reader.ReadLong());
            reader.ReadInt(); // critical flag
            Assert.AreEqual(1, reader.ReadInt()); // original DamageInfo.Init slot 7: deathBlow
            reader.ReadUInt();
            reader.ReadInt();
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        private static void AssertEmptyDamage(CallMethodMessage message, ActionId actionId)
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            message.Packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual((uint)actionId, reader.ReadUInt());
            Assert.AreEqual(1U, reader.ReadUInt());
            for (var i = 0; i < 4; i++)
                Assert.AreEqual(0, reader.ReadList());
            Assert.AreEqual(stream.Length, stream.Position);
        }
    }
}
