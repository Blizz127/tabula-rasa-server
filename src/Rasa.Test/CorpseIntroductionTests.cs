using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.Game.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// A body that was already dead when the client first saw it. Actor.Recv_DeadOnArrival
    /// (client/augmentations/actor.pyo, first=3148) is AnnounceDeath with doDeathFX = 0: the dead
    /// control state, the cancelled action, StopTracking, HideWeapons, StopPersistantEffects and the
    /// ACTOR_STATE_DEAD event that targeting, the radar and the overhead nameplate all listen for -
    /// without the death animation and flash, which belong to a death that was watched.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CorpseIntroductionTests
    {
        private const EntityClasses TestClass = (EntityClasses)9000031;

        [DataTestMethod]
        [DataRow(CharacterState.Dead, true)]
        [DataRow(CharacterState.Normal, false)]
        public void OnlyACorpseIsIntroducedAsOneAndAlwaysBeforeItsActorInfo(CharacterState state, bool expected)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[TestClass] = new EntityClass((uint)TestClass, "test thrax", 0, 1, new List<AugmentationType>(), true);
            var creature = new Creature
            {
                EntityClass = TestClass,
                State = state,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            try
            {
                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                CreatureManager.Instance.CreateCreatureOnClient(client, creature);

                var data = Drain(client).OfType<CallMethodMessage>().Select(message => message.Packet)
                    .OfType<CreatePhysicalEntityPacket>().Single().EntityData;
                var dead = data.OfType<DeadOnArrivalPacket>().SingleOrDefault();
                Assert.AreEqual(expected, dead != null);
                if (!expected)
                    return;

                // Recv_DeadOnArrival announces only `if not self.IsDead()`, and ActorInfo's stateIds
                // carry CharacterState.Dead into SetCurrentStateIds -> TransitionTo(dead). After
                // ActorInfo the guard is already true and the whole announcement is swallowed.
                Assert.IsTrue(data.IndexOf(dead) < data.FindIndex(packet => packet is ActorInfoPacket));
                Assert.IsFalse(dead.CanRevive, "nothing on this server revives a creature");
            }
            finally
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                classes.Remove(TestClass);
            }
        }

        private static List<IClientMessage> Drain(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(client);
            var messages = new List<IClientMessage>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                messages.Add(protocol.Message);
            return messages;
        }
    }
}
