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
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// The two tier-2 abilities that resolve by a game effect, on their client rows: Specialist's Ruin (162, a damage
    /// over time - DECAY 82) and Soldier's Rage (307, a toggled damage and resistance buff - RAGESOURCE 236 / RAGE 235).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EffectAbilityTests
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
                Level = 1, State = CharacterState.Normal, MapContextId = 1220, MapChannel = _map,
                Cells = new uint[1, 1], MovementSpeed = 1, Position = Vector3.Zero
            };
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 1000, 1000, 1000, 0, 1);
            player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, 1000, 1000, 1000, 0, 1);
            _client = new Client(null, new ClientPacketHandler()) { Player = player, State = ClientState.Ingame };
            _map.ClientList.Add(_client);
            _map.MapCellInfo.Cells[0] = new MapCell { ClientList = new List<Client> { _client } };
        }

        [TestCleanup]
        public void Cleanup()
        {
            ActionTableManager.Instance.Remove((ActionId)162);
            ActionTableManager.Instance.Remove((ActionId)307);
            foreach (var creature in _creatures)
            {
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
            }
        }

        [TestMethod]
        public void RuinDamagesItsTargetEverySecondForTenSeconds()
        {
            _client.Player.Class = 3;
            _client.Player.Skills[(SkillId)36] = new SkillsData((SkillId)36, 162, 1);
            _client.Player.Logos.AddRange(new uint[] { 6, 28 });
            var row = Row(162, "abilities.decay", 500, 500, 60);
            row.Properties[AbilityProperty.DamageAmountMin] = 60;
            row.Properties[AbilityProperty.DamageAmountMax] = 70;
            row.Properties[AbilityProperty.Duration] = 10;
            row.Properties[AbilityProperty.Interval] = 1;
            row.Properties[AbilityProperty.DamageScaleType] = 2;
            row.Properties[AbilityProperty.DamageType] = 4;
            row.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 20 });
            row.Costs.Add(new ActionCost { Attribute = Attributes.Chi, Amount = 100 });
            var target = Creature(new Vector3(10, 0, 0));

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(162, target.EntityId)));
            Advance(500);
            Assert.AreEqual(980, _client.Player.Attributes[Attributes.Power].Current);
            Assert.AreEqual(900, _client.Player.Attributes[Attributes.Chi].Current);
            var decay = target.ActiveEffects.Values.Single();
            Assert.AreEqual(AbilityEffects.DecayEffectType, decay.TypeId);
            Assert.AreEqual(10000, decay.Duration);
            var attached = Drain().OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual((60d, 70d, 4d, 1d), (attached.TooltipValues["dmgMin"], attached.TooltipValues["dmgMax"],
                attached.TooltipValues["damageType"], attached.TooltipValues["interval"]));

            for (var second = 1; second <= 10; second++)
                GameEffectManager.Instance.DoWork(_map, 1000);
            var ticks = Drain().OfType<GameEffectTickPacket>().ToList();
            Assert.AreEqual(10, ticks.Count, "one tick a second for ten seconds");
            Assert.IsTrue(ticks.All(t => t.EffectId == decay.EffectId && t.Damage.Single().TargetId == target.EntityId));
            var taken = 100000 - target.Attributes[Attributes.Health].Current;
            Assert.IsTrue(taken >= 600 && taken <= 700, $"ten ticks of 60-70, took {taken}");
            Assert.AreEqual(0, target.ActiveEffects.Count, "expired");
        }

        [TestMethod]
        public void RageBuffsTheSoldiersDamageUntilItIsToggledOff()
        {
            _client.Player.Class = 2;
            _client.Player.Skills[(SkillId)147] = new SkillsData((SkillId)147, 307, 3);
            _client.Player.Logos.AddRange(new uint[] { 2, 10 });
            var row = Row(307, "abilities.rage", 400, 500, 0, level: 3);
            row.Properties[AbilityProperty.Duration] = 40;
            row.Properties[AbilityProperty.Interval] = 5;
            row.Properties[AbilityProperty.DamagePercentMin] = 40;
            row.Properties[AbilityProperty.DamagePercentMax] = 40;
            row.Properties[AbilityProperty.ResistModifier] = 10;

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(307, null, 3)));
            Advance(400);
            var effects = _client.Player.ActiveEffects.Values.ToList();
            Assert.IsTrue(effects.Any(e => e.TypeId == AbilityEffects.RageSourceEffectType && e.Duration == 40000));
            var rage = effects.Single(e => e.TypeId == AbilityEffects.RageEffectType);
            Assert.AreEqual((40, 10), (rage.DamageBonusPercent, rage.ResistRating));
            Assert.AreEqual(140, DamageModifiers.Outgoing(_client.Player, 100), "+40% damage");
            Assert.AreEqual(10, DamageModifiers.ResistRating(_client.Player));
            var packets = Drain();
            Assert.AreEqual(_client.Player.EntityId, packets.OfType<GameEffectTickPacket>().Single().TargetIds.Single(),
                "the source's pulse names who it buffed");

            // Asking again ends it (rage.py isToggle), and with it the buff.
            Advance(2000);
            Assert.IsFalse(_actions.TryStartDamageAbility(_client, Request(307, null, 3)));
            Assert.AreEqual(0, _client.Player.ActiveEffects.Count);
            Assert.AreEqual(100, DamageModifiers.Outgoing(_client.Player, 100));
        }

        [TestMethod]
        public void BioAugmentationRaisesTheAttributeUntilItEnds()
        {
            var player = _client.Player;
            player.Class = 7;
            player.Race = Race.Human;
            foreach (Attributes attribute in System.Enum.GetValues(typeof(Attributes)))
                if (!player.Attributes.ContainsKey(attribute))
                    player.Attributes[attribute] = new ActorAttributes(attribute, 0, 0, 0, 0, 0);
            player.Inventory.EquippedInventory.AddRange(new ulong[22]);
            player.Skills[(SkillId)173] = new SkillsData((SkillId)173, 421, 1);
            player.Logos.AddRange(LogosFor(421));
            ManifestationManager.Instance.UpdateStatsValues(_client, true);
            var healthBefore = player.Attributes[Attributes.Health].CurrentMax;

            var row = Row(421, "abilities.bioaugmentation", 300, 300, 20);
            row.Properties[AbilityProperty.EffectModifier] = 30;
            row.Properties[AbilityProperty.EffectDurationMs] = 900000;
            row.Properties[AbilityProperty.AttributeId] = 4;
            row.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 100 });

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(421, null)), "no target named: the biotechnician");
            Advance(300);
            var effect = player.ActiveEffects.Values.Single(e => e.TypeId == AbilityEffects.BioAugmentationEffectType);
            Assert.AreEqual(900000, effect.Duration);
            Assert.AreEqual(healthBefore + 30, player.Attributes[Attributes.Health].CurrentMax, "Health (attribute 4) +30");
            var attached = Drain().OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == 329);
            Assert.AreEqual((4d, 30d), (attached.TooltipValues["attrId"], attached.TooltipValues["amount"]));

            GameEffectManager.Instance.DettachEffect(_map, player, effect);
            Assert.AreEqual(healthBefore, player.Attributes[Attributes.Health].CurrentMax);
        }

        private static IEnumerable<uint> LogosFor(int abilityId)
        {
            var field = typeof(AbilityRequirements).GetField("RequiredLogos", BindingFlags.NonPublic | BindingFlags.Static);
            return ((Dictionary<int, uint[]>)field.GetValue(null))[abilityId];
        }

        private ActionLevelInfo Row(int actionId, string module, int windup, int recovery, int range, uint level = 1)
        {
            var info = new ActionInfo { ActionId = (ActionId)actionId, Module = module };
            var row = new ActionLevelInfo { ActionId = (ActionId)actionId, Level = level, WindupMs = windup, RecoveryMs = recovery, MaxRange = range };
            info.Levels[level] = row;
            ActionTableManager.Instance.Add(info);
            return row;
        }

        private static RequestPerformAbilityPacket Request(int actionId, ulong? target, int level = 1)
            => new RequestPerformAbilityPacket { ActionId = (ActionId)actionId, ActionArgId = level, Target = target };

        private Creature Creature(Vector3 position)
        {
            var creature = new Creature
            {
                MapContextId = 1220, Level = 1, Faction = Factions.Bane, State = CharacterState.Normal, Cells = new uint[1, 1],
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
