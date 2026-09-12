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
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    [TestClass]
    public class SprintTests
    {
        [DataTestMethod]
        [DataRow(1, 30, 1.2, 3600000)]
        [DataRow(2, 27, 1.3, 3600000)]
        [DataRow(3, 25, 1.4, 3600000)]
        [DataRow(4, 20, 1.5, 3600000)]
        [DataRow(5, 18, 1.6, 5000000)]
        public void RequestRecoveryAppliesOriginalRankCostsAndMovement(int rank, int cost, double movement, int capMs)
        {
            var client = CreateClient(1000);
            Manager().RequestPerformAbility(client, new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitSprint, ActionArgId = rank, Target = client.Player.EntityId
            });
            Assert.AreEqual(1, client.Player.MapChannel.PerformRecovery.Count);
            Assert.AreEqual(1000, Chi(client));
            ActorActionManager.Instance.DoWork(client.Player.MapChannel, 0);
            Assert.AreEqual(0, client.Player.MapChannel.PerformRecovery.Count);
            Assert.AreEqual(1000 - cost, Chi(client));
            Assert.AreEqual(movement, client.Player.MovementSpeed, 0.000001);
            var effect = client.Player.ActiveEffects.Values.Single();
            Assert.AreEqual(GameEffectManager.SprintEffectType, effect.TypeId);
            Assert.AreEqual(capMs, effect.Duration);
            Assert.AreEqual((uint)rank, effect.EffectLevel);

            var packets = DrainPackets(client);
            var attached = packets.OfType<GameEffectAttachedPacket>().Single();
            Assert.IsNull(attached.Duration);
            Assert.IsNull(attached.DamageType);
            Assert.IsNull(attached.AttrId);
            CollectionAssert.AreEqual(new[] { 1.0 }, attached.EffectArguments);
            Assert.AreEqual(client.Player.EntityId, packets.OfType<UpdateChiPacket>().Single().WhoId);
        }

        [TestMethod]
        public void InsufficientChiRejectsBeforeQueueAndDuplicateDoesNotChargeOrStack()
        {
            var client = CreateClient(29);
            var request = new RequestPerformAbilityPacket { ActionId = ActionId.AaRecruitSprint, ActionArgId = 1 };
            Manager().RequestPerformAbility(client, request);
            Assert.AreEqual(0, client.Player.MapChannel.PerformRecovery.Count);
            Assert.AreEqual(29, Chi(client));
            Assert.AreEqual(1, DrainPackets(client).OfType<UserActionFailedPacket>().Count());

            client.Player.Attributes[Attributes.Chi].Current = 100;
            Assert.IsTrue(GameEffectManager.Instance.TryAttachSprint(client.Player.MapChannel, client.Player, 1));
            DrainPackets(client);
            Manager().RequestPerformAbility(client, request);
            Assert.IsFalse(GameEffectManager.Instance.TryAttachSprint(client.Player.MapChannel, client.Player, 5));
            Assert.AreEqual(0, client.Player.MapChannel.PerformRecovery.Count);
            Assert.AreEqual(70, Chi(client));
            Assert.AreEqual(1, client.Player.ActiveEffects.Count);
            Assert.AreEqual(1.2, client.Player.MovementSpeed, 0.000001);
        }

        [TestMethod]
        public void RecoveryRechecksChiAfterOtherActionsHaveSpentIt()
        {
            var client = CreateClient(30);
            Manager().RequestPerformAbility(client, new RequestPerformAbilityPacket
            {
                ActionId = ActionId.AaRecruitSprint, ActionArgId = 1
            });
            client.Player.Attributes[Attributes.Chi].Current = 29;
            ActorActionManager.Instance.DoWork(client.Player.MapChannel, 0);
            Assert.AreEqual(0, client.Player.ActiveEffects.Count);
            Assert.AreEqual(29, Chi(client));
            Assert.AreEqual(1, DrainPackets(client).OfType<UserActionFailedPacket>().Count());
        }

        [TestMethod]
        public void UpkeepUsesAllElapsedMillisecondsAndStopsWhenNextPaymentIsUnavailable()
        {
            var client = CreateClient(100);
            var effects = GameEffectManager.Instance;
            var map = client.Player.MapChannel;
            client.Player.MovementSpeed = 0.8;
            Assert.IsTrue(effects.TryAttachSprint(map, client.Player, 1));
            Assert.AreEqual(0.96, client.Player.MovementSpeed, 0.000001);
            effects.DoWork(map, 1999);
            Assert.AreEqual(70, Chi(client));
            effects.DoWork(map, 1);
            Assert.AreEqual(40, Chi(client));
            effects.DoWork(map, 4000); // Due ticks at 4000 and 6000; only the first is affordable.
            Assert.AreEqual(10, Chi(client));
            Assert.AreEqual(0, client.Player.ActiveEffects.Count);
            Assert.AreEqual(0.8, client.Player.MovementSpeed, 0.000001);
            Assert.AreEqual(1, DrainPackets(client).OfType<GameEffectDetachedPacket>().Count());
            effects.DoWork(map, 10000);
            Assert.AreEqual(10, Chi(client));
            Assert.AreEqual(0, DrainPackets(client).Count);
        }

        [TestMethod]
        public void OwnerCanCancelSprintButCannotRemoveAnotherOrUnsupportedEffect()
        {
            var first = CreateClient(100);
            var second = CreateClient(100);
            var effects = GameEffectManager.Instance;
            Assert.IsTrue(effects.TryAttachSprint(first.Player.MapChannel, first.Player, 1));
            var id = first.Player.ActiveEffects.Keys.Single();
            Assert.IsFalse(effects.TryDetachRequestedEffect(second.Player.MapChannel, second.Player, id));
            first.Player.ActiveEffects[999] = new GameEffect { EffectId = 999, TypeId = 123, Duration = 1000 };
            Assert.IsFalse(effects.TryDetachRequestedEffect(first.Player.MapChannel, first.Player, 999));
            Manager().RequestDetachGameEffect(first, new RequestDetachGameEffectPacket { EffectId = id });
            Assert.IsFalse(first.Player.ActiveEffects.ContainsKey(id));
            Assert.IsTrue(first.Player.ActiveEffects.ContainsKey(999));
            Assert.AreEqual(1.0, first.Player.MovementSpeed);
            Assert.IsFalse(effects.TryDetachRequestedEffect(first.Player.MapChannel, first.Player, id));
        }

        [TestMethod]
        public void LongCapAndSimultaneousExpirationsDoNotLoseTimeOrSkipEffects()
        {
            var client = CreateClient(int.MaxValue);
            var effects = GameEffectManager.Instance;
            Assert.IsTrue(effects.TryAttachSprint(client.Player.MapChannel, client.Player, 5));
            client.Player.ActiveEffects[1000] = new GameEffect { EffectId = 1000, TypeId = 123, Duration = 5000000 };
            client.Player.ActiveEffects[1001] = new GameEffect { EffectId = 1001, TypeId = 124, Duration = 5000000 };
            effects.DoWork(client.Player.MapChannel, 5000000);
            Assert.AreEqual(0, client.Player.ActiveEffects.Count);
            // The initial payment plus 2499 intervals strictly before the cap.
            Assert.AreEqual(int.MaxValue - 2500 * 18, Chi(client));
            Assert.AreEqual(3, DrainPackets(client).OfType<GameEffectDetachedPacket>().Count());
        }

        [TestMethod]
        public void EffectArgumentsAreFlatScalarsAndTooltipDurationUsesSeconds()
        {
            var packet = new GameEffectAttachedPacket
            {
                EffectTypeId = 247, EffectId = 19, EffectLevel = 1,
                SourceId = 0x100000002UL, Announced = true, EffectArguments = new[] { 1.0 }
            };
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(7, reader.ReadTuple());
            Assert.AreEqual(247, reader.ReadInt());
            Assert.AreEqual(19, reader.ReadInt());
            Assert.AreEqual(1U, reader.ReadUInt());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
            Assert.IsTrue(reader.ReadBool());
            Assert.AreEqual(0, reader.ReadDictionary());
            Assert.AreEqual(1.0, reader.ReadDouble());
            Assert.AreEqual(stream.Length, stream.Position);

            stream.SetLength(0);
            stream.Position = 0;
            packet.Duration = 2.5;
            packet.Write(writer);
            stream.Position = 0;
            reader.ReadTuple(); reader.ReadInt(); reader.ReadInt(); reader.ReadUInt();
            reader.ReadULong(); reader.ReadBool();
            Assert.AreEqual(1, reader.ReadDictionary());
            Assert.AreEqual("duration", reader.ReadString());
            Assert.AreEqual(2.5, reader.ReadDouble());
        }

        [TestMethod]
        public void ChiUpdateRetainsTheFullSourceEntityId()
        {
            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            new UpdateChiPacket(new ActorAttributes(Attributes.Chi, 1000, 1000, 970, 0, 1), 0x100000002UL).Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(970, reader.ReadInt());
            Assert.AreEqual(1000, reader.ReadInt());
            Assert.AreEqual(0, reader.ReadInt());
            Assert.AreEqual(0x100000002UL, reader.ReadULong());
        }

        private static int Chi(Client client) => client.Player.Attributes[Attributes.Chi].Current;

        private static Client CreateClient(int chi)
        {
            var player = new Manifestation { Class = 1, Level = 1, State = CharacterState.Normal, MovementSpeed = 1.0, Cells = new uint[1, 1] };
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 1000, 1000, chi, 0, 1);
            player.Skills[(SkillId)165] = new SkillsData((SkillId)165, 401, 5);
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame, Player = player };
            var map = new MapChannel { ClientList = new List<Client> { client } };
            player.MapChannel = map;
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { client } };
            return client;
        }

        private static ManifestationManager Manager() => (ManifestationManager)Activator.CreateInstance(
            typeof(ManifestationManager), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { null }, null);

        private static List<PythonPacket> DrainPackets(Client client)
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket protocol)
                packets.Add(((CallMethodMessage)protocol.Message).Packet);
            return packets;
        }
    }
}
