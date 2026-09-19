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

        [TestMethod]
        public void FireSupportStrikesTheGroundAfterItsDelay()
        {
            _client.Player.Class = 5;
            _client.Player.Skills[(SkillId)163] = new SkillsData((SkillId)163, 387, 1);
            _client.Player.Logos.AddRange(LogosFor(387));
            var row = Row(387, "abilities.firesupport", 666, 966, 60);
            row.Properties[AbilityProperty.DamageAmountMin] = 270;
            row.Properties[AbilityProperty.DamageAmountMax] = 540;
            row.Properties[AbilityProperty.EffectRadius] = 15;
            row.Properties[AbilityProperty.DelayTimeMs] = 2500;
            row.Properties[AbilityProperty.IntervalMs] = 500;
            var inside = Creature(new Vector3(30, 0, 5));
            var outside = Creature(new Vector3(30, 0, 30));

            var request = Request(387, null);
            request.TargetLocation = (30, 0, 0);
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, request));
            Advance(666);
            GameEffectManager.Instance.DoWork(_map, 2400);
            Assert.AreEqual(100000, inside.Attributes[Attributes.Health].Current, "not before the delay");
            GameEffectManager.Instance.DoWork(_map, 100);
            Assert.IsTrue(100000 - inside.Attributes[Attributes.Health].Current is >= 270 and <= 540);
            Assert.AreEqual(100000, outside.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void FireSupportPumpTwoStrikesItsTargetAndStunsIt()
        {
            _client.Player.Class = 5;
            _client.Player.Skills[(SkillId)163] = new SkillsData((SkillId)163, 387, 2);
            _client.Player.Logos.AddRange(LogosFor(387));
            var row = Row(387, "abilities.firesupport", 666, 666, 100, 2);
            row.Properties[AbilityProperty.DamageAmountMin] = 150;
            row.Properties[AbilityProperty.DamageAmountMax] = 180;
            row.Properties[AbilityProperty.EffectRadius] = 2;
            row.Properties[AbilityProperty.DelayTimeMs] = 5000;
            row.Properties[AbilityProperty.EffectDurationMs] = 4000;
            var target = Creature(new Vector3(40, 0, 0));

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(387, target.EntityId, 2)));
            Advance(666);
            GameEffectManager.Instance.DoWork(_map, 5000);
            Assert.IsTrue(100000 - target.Attributes[Attributes.Health].Current is >= 150 and <= 180);
            Assert.IsTrue(target.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.StunEffectType && e.Duration == 4000));
        }

        private ActionLevelInfo Biotechnician(int pump)
        {
            _client.Player.Class = 7;
            _client.Player.Skills[(SkillId)34] = new SkillsData((SkillId)34, 186, pump);
            _client.Player.Logos.AddRange(LogosFor(186));
            return Row(186, "abilities.cure", 300, 300, 60, (uint)pump);
        }

        [TestMethod]
        public void CurePumpOneRemovesDebuffsButNotTheDeathPenalty()
        {
            Biotechnician(1);
            var poison = GameEffectManager.Instance.AttachEffect(_map, _client.Player, AbilityEffects.DecayEffectType, 1, 10000, 0, false, null);
            var trauma = GameEffectManager.Instance.AttachTimedDebuff(_map, _client.Player, DeathPenaltyRules.RezSicknessEffectType, 1, 120000);

            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(186, null)));
            Advance(300);
            Assert.IsFalse(_client.Player.ActiveEffects.ContainsKey(poison.EffectId));
            Assert.IsTrue(_client.Player.ActiveEffects.ContainsKey(trauma.EffectId));
        }

        [TestMethod]
        public void CurePumpFourGuardsAgainstNewDebuffs()
        {
            var row = Biotechnician(4);
            row.Properties[AbilityProperty.Duration] = 25;
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(186, null, 4)));
            Advance(300);
            Assert.IsTrue(_client.Player.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.CureDebuffGuardType && e.Duration == 25000));
            var poison = GameEffectManager.Instance.AttachEffect(_map, _client.Player, AbilityEffects.DecayEffectType, 1, 10000, 0, false, null);
            Assert.IsFalse(_client.Player.ActiveEffects.ContainsKey(poison.EffectId), "kept off by the guard");
        }

        [TestMethod]
        public void CurePumpThreeRevivesTheDeadBesideTheBiotechnician()
        {
            var row = Biotechnician(3);
            row.Properties[AbilityProperty.AttributeMaxChange] = 50;
            var fallen = new Manifestation
            {
                Level = 1, State = CharacterState.Dead, MapContextId = 1220, MapChannel = _map, Cells = new uint[1, 1],
                Position = new Vector3(20, 0, 0)
            };
            fallen.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 0, 0, 0);
            var fallenClient = new Client(null, new ClientPacketHandler()) { Player = fallen, State = ClientState.Ingame };
            _map.ClientList.Add(fallenClient);
            EntityManager.Instance.RegisterEntity(fallen.EntityId, EntityType.Character);
            EntityManager.Instance.Players[fallen.EntityId] = fallen;
            var persist = typeof(PlayerDeathManager).GetField("_persist", BindingFlags.NonPublic | BindingFlags.Instance);
            var saved = persist.GetValue(PlayerDeathManager.Instance);
            persist.SetValue(PlayerDeathManager.Instance, new System.Action<Client, CharacterUpdate, object>((_, _, _) => { }));
            try
            {
                Assert.IsFalse(_actions.TryStartDamageAbility(_client, Request(186, _client.Player.EntityId, 3)), "only the dead can be resuscitated");
                Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(186, fallen.EntityId, 3)));
                Advance(300);
                Assert.AreEqual(CharacterState.Normal, fallen.State);
                Assert.AreEqual(500, fallen.Attributes[Attributes.Health].Current, "back at 50% health");
                Assert.AreEqual(_client.Player.Position, fallen.Position, "summoned to the biotechnician");
                Assert.IsFalse(fallen.ActiveEffects.Values.Any(e => e.TypeId == DeathPenaltyRules.RezSicknessEffectType), "no penalty");
                CollectionAssert.AreEqual(new[] { fallen.EntityId },
                    Drain().OfType<Packets.MapChannel.Server.PerformRecovery.CureRecovery>().Single().Revived.ToArray());
            }
            finally
            {
                persist.SetValue(PlayerDeathManager.Instance, saved);
                EntityManager.Instance.Players.Remove(fallen.EntityId);
                EntityManager.Instance.UnregisterEntity(fallen.EntityId);
            }
        }

        private ActionLevelInfo Tier4(int classId, int skill, int ability, string module, int pump = 1)
        {
            _client.Player.Class = (uint)classId;
            _client.Player.Skills[(SkillId)skill] = new SkillsData((SkillId)skill, ability, pump);
            _client.Player.Logos.AddRange(LogosFor(ability));
            return Row(ability, module, 400, 400, 0, (uint)pump);
        }

        [TestMethod]
        public void ReflectionSendsItsShareOfAReflectedTypeBack()
        {
            var row = Tier4(9, 26, 177, "abilities.reflection");
            row.Properties[AbilityProperty.Duration] = 120;
            row.Properties[AbilityProperty.DamageType] = (int)DamageType.Laser;
            row.Properties[AbilityProperty.DamagePercentMin] = 50;
            row.Properties[AbilityProperty.DamagePercentMax] = 50;
            var attacker = Creature(new Vector3(5, 0, 0));
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(177, null)));
            Advance(400);
            Drain();

            AbilityEffects.OnPlayerDamaged(_map, _client.Player, attacker, 100, DamageType.Laser);
            Assert.AreEqual(100000 - 50, attacker.Attributes[Attributes.Health].Current, "half of a laser hit back");
            Assert.AreEqual("AnnounceReflect", Drain().OfType<CallGameEffectMethodPacket>().Single().MethodName);
            AbilityEffects.OnPlayerDamaged(_map, _client.Player, attacker, 100, DamageType.Fire);
            Assert.AreEqual(100000 - 50, attacker.Attributes[Attributes.Health].Current, "fire is not reflected at pump 1");
        }

        [TestMethod]
        public void ConversionMakesTheGuardianTakeMore()
        {
            var row = Tier4(9, 43, 233, "abilities.conversion");
            row.Properties[AbilityProperty.DamagePercentMax] = 20;
            row.Properties[AbilityProperty.Duration] = 30;
            row.Properties[AbilityProperty.EffectRadius] = 3;
            row.Properties[AbilityProperty.HealPercentMax] = 60;
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(233, null)));
            Advance(400);
            Assert.AreEqual(120, DamageModifiers.Taken(_client.Player, 100));
        }

        [TestMethod]
        public void ShieldWaveAbsorbsItsAmountWhole()
        {
            var row = Tier4(9, 92, 305, "abilities.shieldwave");
            row.Properties[AbilityProperty.Duration] = 120;
            row.Properties[AbilityProperty.RadiusAroundSource] = 25;
            row.Properties[AbilityProperty.EffectModifier] = 300;
            row.Properties[AbilityProperty.AttrScaleType] = 2;
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(305, null)));
            Advance(400);
            Assert.AreEqual(0, DamageModifiers.ThroughShield(_client.Player, 200), "all of it, while the 300 lasts");
            Assert.AreEqual(100, DamageModifiers.ThroughShield(_client.Player, 200), "100 left to absorb");
            Assert.IsFalse(_client.Player.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.ShieldWaveType), "spent");
        }

        [TestMethod]
        public void ResistanceAddsItsRatingAndViralConversionChangesVirulent()
        {
            var row = Tier4(14, 153, 386, "abilities.resistance");
            row.Properties[AbilityProperty.Duration] = 45;
            row.Properties[AbilityProperty.RadiusAroundSource] = 20;
            row.Properties[AbilityProperty.ResistModifier] = 10;
            row.Properties[AbilityProperty.Interval] = 5;
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(386, null)));
            Advance(400);
            Assert.AreEqual(10, DamageModifiers.ResistRating(_client.Player));

            var viral = Row(187, "abilities.damageconversion", 500, 500, 0);
            viral.Properties[AbilityProperty.Duration] = 30;
            viral.Properties[AbilityProperty.DamageType] = (int)DamageType.Physical;
            AbilityEffects.ViralConversion(_map, _client.Player, viral);
            Assert.AreEqual(DamageType.Physical, DamageModifiers.DealtType(_client.Player, DamageType.Virulent));
            Assert.AreEqual(DamageType.Fire, DamageModifiers.DealtType(_client.Player, DamageType.Fire));
        }

        [TestMethod]
        public void DiseaseAtPumpFiveStopsHealing()
        {
            var row = Row(246, "abilities.disease", 500, 700, 20, 5);
            row.Properties[AbilityProperty.Duration] = 20;
            row.Properties[AbilityProperty.EffectHealthRegenModifier] = 0;
            row.Properties[AbilityProperty.HealingModifier] = 0;
            var target = Creature(new Vector3(5, 0, 0));
            target.Attributes[Attributes.Health].Current = 500;
            AbilityEffects.Disease(_map, _client.Player, target, row);
            Assert.IsTrue(target.ActiveEffects.Values.Single(e => e.TypeId == AbilityEffects.DiseaseType).PreventsHealing);
            AbilityEffects.Heal(_map, target, 100);
            Assert.AreEqual(500, target.Attributes[Attributes.Health].Current);
        }

        private static Missile WeaponHit(Actor source, Creature target, int damage)
            => new Missile { Source = source, TargetActor = target, TargetEntityId = target.EntityId, DamageA = damage, ActionId = ActionId.WeaponAttack };

        [TestMethod]
        public void SacrificeTradesMitigationForDamageAndDrawsThreatUntilUsedAgain()
        {
            var row = Tier4(8, 155, 385, "abilities.sacrifice");
            row.Properties[AbilityProperty.ResistModifier] = 10;
            row.Properties[AbilityProperty.OffensiveDamageModifier] = -10;
            row.Properties[AbilityProperty.ThreatModifierPercent] = 30;
            var creature = Creature(new Vector3(5, 0, 0));
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(385, null)));
            Advance(400);
            Assert.AreEqual(90, DamageModifiers.Outgoing(_client.Player, 100));
            Assert.AreEqual(10, DamageModifiers.ResistRating(_client.Player));
            AbilityEffects.OnCreatureHit(_map, WeaponHit(_client.Player, creature, 50), creature, new HitData());
            Assert.AreEqual((BehaviorManager.BehaviorActionFighting, _client.Player.EntityId),
                (creature.Controller.CurrentAction, creature.Controller.ActionFighting.TargetEntityId), "the creature turns on the grenadier");
            Advance(3000);
            Assert.IsFalse(_actions.TryStartDamageAbility(_client, Request(385, null)), "used again, it ends");
            Assert.AreEqual(100, DamageModifiers.Outgoing(_client.Player, 100));
        }

        [TestMethod]
        public void SelfDestructBlastsAroundAndReturnsTheDemolitionistHome()
        {
            var row = Tier4(12, 114, 267, "abilities.selfdestruct");
            row.Properties[AbilityProperty.Duration] = 30;
            row.Properties[AbilityProperty.DamageAmountMin] = 226;
            row.Properties[AbilityProperty.DamageAmountMax] = 300;
            row.Properties[AbilityProperty.EffectRadius] = 10;
            _client.Player.Position = new Vector3(0, 0, 0);
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(267, null)));
            Advance(400);
            _client.Player.Position = new Vector3(40, 0, 0);
            var nearby = Creature(new Vector3(45, 0, 0));
            for (var second = 0; second < 30; second++)
                GameEffectManager.Instance.DoWork(_map, 1000);
            Assert.IsTrue(100000 - nearby.Attributes[Attributes.Health].Current is >= 226 and <= 300, "the blast where the demolitionist stood");
            Assert.AreEqual(Vector3.Zero, _client.Player.Position, "back at the mark");
        }

        [TestMethod]
        public void ScatterbombsReachTheScatterPlusTheBurst()
        {
            var row = Tier4(8, 79, 232, "abilities.scatterbombs");
            row.Properties[AbilityProperty.RadiusAroundSource] = 5;
            row.Properties[AbilityProperty.EffectRadius] = 7;
            row.Properties[AbilityProperty.DamageAmountMin] = 100;
            row.Properties[AbilityProperty.DamageAmountMax] = 175;
            var inside = Creature(new Vector3(11, 0, 0));
            var outside = Creature(new Vector3(13, 0, 0));
            Assert.IsTrue(_actions.TryStartDamageAbility(_client, Request(232, null)));
            Advance(400);
            Assert.IsTrue(100000 - inside.Attributes[Attributes.Health].Current is >= 100 and <= 175);
            Assert.AreEqual(100000, outside.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ShredderAmmoAddsItsDamageOncePerInterval()
        {
            var row = Row(390, "abilities.weaponenhancement", 400, 400, 40);
            row.Properties[AbilityProperty.DamageAmountMin] = 60;
            row.Properties[AbilityProperty.DamageType] = (int)DamageType.Physical;
            row.Properties[AbilityProperty.DurationMs] = 30000;
            var target = Creature(new Vector3(5, 0, 0));
            AbilityEffects.ShredderAmmo(_map, _client.Player, _client, row);
            AbilityEffects.OnCreatureHit(_map, WeaponHit(_client.Player, target, 10), target, new HitData());
            AbilityEffects.OnCreatureHit(_map, WeaponHit(_client.Player, target, 10), target, new HitData());
            Assert.AreEqual(100000 - 60, target.Attributes[Attributes.Health].Current, "the extra 60 once, not twice within the interval");
        }

        [TestMethod]
        public void CalledShotToTheHeadMakesTheNextHitHarder()
        {
            var row = Row(430, "abilities.calledshot", 500, 500, 100, 5);
            row.Properties[AbilityProperty.Duration] = 20;
            row.Properties[AbilityProperty.DamageModifierPercent] = 50;
            row.Properties[AbilityProperty.EffectDurationMs] = 5000;
            var target = Creature(new Vector3(50, 0, 0));
            AbilityEffects.CalledShot(_map, _client.Player, target, row);
            var shot = WeaponHit(_client.Player, target, 100);
            AbilityEffects.OnCreatureHit(_map, shot, target, new HitData());
            Assert.AreEqual(150, shot.DamageA, "+50% on the called head shot");
            Assert.IsFalse(target.ActiveEffects.Values.Any(e => e.TypeId == AbilityEffects.CalledShotType), "the mark is spent");
            var next = WeaponHit(_client.Player, target, 100);
            AbilityEffects.OnCreatureHit(_map, next, target, new HitData());
            Assert.AreEqual(100, next.DamageA);
        }

        [TestMethod]
        public void ControlledFissionBlastsAroundItsTargetAfterTheDelay()
        {
            var row = Row(381, "abilities.controlledfission", 500, 500, 60);
            row.Properties[AbilityProperty.EffectRadius] = 10;
            row.Properties[AbilityProperty.DamageAmountMin] = 150;
            row.Properties[AbilityProperty.DamageAmountMax] = 150;
            row.Properties[AbilityProperty.DelayTimeMs] = 10000;
            var target = Creature(new Vector3(20, 0, 0));
            var beside = Creature(new Vector3(25, 0, 0));
            AbilityEffects.ControlledFission(_map, _client.Player, target, row, new System.Random(1));
            GameEffectManager.Instance.DoWork(_map, 9999);
            Assert.AreEqual(100000, beside.Attributes[Attributes.Health].Current);
            GameEffectManager.Instance.DoWork(_map, 1);
            Assert.AreEqual((100000 - 150, 100000 - 150), (target.Attributes[Attributes.Health].Current, beside.Attributes[Attributes.Health].Current));
        }

        [TestMethod]
        public void ExplosiveNanitesGoOffOnEachHitUntilSpent()
        {
            var row = Row(383, "abilities.explodingnanites", 500, 500, 60);
            row.Properties[AbilityProperty.DamageAmountMin] = 40;
            row.Properties[AbilityProperty.DamageAmountMax] = 40;
            row.Properties[AbilityProperty.Duration] = 30;
            row.Properties[AbilityProperty.UseCount] = 3;
            row.Properties[AbilityProperty.UseDropoff] = 5;
            var target = Creature(new Vector3(20, 0, 0));
            AbilityEffects.ExplosiveNanites(_map, _client.Player, target, row, new System.Random(1));
            for (var hit = 0; hit < 5; hit++)
                MissileManager.Instance.DamageTick(_map, _client.Player, target, 1, DamageType.Physical);
            Assert.AreEqual(100000 - 5 - (40 + 38 + 36), target.Attributes[Attributes.Health].Current, "three explosions, 5% weaker each, then spent");
        }

        [TestMethod]
        public void PolarityFieldMakesTheTargetVulnerableToItsType()
        {
            var row = Row(388, "abilities.polarityfield", 400, 400, 60);
            row.Properties[AbilityProperty.PerPumpMod] = -10;
            row.Properties[AbilityProperty.DamageType] = (int)DamageType.Electrical;
            row.Properties[AbilityProperty.DurationMs] = 30000;
            var target = Creature(new Vector3(20, 0, 0));
            AbilityEffects.PolarityField(_map, _client.Player, target, row);
            Assert.AreEqual(110, DamageModifiers.AgainstCreature(target, 100, DamageType.Electrical), "the client's (100 - r) / 100");
            Assert.AreEqual(100, DamageModifiers.AgainstCreature(target, 100, DamageType.Fire));
        }

        [TestMethod]
        public void FeedbackHurtsTheCreatureEachTimeItAttacks()
        {
            var row = Row(298, "abilities.feedback", 500, 500, 60);
            row.Properties[AbilityProperty.DamageAmountMin] = 50;
            row.Properties[AbilityProperty.DamageAmountMax] = 50;
            row.Properties[AbilityProperty.Duration] = 20;
            var target = Creature(new Vector3(20, 0, 0));
            AbilityEffects.Feedback(_map, _client.Player, target, row, new System.Random(1));
            AbilityEffects.OnCreatureAttack(target);
            AbilityEffects.OnCreatureAttack(target);
            Assert.AreEqual(100000 - 100, target.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void CloakWaveHidesThePlayerUntilTheyAct()
        {
            var row = Row(252, "abilities.cloakwave", 2300, 847, 0);
            row.Properties[AbilityProperty.RadiusAroundSource] = 25;
            row.Properties[AbilityProperty.EffectDurationMs] = 60000;
            AbilityEffects.CloakWave(_map, _client, row);
            Assert.IsTrue(_client.Player.ActiveEffects.Values.Any(e => e.Stealth));
            AbilityEffects.BreakStealth(_map, _client.Player);
            Assert.IsFalse(_client.Player.ActiveEffects.Values.Any(e => e.Stealth));
        }

        [TestMethod]
        public void TraitorTurnsTheCreatureUntilTheEffectEnds()
        {
            var row = Row(393, "abilities.traitor", 500, 700, 20);
            row.Properties[AbilityProperty.Duration] = 10;
            row.Properties[AbilityProperty.EffectRadius] = 10;
            var turned = Creature(new Vector3(10, 0, 0));
            var former = Creature(new Vector3(14, 0, 0));
            Assert.IsTrue(AbilityEffects.Traitor(_map, _client.Player, turned, row));
            Assert.AreEqual(Factions.AFS, turned.Faction);
            Assert.AreEqual(former.EntityId, turned.Controller.ActionFighting.TargetEntityId, "set on its nearest former ally");
            GameEffectManager.Instance.DoWork(_map, 10000);
            Assert.AreEqual(Factions.Bane, turned.Faction);
        }

        [TestMethod]
        public void HackTurnsOnlyMechanicalCreatures()
        {
            var row = Row(303, "abilities.hack", 400, 400, 20);
            row.Properties[AbilityProperty.Duration] = 10;
            row.Properties[AbilityProperty.EffectRadius] = 10;
            const EntityClasses droneClass = (EntityClasses)9000031, beastClass = (EntityClasses)9000032;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[droneClass] = new EntityClass((uint)droneClass, "test drone", 0, 1, new List<AugmentationType>(), true);
            classes[droneClass].CreatureFlags.Add(CreatureFlag.Mechanical);
            classes[beastClass] = new EntityClass((uint)beastClass, "test beast", 0, 1, new List<AugmentationType>(), true);
            classes[beastClass].CreatureFlags.Add(CreatureFlag.Biological);
            try
            {
                var drone = Creature(new Vector3(10, 0, 0));
                drone.EntityClass = droneClass;
                var beast = Creature(new Vector3(12, 0, 0));
                beast.EntityClass = beastClass;
                Assert.IsFalse(AbilityEffects.Hack(_map, _client.Player, beast, row), "a biological creature cannot be hacked");
                Assert.IsTrue(AbilityEffects.Hack(_map, _client.Player, drone, row));
                Assert.AreEqual(Factions.AFS, drone.Faction);
                Assert.AreEqual(beast.EntityId, drone.Controller.ActionFighting.TargetEntityId, "it attacks its former ally");
            }
            finally
            {
                classes.Remove(droneClass);
                classes.Remove(beastClass);
            }
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(3)]
        public void MindControlMakesTheTargetFleeOrTurnOnItsAllies(int pump)
        {
            var row = Row(304, "abilities.mindcontrol", 500, 700, 20, (uint)pump);
            row.Properties[AbilityProperty.Duration] = 15;
            row.Properties[AbilityProperty.Interval] = 4;
            var target = Creature(new Vector3(10, 0, 0));
            var ally = Creature(new Vector3(14, 0, 0));
            BehaviorManager.Instance.SetActionFighting(target, _client.Player.EntityId);
            AbilityEffects.MindControl(_map, _client.Player, target, (ActionId)304, row, new System.Random(1));
            GameEffectManager.Instance.DoWork(_map, 4000);
            if (pump == 1)
            {
                Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, target.Controller.CurrentAction, "it flees the fight");
                Assert.IsTrue(target.StunnedUntil > System.Environment.TickCount64);
            }
            else
                Assert.AreEqual(ally.EntityId, target.Controller.ActionFighting.TargetEntityId, "it attacks an ally");
        }

        [TestMethod]
        public void ReanimationRaisesABiologicalCorpseToFightForTheUserThenLetsItFall()
        {
            var row = Row(240, "abilities.reanimation", 1000, 966, 40);
            row.Properties[AbilityProperty.Duration] = 120;
            row.Properties[AbilityProperty.CreatureLevelDifference] = -4;
            const EntityClasses beastClass = (EntityClasses)9000033;
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            classes[beastClass] = new EntityClass((uint)beastClass, "test beast", 0, 1, new List<AugmentationType>(), true);
            classes[beastClass].CreatureFlags.Add(CreatureFlag.Biological);
            try
            {
                _client.Player.Level = 10;
                var corpse = Creature(new Vector3(10, 0, 0));
                corpse.EntityClass = beastClass;
                corpse.State = CharacterState.Dead;
                corpse.Attributes[Attributes.Health].Current = 0;
                Assert.IsTrue(AbilityEffects.Reanimate(_map, _client.Player, corpse, row));
                Assert.AreEqual((CharacterState.Normal, Factions.AFS, 6u, 100000),
                    (corpse.State, corpse.Faction, corpse.Level, corpse.Attributes[Attributes.Health].Current));
                Assert.IsTrue(Drain().OfType<Packets.ClientMethod.Server.RevivedPacket>().Any());
                GameEffectManager.Instance.DoWork(_map, 120000);
                Assert.AreEqual((CharacterState.Dead, Factions.Bane), (corpse.State, corpse.Faction));
                Assert.IsFalse(Drain().OfType<GameEffectAttachedPacket>().Any(), "no invented client effect");
            }
            finally
            {
                classes.Remove(beastClass);
            }
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
                if (packet.Message is CallMethodMessage call)
                    packets.Add(call.Packet);
            return packets;
        }
    }
}
