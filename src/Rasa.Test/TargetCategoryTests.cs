using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    /// The client will not target an entity it has not been given a category for: targeting.py's
    /// SetDirectTarget returns False when GetTargetCategory() is None, before it sends SetTargetId.
    /// Dynamic objects were never sent one, so the boot camp's practice dummy could not be targeted and
    /// every shot at it resolved against entity 0 (live trace, 2026-09-18).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TargetCategoryTests
    {
        private const ulong DummyEntityId = 0x7a11;
        private const EntityClasses DummyClass = (EntityClasses)29365;

        [TestMethod]
        public void ADestroyableIsIntroducedAsATargetableObject()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var hadClass = classes.TryGetValue(DummyClass, out var previousClass);
            classes[DummyClass] = new EntityClass(29365, "UsableStatelessHumPracticeDummyV01", 48957, 1, new List<AugmentationType>(), true);

            var dummy = new DynamicObject
            {
                EntityId = DummyEntityId,
                EntityClassId = DummyClass,
                Position = new Vector3(1f, 2f, 3f),
                DynamicObjectType = DynamicObjectType.ContentUsable,
                StateId = (UseObjectState)110,
                HitPoints = 100,
                MaxHitPoints = 100
            };
            EntityManager.Instance.RegisterEntity(DummyEntityId, EntityType.Object);
            EntityManager.Instance.RegisterDynamicObject(dummy);

            try
            {
                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                DynamicObjectManager.Instance.CreateDynamicObjectOnClient(client, dummy);

                var create = Drain(client).Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().Single();
                var category = create.EntityData.OfType<TargetCategoryPacket>().SingleOrDefault();
                Assert.IsNotNull(category, "a dynamic object without a target category cannot be targeted by the client");
                Assert.AreEqual(TargetCategory.Object, category.TargetCategory);
                Assert.IsTrue(create.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);

                // And it has to be told it can be damaged: the client's usabledata row for 29365 is all
                // None, and IsDirectTargetable - which a weapon's Hostile target type needs for anything
                // not hostile - returns False without canBeDamaged.
                var damage = create.EntityData.OfType<DamageInfoPacket>().SingleOrDefault();
                Assert.IsNotNull(damage, "a destroyable the client thinks cannot be damaged cannot be targeted by a weapon");
                Assert.IsTrue(damage.CanBeDamaged);
                Assert.AreEqual(100u, damage.TotalHitPoints);
                Assert.AreEqual(100u, damage.CurrentHitPoints);
            }
            finally
            {
                EntityManager.Instance.UnregisterDynamicObject(DummyEntityId);
                EntityManager.Instance.UnregisterEntity(DummyEntityId);
                if (hadClass) classes[DummyClass] = previousClass; else classes.Remove(DummyClass);
            }
        }

        [TestMethod]
        public void AnEscortIsIntroducedAsAnEscortAndNoOtherCreatureIs()
        {
            // client/augmentations/creature.pyo: IsEscort and the overhead escort marker are set only by
            // Recv_UpdateEscortStatus(bIsEscort).
            const EntityClasses rangerClass = (EntityClasses)9000021;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[rangerClass] = new EntityClass((uint)rangerClass, "test ranger", 0, 1, new List<AugmentationType>(), true);
            var escort = new Creature { EntityClass = rangerClass, IsEscort = true, AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
            var bystander = new Creature { EntityClass = rangerClass, AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
            foreach (var creature in new[] { escort, bystander })
            {
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
            }
            try
            {
                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                CreatureManager.Instance.CreateCreatureOnClient(client, escort);
                CreatureManager.Instance.CreateCreatureOnClient(client, bystander);

                var created = Drain(client).Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().ToList();
                Assert.AreEqual(2, created.Count);
                Assert.IsTrue(created[0].EntityData.OfType<UpdateEscortStatusPacket>().Single().IsEscort);
                Assert.IsFalse(created[1].EntityData.OfType<UpdateEscortStatusPacket>().Any());
            }
            finally
            {
                foreach (var creature in new[] { escort, bystander })
                {
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                }
                classes.Remove(rangerClass);
            }
        }

        [TestMethod]
        public void TheClientCategoriesAreTheClientsOwnValues()
        {
            // generated/client/targetdata.pyo
            Assert.AreEqual(0, (int)TargetCategory.Hostile);
            Assert.AreEqual(1, (int)TargetCategory.Friendly);
            Assert.AreEqual(2, (int)TargetCategory.Object);
            Assert.AreEqual(3, (int)TargetCategory.Neutral);
            Assert.AreEqual(4, (int)TargetCategory.Decoration);
        }

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
