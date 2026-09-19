using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Game.Handlers;
using Rasa.Managers;
using Rasa.Packets;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.MapChannel.Server.PerformRecovery;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// A class damage ability from the client's action tables, end to end: Shrapnel (178) level 1, whose client row
    /// is windup 500 ms, recovery 666 ms, no reuse, 25 power, 180-240 damage to everything within 6 m of the soldier.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DamageAbilityLifecycleTests
    {
        private long _now;
        private ActorActionManager _actions;
        private Client _client;
        private MapChannel _map;
        private readonly List<Creature> _creatures = new List<Creature>();

        [TestInitialize]
        public void Initialize()
        {
            _now = 0;
            _actions = new ActorActionManager(() => _now);
            _map = new MapChannel { MapInfo = new MapInfo(1220, "Test", 1, 1), ClientList = new List<Client>() };
            var player = new Manifestation
            {
                Class = 2, Level = 1, State = CharacterState.Normal, MapContextId = 1220, MapChannel = _map,
                Cells = new uint[1, 1], MovementSpeed = 1, Position = Vector3.Zero
            };
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 1000, 1000, 1000, 0, 1);
            player.Skills[(SkillId)25] = new SkillsData((SkillId)25, 178, 1);
            player.Logos.Add(24);
            player.Logos.Add(1);
            _client = new Client(null, new ClientPacketHandler()) { Player = player, State = ClientState.Ingame };
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _client } };

            var shrapnel = new ActionInfo { ActionId = (ActionId)178, Name = "AA_SOLDIER_SHRAPNEL", Module = "abilities.shrapnel" };
            var level = new ActionLevelInfo { ActionId = (ActionId)178, Level = 1, WindupMs = 500, RecoveryMs = 666, MaxRange = 0, ReuseMs = 0 };
            level.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 25 });
            level.Properties[AbilityProperty.DamageScaleType] = 2;
            level.Properties[AbilityProperty.RadiusAroundSource] = 6;
            level.Properties[AbilityProperty.DamageAmountMin] = 180;
            level.Properties[AbilityProperty.DamageAmountMax] = 240;
            shrapnel.Levels[1] = level;
            ActionTableManager.Instance.Add(shrapnel);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ActionTableManager.Instance.Remove((ActionId)178);
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }
        }

        [TestMethod]
        public void ShrapnelHitsEveryHostileWithinItsRadiusOnceTheWindupHasRun()
        {
            var near = Creature(new Vector3(3, 0, 0), Factions.Bane);
            var alsoNear = Creature(new Vector3(0, 0, -5), Factions.Bane);
            var friendly = Creature(new Vector3(2, 0, 0), Factions.AFS);
            var far = Creature(new Vector3(9, 0, 0), Factions.Bane);

            Assert.IsTrue(ActionTableManager.Instance.TryGetDirectDamage((ActionId)178, 1, out _, out _));
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, new RequestPerformAbilityPacket { ActionId = (ActionId)178, ActionArgId = 1 }),
                "an area-around-source ability needs no target");
            Assert.AreEqual(1, Drain().OfType<PerformWindupPacket>().Count());

            Advance(499);
            Assert.AreEqual(1000, Power, "nothing is paid before the windup has run");
            Assert.AreEqual(100000, Health(near));

            Advance(500);
            Assert.AreEqual(975, Power);
            foreach (var hit in new[] { near, alsoNear })
                Assert.IsTrue(100000 - Health(hit) is >= 180 and <= 240, "damage from the row, unscaled at level 1");
            Assert.AreEqual(100000, Health(friendly), "an AFS creature is not hostile");
            Assert.AreEqual(100000, Health(far), "outside the 6 m radius");

            var recovery = Drain().OfType<DamageAbilityRecovery>().Single();
            CollectionAssert.AreEquivalent(new[] { near.EntityId, alsoNear.EntityId }, recovery.HitEntities.ToArray());
            Assert.AreEqual(recovery.HitEntities.Count, recovery.Hits.Count, "DamageBase.DoAbility indexes hitdata by hit");
        }

        [TestMethod]
        public void WithoutThePowerOrTheSkillItIsRefused()
        {
            _client.Player.Attributes[Attributes.Power].Current = 24;
            Assert.IsFalse(_actions.TryStartDamageAbility(_client, new RequestPerformAbilityPacket { ActionId = (ActionId)178, ActionArgId = 1 }));
            _client.Player.Attributes[Attributes.Power].Current = 1000;
            _client.Player.Skills.Remove((SkillId)25);
            Assert.IsFalse(_actions.TryStartDamageAbility(_client, new RequestPerformAbilityPacket { ActionId = (ActionId)178, ActionArgId = 1 }));
        }

        [TestMethod]
        public void LightningKeepsItsOwnLifecycle()
        {
            ActionTableManager.Instance.Add(new ActionInfo
            {
                ActionId = ActionId.AaRecruitLightning, Module = "abilities.lightning",
                Levels = { [1] = new ActionLevelInfo { Properties = { [AbilityProperty.DamageAmountMin] = 180 } } }
            });
            try
            {
                Assert.IsFalse(ActionTableManager.Instance.TryGetDirectDamage(ActionId.AaRecruitLightning, 1, out _, out _));
            }
            finally
            {
                ActionTableManager.Instance.Remove(ActionId.AaRecruitLightning);
            }
        }

        [TestMethod]
        public void AConeReachesWhatIsInFrontWithinTheAbilitysRange()
        {
            // Tectonic Strike (229) level 1: CONE_RADIUS 10 is the cone's angle in degrees (client/targeting.py turns it
            // into radians), and the reach is the ability's range, 20 m.
            var strike = new ActionInfo { ActionId = (ActionId)178, Module = "abilities.tectonicstrike" };
            var level = new ActionLevelInfo { ActionId = (ActionId)178, Level = 1, WindupMs = 333, RecoveryMs = 333, MaxRange = 20 };
            level.Properties[AbilityProperty.ConeRadius] = 10;
            level.Properties[AbilityProperty.DamageAmountMin] = 180;
            level.Properties[AbilityProperty.DamageAmountMax] = 210;
            strike.Levels[1] = level;
            ActionTableManager.Instance.Add(strike);
            _client.Player.Rotation = 0;   // facing +Z

            var ahead = Creature(new Vector3(0.5f, 0, 15), Factions.Bane);     // 1.9 degrees off, 15 m
            var wide = Creature(new Vector3(8, 0, 8), Factions.Bane);          // 45 degrees off
            var beyond = Creature(new Vector3(0, 0, 25), Factions.Bane);       // straight ahead, past 20 m
            var behind = Creature(new Vector3(0, 0, -3), Factions.Bane);

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, new RequestPerformAbilityPacket { ActionId = (ActionId)178, ActionArgId = 1, ClientYaw = 0 }));
            Advance(333);
            Assert.IsTrue(Health(ahead) < 100000);
            foreach (var missed in new[] { wide, beyond, behind })
                Assert.AreEqual(100000, Health(missed));
        }

        [TestMethod]
        public void AStunHoldsTheCreatureAndItsEffectExpires()
        {
            var target = Creature(new Vector3(3, 0, 0), Factions.Bane);
            var row = new ActionLevelInfo();
            row.Properties[AbilityProperty.StunChance] = 50;
            row.Properties[AbilityProperty.StunDuration] = 3;

            AbilityEffects.Apply(_map, _client.Player, target, row, percent => percent == 50);
            Assert.IsTrue(target.StunnedUntil > System.Environment.TickCount64 + 2000);
            var stun = target.ActiveEffects.Values.Single();
            Assert.AreEqual(AbilityEffects.StunEffectType, stun.TypeId);
            Assert.AreEqual(3000, stun.Duration);
            Assert.IsTrue(Drain().OfType<GameEffectAttachedPacket>().Any(p => p.EffectTypeId == 86));

            GameEffectManager.Instance.DoWork(_map, 3000);
            Assert.AreEqual(0, target.ActiveEffects.Count);
            Assert.IsFalse(_map.CreaturesWithEffects.Contains(target));
            Assert.IsTrue(Drain().OfType<GameEffectDetachedPacket>().Any());
        }

        [TestMethod]
        public void AFailedStunRollDoesNothing()
        {
            var target = Creature(new Vector3(3, 0, 0), Factions.Bane);
            var row = new ActionLevelInfo();
            row.Properties[AbilityProperty.StunChance] = 50;
            row.Properties[AbilityProperty.StunDuration] = 3;
            AbilityEffects.Apply(_map, _client.Player, target, row, percent => false);
            Assert.AreEqual(0L, target.StunnedUntil);
            Assert.AreEqual(0, target.ActiveEffects.Count);
        }

        private Creature Creature(Vector3 position, Factions faction)
        {
            var creature = new Creature
            {
                MapContextId = 1220, Level = 1, Faction = faction, State = CharacterState.Normal, Cells = new uint[1, 1],
                Position = position, MapChannel = _map
            };
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100000, 100000, 100000, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            _map.MapCellInfo.Cells[0].CreatureList.Add(creature);
            _creatures.Add(creature);
            return creature;
        }

        private int Power => _client.Player.Attributes[Attributes.Power].Current;
        private static int Health(Creature creature) => creature.Attributes[Attributes.Health].Current;

        private void Advance(long now)
        {
            var delta = now - _now;
            _now = now;
            _actions.DoWork(_map, delta);
        }

        private List<PythonPacket> Drain()
        {
            var queue = (PacketQueue)typeof(Client).GetField("_packetQueue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_client);
            var packets = new List<PythonPacket>();
            while (queue.PopOutgoing() is ProtocolPacket packet)
                packets.Add(((CallMethodMessage)packet.Message).Packet);
            return packets;
        }
    }
}
