using System;
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
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// TargetId (372), server to client: what an actor is aiming at. Creature.SetTargetId looks the class up in
    /// generated/client/bonetracking.creaturedata, adds that bone tracker and calls
    /// body.SyncBoneTrackerToTarget(trackerId, connectionpoint.DAMAGE1, targetId), so the weapon bone follows the
    /// target; no shipped UI subscribes to the ACTOR_TARGET_CHANGED the base implementation posts, so the aiming
    /// is the whole visible effect. Nothing ever sent it, so a turret shot you while pointed at its idle heading.
    /// (verify/dis/trpython-client-augmentations-creature.pyo.dis :: Creature.SetTargetId first=186;
    ///  trpython-client-augmentations-actor.pyo.dis :: Actor.Recv_TargetId first=3247, SetTargetId first=3251)
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TargetAimTests
    {
        [TestMethod]
        public void ATargetIsAOneElementTupleAndNoTargetIsNoneNotZero()
        {
            var aimed = Read(new TargetIdPacket(0x100000002UL));
            Assert.AreEqual(1, aimed.ReadTuple());
            Assert.AreEqual(0x100000002UL, aimed.ReadULong());

            // Both SetTargetId overrides branch on `targetId is not None`, and only the None branch removes the
            // bone tracker; a 0 would leave the weapon synced to an entity that does not exist.
            var cleared = Read(new TargetIdPacket(0));
            Assert.AreEqual(1, cleared.ReadTuple());
            cleared.ReadNoneStruct();
        }

        [TestMethod]
        public void ACreatureAimsWhenItPicksAFightAndDropsItWhenTheFightEnds()
        {
            var watcher = Observer();
            var map = new MapChannel();
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { watcher } };
            var turret = new Creature { MapChannel = map, State = CharacterState.Normal, Cells = new uint[1, 1] };

            BehaviorManager.Instance.SetActionFighting(turret, 0x4242UL);
            Assert.AreEqual(0x4242UL, Aims(watcher).Single());

            // The same target again, as every behaviour tick that re-enters the fight would: the client re-syncs
            // its tracker on every TargetId it receives, so a repeat is pure traffic.
            BehaviorManager.Instance.SetActionFighting(turret, 0x4242UL);
            Assert.AreEqual(0, Aims(watcher).Count);

            BehaviorManager.Instance.SetActionFighting(turret, 0x4343UL);
            Assert.AreEqual(0x4343UL, Aims(watcher).Single());

            // Leaving the fight - a dead, gone or leashed target all reach SetActionWander - drops the aim.
            BehaviorManager.Instance.DropFight(turret);
            Assert.AreEqual(0UL, Aims(watcher).Single());
            Assert.AreEqual(0UL, turret.AnnouncedTarget);
        }

        [TestMethod]
        public void APlayersAimGoesToTheRestOfTheCellAndNotBackToThemselves()
        {
            var shooter = Observer();
            var watcher = Observer();
            var map = new MapChannel { ClientList = new List<Client> { shooter, watcher } };
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { shooter, watcher } };
            shooter.Player.MapChannel = map;
            watcher.Player.MapChannel = map;

            var manager = Manager();
            manager.SetTargetId(shooter, 0x4242UL);
            Assert.AreEqual(0x4242UL, shooter.Player.Target);
            Assert.AreEqual(0x4242UL, Aims(watcher).Single());
            Assert.AreEqual(0, Aims(shooter).Count, "the shooter's own client set and aimed at this target itself");

            manager.SetTargetId(shooter, 0x4242UL);
            Assert.AreEqual(0, Aims(watcher).Count);

            // ClientPacketHandler.ClearTargetId reaches this same call with 0, so dropping a target clears the aim.
            manager.SetTargetId(shooter, 0);
            Assert.AreEqual(0UL, shooter.Player.Target);
            Assert.AreEqual(0UL, Aims(watcher).Single());
        }

        [TestMethod]
        public void ACreatureAlreadyFightingBringsItsAimToAClientThatOnlyNowSeesIt()
        {
            const EntityClasses turretClass = (EntityClasses)9000032;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[turretClass] = new EntityClass((uint)turretClass, "test turret", 0, 1, new List<AugmentationType>(), true);
            var fighting = new Creature { EntityClass = turretClass, Cells = new uint[1, 1], AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
            var idle = new Creature { EntityClass = turretClass, Cells = new uint[1, 1], AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };

            foreach (var creature in new[] { fighting, idle })
            {
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
            }

            try
            {
                var map = new MapChannel();
                map.MapCellInfo.Cells[0] = new MapCell();
                fighting.MapChannel = map;
                BehaviorManager.Instance.SetActionFighting(fighting, 0x4242UL);

                var arriving = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                CreatureManager.Instance.CreateCreatureOnClient(arriving, fighting);
                CreatureManager.Instance.CreateCreatureOnClient(arriving, idle);

                var created = Drain(arriving).OfType<CreatePhysicalEntityPacket>().ToList();
                Assert.AreEqual(2, created.Count);
                Assert.AreEqual(0x4242UL, created[0].EntityData.OfType<TargetIdPacket>().Single().TargetEntityId);
                Assert.IsFalse(created[1].EntityData.OfType<TargetIdPacket>().Any());
            }
            finally
            {
                foreach (var creature in new[] { fighting, idle })
                {
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                }
                classes.Remove(turretClass);
            }
        }

        private static List<ulong> Aims(Client client)
            => Drain(client).OfType<TargetIdPacket>().Select(packet => packet.TargetEntityId).ToList();

        private static PythonReader Read(PythonPacket packet)
        {
            var stream = new MemoryStream();
            packet.Write(new PythonWriter(new BinaryWriter(stream)));
            stream.Position = 0;
            return new PythonReader(new BinaryReader(stream));
        }

        private static Client Observer()
        {
            var player = new Manifestation { State = CharacterState.Normal, MovementSpeed = 1.0, Cells = new uint[1, 1] };
            return new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame, Player = player };
        }

        private static ManifestationManager Manager() => (ManifestationManager)Activator.CreateInstance(
            typeof(ManifestationManager), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { null }, null);

        private static List<PythonPacket> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                if (protocol.Message is CallMethodMessage call)
                    packets.Add(call.Packet);
            return packets;
        }
    }
}
