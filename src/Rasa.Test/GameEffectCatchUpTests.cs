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
    /// The bulk GameEffects catch-up. PhysicalEntity.Recv_GameEffects(effectDataSeq) is
    /// `for data in effectDataSeq: effect = apply(self.AttachGameEffect, data); effect.AnnounceAttach(self)`,
    /// so each element is AttachGameEffect's own tuple - the GameEffectAttached one without the announce flag.
    /// Nothing ever sent it, so a buff or debuff was invisible to anyone who did not watch it land.
    /// (verify/dis/trpython-client-physicalentity.pyo.dis :: Recv_GameEffects first=725,
    ///  AttachGameEffect first=488, Recv_GameEffectAttached first=601)
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GameEffectCatchUpTests
    {
        [TestMethod]
        public void ABulkElementIsTheAttachedTupleWithoutTheAnnounceFlag()
        {
            var attached = new GameEffectAttachedPacket
            {
                EffectTypeId = 247, EffectId = 19, EffectLevel = 3,
                SourceId = 0x100000002UL, Announced = true, EffectArguments = new[] { 1.0 }
            };

            var bulk = Read(new GameEffectsPacket(new[] { attached }));
            Assert.AreEqual(1, bulk.ReadTuple());
            Assert.AreEqual(1, bulk.ReadList());
            // Five named arguments plus the one variadic extra - the announce bool is gone.
            Assert.AreEqual(6, bulk.ReadTuple());
            Assert.AreEqual(247, bulk.ReadInt());
            Assert.AreEqual(19, bulk.ReadInt());
            Assert.AreEqual(3U, bulk.ReadUInt());
            Assert.AreEqual(0x100000002UL, bulk.ReadULong());
            Assert.AreEqual(0, bulk.ReadDictionary());
            Assert.AreEqual(1.0, bulk.ReadDouble());

            // The single-effect form of the same effect still carries it, in the sixth position.
            var single = Read(attached);
            Assert.AreEqual(7, single.ReadTuple());
            single.ReadInt(); single.ReadInt(); single.ReadUInt(); single.ReadULong();
            Assert.IsTrue(single.ReadBool());
        }

        [TestMethod]
        public void TheCountdownIsWhatIsLeftOfTheEffectNotWhatItStartedWith()
        {
            // BaseGameEffect.SetTooltipDict (first=482) sets __expireTime = gameclient.Time() + duration, so the
            // tooltip's duration is measured from the moment the packet arrives.
            var client = CreateClient();
            var effects = GameEffectManager.Instance;
            effects.AttachEffect(client.Player.MapChannel, client.Player, 235, 1, 30000, client.Player.EntityId,
                true, new Dictionary<string, double> { { "dmgMod", 15.5 } });
            effects.DoWork(client.Player.MapChannel, 12000);

            var reader = Read(GameEffectManager.CatchUpPacket(client.Player));
            reader.ReadTuple();
            Assert.AreEqual(1, reader.ReadList());
            reader.ReadTuple(); reader.ReadInt(); reader.ReadInt(); reader.ReadUInt(); reader.ReadULong();
            var tooltip = ReadTooltip(reader);
            Assert.AreEqual(18.0, tooltip["duration"]);
            Assert.AreEqual(15.5, tooltip["dmgMod"]);

            // The effect the client already holds was told 30 seconds: this is a second, later reading of the
            // same announcement, not a rewrite of it.
            Assert.AreEqual(30.0, client.Player.ActiveEffects.Values.Single().Announcement.Duration);
        }

        [TestMethod]
        public void AnOpenEndedEffectKeepsNoCountdownAndAServerTimerIsNeverMentioned()
        {
            var client = CreateClient();
            var effects = GameEffectManager.Instance;
            // Sprint's announcement is deliberately open-ended: the authored description has no duration, and the
            // 3600 s figure is only the server's internal cap.
            Assert.IsTrue(effects.TryAttachSprint(client.Player.MapChannel, client.Player, 1));
            effects.AttachServerTimer(client.Player.MapChannel, client.Player, 5000, null);
            Assert.AreEqual(2, client.Player.ActiveEffects.Count);

            var reader = Read(GameEffectManager.CatchUpPacket(client.Player));
            reader.ReadTuple();
            Assert.AreEqual(1, reader.ReadList());
            Assert.AreEqual(6, reader.ReadTuple());
            Assert.AreEqual(GameEffectManager.SprintEffectType, reader.ReadInt());
            reader.ReadInt(); reader.ReadUInt(); reader.ReadULong();
            var tooltip = ReadTooltip(reader);
            Assert.IsFalse(tooltip.ContainsKey("duration"));
            Assert.AreEqual(1.0, tooltip["isActive"]);
            Assert.AreEqual(1.0, tooltip["isBuff"]);
        }

        [TestMethod]
        public void NothingToSayMeansNoPacketAtAll()
        {
            var client = CreateClient();
            Assert.IsNull(GameEffectManager.CatchUpPacket(client.Player));

            GameEffectManager.Instance.AttachServerTimer(client.Player.MapChannel, client.Player, 5000, null);
            Assert.IsNull(GameEffectManager.CatchUpPacket(client.Player), "a server-side timer has no client half");

            // An effect with nothing left of it is left out; the expiry tick is about to detach it anyway.
            var spent = GameEffectManager.Instance.AttachEffect(client.Player.MapChannel, client.Player, 235, 1,
                1000, client.Player.EntityId, true, null);
            spent.EffectTime = 1000;
            Assert.IsNull(GameEffectManager.CatchUpPacket(client.Player));
        }

        [TestMethod]
        public void ACreatureBringsItsEffectsWithItWhenItComesIntoView()
        {
            const EntityClasses thraxClass = (EntityClasses)9000031;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[thraxClass] = new EntityClass((uint)thraxClass, "test thrax", 0, 1, new List<AugmentationType>(), true);
            var burning = new Creature { EntityClass = thraxClass, AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };
            var untouched = new Creature { EntityClass = thraxClass, AppearanceData = new Dictionary<EquipmentData, AppearanceData>() };

            foreach (var creature in new[] { burning, untouched })
            {
                EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
                EntityManager.Instance.RegisterCreature(creature);
            }

            try
            {
                var map = new MapChannel();
                map.MapCellInfo.Cells[0] = new MapCell();
                GameEffectManager.Instance.AttachEffect(map, burning, 219, 1, 8000, 0x99UL, false, null);

                var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame };
                CreatureManager.Instance.CreateCreatureOnClient(client, burning);
                CreatureManager.Instance.CreateCreatureOnClient(client, untouched);

                var created = Drain(client).OfType<CreatePhysicalEntityPacket>().ToList();
                Assert.AreEqual(2, created.Count);
                var catchUp = created[0].EntityData.OfType<GameEffectsPacket>().Single();
                Assert.AreEqual(219, catchUp.Effects.Single().EffectTypeId);
                Assert.IsFalse(created[1].EntityData.OfType<GameEffectsPacket>().Any());

                // After the appearance it hangs its visuals on, and after ActorInfo, which carries the state.
                var data = created[0].EntityData;
                Assert.IsTrue(data.IndexOf(catchUp) > data.FindIndex(packet => packet is AppearanceDataPacket));
                Assert.IsTrue(data.IndexOf(catchUp) > data.FindIndex(packet => packet is ActorInfoPacket));
            }
            finally
            {
                foreach (var creature in new[] { burning, untouched })
                {
                    EntityManager.Instance.UnregisterCreature(creature.EntityId);
                    EntityManager.Instance.UnregisterEntity(creature.EntityId);
                }
                classes.Remove(thraxClass);
            }
        }

        private static Dictionary<string, double> ReadTooltip(PythonReader reader)
        {
            var count = reader.ReadDictionary();
            var tooltip = new Dictionary<string, double>();

            for (var entry = 0; entry < count; entry++)
            {
                var name = reader.ReadString();
                // The packet narrows a whole number to an int and a bool to a bool; read either back as a double.
                tooltip[name] = name.StartsWith("is") ? (reader.ReadBool() ? 1.0 : 0.0) : reader.ReadDouble();
            }

            return tooltip;
        }

        private static PythonReader Read(PythonPacket packet)
        {
            Assert.IsNotNull(packet);
            var stream = new MemoryStream();
            packet.Write(new PythonWriter(new BinaryWriter(stream)));
            stream.Position = 0;
            return new PythonReader(new BinaryReader(stream));
        }

        private static Client CreateClient()
        {
            var player = new Manifestation { Class = 1, Level = 1, State = CharacterState.Normal, MovementSpeed = 1.0, Cells = new uint[1, 1] };
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 1000, 1000, 1000, 0, 1);
            var client = new Client(null, new ClientPacketHandler()) { State = ClientState.Ingame, Player = player };
            var map = new MapChannel { ClientList = new List<Client> { client } };
            player.MapChannel = map;
            map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { client } };
            return client;
        }

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
