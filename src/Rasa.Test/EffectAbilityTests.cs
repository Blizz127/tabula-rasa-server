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

        [TestMethod]
        public void ScourgeDamagesEveryHostileAroundTheCommandoEachPulse()
        {
            _client.Player.Class = 4;
            _client.Player.Skills[(SkillId)164] = new SkillsData((SkillId)164, 380, 1);
            _client.Player.Logos.AddRange(LogosFor(380));
            var row = Row(380, "abilities.scourge", 500, 500, 0);
            row.Properties[AbilityProperty.Duration] = 15;
            row.Properties[AbilityProperty.DamageAmountMin] = 23;
            row.Properties[AbilityProperty.DamageAmountMax] = 30;
            row.Properties[AbilityProperty.EffectRadius] = 6;
            row.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 30 });
            var near = Creature(new Vector3(4, 0, 0));
            var far = Creature(new Vector3(9, 0, 0));

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(380, null)), "a self ability takes no target");
            Advance(500);
            Assert.AreEqual(15000, _client.Player.ActiveEffects.Values.Single(e => e.TypeId == AbilityEffects.ScourgeEffectType).Duration);
            Drain();
            for (var second = 0; second < 15; second++)
                GameEffectManager.Instance.DoWork(_map, 1000);
            var announced = Drain().OfType<CallGameEffectMethodPacket>().ToList();
            Assert.AreEqual(15, announced.Count);
            Assert.IsTrue(announced.All(p => p.MethodName == "AnnounceDamage" && p.Damage.Single().TargetId == near.EntityId));
            var taken = 100000 - near.Attributes[Attributes.Health].Current;
            Assert.IsTrue(taken >= 15 * 23 && taken <= 15 * 30, $"fifteen pulses of 23-30, took {taken}");
            Assert.AreEqual(100000, far.Attributes[Attributes.Health].Current, "outside the 6 m radius");
        }

        [TestMethod]
        public void ShieldExtenderAbsorbsItsShareUntilThePoolIsSpent()
        {
            _client.Player.Class = 6;
            _client.Player.Skills[(SkillId)174] = new SkillsData((SkillId)174, 446, 1);
            _client.Player.Logos.AddRange(LogosFor(446));
            var row = Row(446, "abilities.shieldextender", 400, 500, 20);
            row.Properties[AbilityProperty.EffectRadius] = 6;
            row.Properties[AbilityProperty.EffectModifier] = 15;
            row.Properties[AbilityProperty.EffectDurationMs] = 45000;
            row.Properties[AbilityProperty.EffectDamageMax] = 120;
            row.Costs.Add(new ActionCost { Attribute = Attributes.Power, Amount = 70 });

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(446, null)), "the sapper may shield themself");
            Advance(400);
            var player = _client.Player;
            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.ShieldExtenderSourceType));
            Assert.IsTrue(player.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.ShieldExtenderShieldedType));

            Assert.AreEqual(85, DamageModifiers.ThroughShield(player, 100), "15% of the hit absorbed");
            for (var hit = 0; hit < 7; hit++)
                DamageModifiers.ThroughShield(player, 100);                 // 15 more each: 120 in all after 8 hits
            Assert.AreEqual(0, player.ActiveEffects.Count, "a spent shield breaks, and its protection with it");
            Assert.AreEqual(100, DamageModifiers.ThroughShield(player, 100));
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        public void ReconstructionHealsTheBiotechnicianAndHurtsTheEnemiesAround(int pump)
        {
            var player = _client.Player;
            player.Class = 7;
            player.Skills[(SkillId)35] = new SkillsData((SkillId)35, 188, pump);
            player.Logos.AddRange(LogosFor(188));
            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 500, 0, 0);
            var row = Row(188, "abilities.reconstruction", 300, 300, 0, (uint)pump);
            row.Properties[AbilityProperty.RadiusAroundSource] = 15;
            row.Properties[AbilityProperty.DamageScaleType] = 2;
            row.Properties[AbilityProperty.DamageType] = 4;
            if (pump == 1)
            {
                row.Properties[AbilityProperty.DamageAmountMin] = 90;
                row.Properties[AbilityProperty.DamageAmountMax] = 120;
                row.Properties[AbilityProperty.HealAmountMin] = 100;
                row.Properties[AbilityProperty.HealAmountMax] = 100;
            }
            else
            {
                row.Properties[AbilityProperty.DamageAmountMin] = 36;
                row.Properties[AbilityProperty.DamageAmountMax] = 58;
                row.Properties[AbilityProperty.HealAmountMin] = 40;
                row.Properties[AbilityProperty.HealAmountMax] = 60;
                row.Properties[AbilityProperty.Duration] = 15;
                row.Properties[AbilityProperty.Interval] = 3;
            }
            var enemy = Creature(new Vector3(10, 0, 0));
            var far = Creature(new Vector3(20, 0, 0));

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(188, null, pump)));
            Advance(300);
            var recovery = Drain().OfType<Packets.MapChannel.Server.PerformRecovery.EffectListRecovery>().Single();
            CollectionAssert.AreEquivalent(new[] { (player.EntityId, AbilityEffects.ReconstructionHelpType), (enemy.EntityId, AbilityEffects.ReconstructionHarmType) },
                recovery.Entries.ToArray());
            if (pump == 1)
            {
                Assert.AreEqual(600, player.Attributes[Attributes.Health].Current, "healed 100 at once");
                Assert.IsTrue(100000 - enemy.Attributes[Attributes.Health].Current is >= 90 and <= 120);
            }
            else
            {
                for (var second = 0; second < 15; second++)
                    GameEffectManager.Instance.DoWork(_map, 1000);
                var healed = player.Attributes[Attributes.Health].Current - 500;
                Assert.IsTrue(healed >= 5 * 40 && healed <= 5 * 60, $"five pulses of 40-60, healed {healed}");
                var taken = 100000 - enemy.Attributes[Attributes.Health].Current;
                Assert.IsTrue(taken >= 5 * 36 && taken <= 5 * 58, $"five pulses of 36-58, took {taken}");
            }
            Assert.AreEqual(100000, far.Attributes[Attributes.Health].Current, "outside the 15 m radius");
        }

        private void Ranger(int pump)
        {
            _client.Player.Class = 5;
            _client.Player.Skills[(SkillId)54] = new SkillsData((SkillId)54, 10000005, pump);
            _client.Player.Logos.AddRange(LogosFor(10000005));
        }

        [TestMethod]
        public void TacticalEvasionPumpOneMakesTheEnemiesAroundForgetTheRanger()
        {
            Ranger(1);
            var row = Row(10000005, "abilities.tacticalevasion", 400, 400, 60);
            row.Properties[AbilityProperty.RadiusAroundSource] = 10;
            row.Properties[AbilityProperty.EffectDurationMs] = 5000;
            var hunter = Creature(new Vector3(5, 0, 0));
            BehaviorManager.Instance.SetActionFighting(hunter, _client.Player.EntityId);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, hunter.Controller.CurrentAction);

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(10000005, null)));
            Advance(400);
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, hunter.Controller.CurrentAction, "its hate is cleared");
            Assert.IsTrue(hunter.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.MagFlashType));
            CollectionAssert.AreEqual(new[] { hunter.EntityId },
                Drain().OfType<Packets.MapChannel.Server.PerformRecovery.IdListRecovery>().Single().Ids.ToArray());
        }

        [TestMethod]
        public void TacticalEvasionPumpTwoScreensTheRangerFromRangedDamage()
        {
            Ranger(2);
            var row = Row(10000005, "abilities.tacticalevasion", 400, 400, 60, 2);
            row.Properties[AbilityProperty.EffectRadius] = 5;
            row.Properties[AbilityProperty.EffectModifier] = 30;
            row.Properties[AbilityProperty.EffectDurationMs] = 10000;
            row.Properties[AbilityProperty.EffectIntervalMs] = 1000;

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(10000005, null, 2)));
            Advance(400);
            Assert.AreEqual(70, DamageModifiers.ThroughSmoke(_client.Player, 100, melee: false), "ranged damage -30%");
            Assert.AreEqual(100, DamageModifiers.ThroughSmoke(_client.Player, 100, melee: true), "a melee swing is not ranged");
            for (var second = 0; second < 10; second++)
                GameEffectManager.Instance.DoWork(_map, 1000);
            Assert.AreEqual(100, DamageModifiers.ThroughSmoke(_client.Player, 100, melee: false), "the screen has lifted");
        }

        [TestMethod]
        public void TacticalEvasionPumpFiveReturnsTheRangerWhenTheRetreatRunsOut()
        {
            Ranger(5);
            var row = Row(10000005, "abilities.tacticalevasion", 400, 400, 60, 5);
            row.Properties[AbilityProperty.EffectDurationMs] = 60000;
            _client.Player.Position = new Vector3(1, 2, 3);

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(10000005, null, 5)));
            Advance(400);
            _client.Player.Position = new Vector3(50, 2, 50);
            for (var second = 0; second < 60; second++)
                GameEffectManager.Instance.DoWork(_map, 1000);
            Assert.AreEqual(new Vector3(1, 2, 3), _client.Player.Position);
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
