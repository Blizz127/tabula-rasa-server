using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Models;
    using Structures;

    /// <summary>
    /// What a damage ability does to a creature besides the damage, from the ability's own client row.
    ///
    ///   - STUN_CHANCE (percent) and STUN_DURATION (seconds): the client's STUN game effect (86,
    ///     gameeffects.stuneffect.StunEffect) makes its target uncontrolled and blocks its movement while attached; the
    ///     server holds the creature's AI for the same time. Lightning ranks 4-5 carry 50% / 3 s.
    ///   - KNOCKBACK_DISTANCE (metres): the client's KNOCKBACK effect (8, gameeffects.knockbackeffect) only times the
    ///     knockback, so the displacement is the server's. The creature is pushed straight away from the performer by
    ///     that distance, no further than the navmesh lets it walk, and set on the ground (inferred: no source gives
    ///     the original's direction or pathing rule). The effect lasts the row's DURATION_MS where it has one, else
    ///     one second (inferred).
    /// </summary>
    public static class AbilityEffects
    {
        public const int StunEffectType = 86;
        public const int KnockbackEffectType = 8;
        public const int DecayEffectType = 82;          // gameeffectdata DECAY, actions.abilities.decay.DecayEffect
        public const int RageEffectType = 235;          // RAGE, actions.abilities.rage.RageEffect
        public const int RageSourceEffectType = 236;    // RAGESOURCE, actions.abilities.rage.RageSourceEffect
        public const int BioAugmentationEffectType = 329; // BIO_AUGMENTATION_EFFECT, "Increases %(attrId)s by %(amount)s"
        public const int ScourgeEffectType = 256;        // SCOURGE_EFFECT, "Continuously damages all hostiles in a radius around you"
        public const int ShieldExtenderSourceType = 10000056; // SHIELD_EXTENDER_SOURCE
        public const int ShieldExtenderShieldedType = 10000055; // SHIELD_EXTENDER_SHIELDED
        public const int MagFlashType = 10000075;             // TACTICAL_EVASION_MAG_FLASH_EFFECT
        public const int SmokeScreenType = 10000076;          // TACTICAL_EVASION_SMOKE_SCREEN_EFFECT, "Incoming ranged damage reduced by %(modAmt)s%%"
        public const int SmokeScreenAuraType = 10000077;      // TACTICAL_EVASION_SMOKE_SCREEN_AURA_EFFECT
        public const int EvasionTeleportType = 10000078;      // TACTICAL_EVASION_TELEPORT_EFFECT, "Tactical Retreat", detachable
        public const int EvasionLocationType = 10000079;      // TACTICAL_EVASION_LOCATION_EFFECT
        public const int ReconstructionHelpType = 180;          // "Spirit: +x% / Healing: min - max HP / Adrenaline Gain ... every interval"
        public const int ReconstructionHarmType = 10000063;     // "Spirit: -x% / Damage: min - max HP / Adrenaline Drain ... every interval"
        public const int ReconstructionHelpPoolType = 10000064; // "Maximum Health: +x%"
        public const int ReconstructionHarmPoolType = 10000065; // "Maximum Health: -x%"

        /// <summary>
        /// The pulse of an effect whose row gives no interval (Scourge, Shield Extender's reach): one second, inferred -
        /// the tooltips say "continuously" and "within".
        /// </summary>
        public const int DefaultPulseMs = 1000;
        private static readonly Random Random = new Random();

        public static void Apply(MapChannel map, Actor source, Creature target, ActionLevelInfo info, Func<int, bool> roll = null)
        {
            if (map == null || source == null || target == null || info == null || target.State == CharacterState.Dead)
                return;
            roll ??= percent => Random.Next(100) < percent;

            if (info.Has(AbilityProperty.StunChance) && info.Get(AbilityProperty.StunDuration) > 0 && roll(info.Get(AbilityProperty.StunChance)))
                Stun(map, target, info.Get(AbilityProperty.StunDuration) * 1000);

            var distance = info.Get(AbilityProperty.KnockbackDistance);
            if (distance > 0)
                Knockback(map, source, target, distance, info.Has(AbilityProperty.DurationMs) ? info.Get(AbilityProperty.DurationMs) : 1000);
        }

        /// <summary>
        /// Ruin: a damage over time on one enemy. The DECAY effect's own tooltip reads "Deals %(dmgMin)s - %(dmgMax)s
        /// %(damageType)s damage every %(interval)s seconds", so DAMAGE_AMOUNT_MIN..MAX is rolled every INTERVAL
        /// seconds for DURATION seconds, scaled by DAMAGE_SCALE_TYPE like any ability damage, and each tick is sent as
        /// the effect's GameEffectTick(effectId, [(targetId, rawInfo)]) - what DamageOverTime.OnTick reads. Pump 5's
        /// EFFECT_MOVEMENT_MODIFIER ("Incendiary, -Movement" in the skill text) slows the target by that percent while it
        /// lasts (inferred: the row gives the number, the text only its direction).
        /// </summary>
        public static void Decay(MapChannel map, Manifestation source, Creature target, ActionLevelInfo info, Random random)
        {
            if (map == null || source == null || target == null || target.State == CharacterState.Dead)
                return;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), source.Level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), source.Level, scaleType));
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var interval = Math.Max(1, info.Get(AbilityProperty.Interval, 1));
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var tooltip = new System.Collections.Generic.Dictionary<string, double>
            {
                ["dmgMin"] = min, ["dmgMax"] = max, ["damageType"] = (int)damageType, ["interval"] = interval
            };
            var slow = info.Get(AbilityProperty.EffectMovementModifier);
            var walk = target.WalkSpeed;
            var run = target.RunSpeed;
            var effect = GameEffectManager.Instance.AttachEffect(map, target, DecayEffectType, info.Level, duration, source.EntityId,
                false, tooltip, interval * 1000, tick =>
                {
                    if (target.State == CharacterState.Dead)
                        return;
                    var hit = MissileManager.Instance.DamageTick(map, source, target, DamageModifiers.Outgoing(source, random.Next(min, max + 1)), damageType);
                    CellManager.Instance.CellCallMethod(map, target, new Packets.MapChannel.Server.GameEffectTickPacket(tick.EffectId,
                        new[] { (target.EntityId, hit) }));
                });
            if (slow > 0)
            {
                target.WalkSpeed = walk * (100 - slow) / 100f;
                target.RunSpeed = run * (100 - slow) / 100f;
                effect.OnDetach = _ => { target.WalkSpeed = walk; target.RunSpeed = run; };
            }
        }

        /// <summary>
        /// Rage: a toggle (rage.py RageAction.isToggle) that puts RAGESOURCE on the soldier for DURATION seconds. Every
        /// INTERVAL seconds it pulses: the soldier and, where the row has RADIUS_AROUND_SOURCE (pumps 2, 4, 5 - "Squad"
        /// in the skill text), every squad member on the map within that radius carry RAGE, whose tooltip is "Damage
        /// Bonus: %(dmgMod)s%% / Damage Resist: %(resistMod)s". The pulse is RAGESOURCE's GameEffectTick(effectId,
        /// targetIds), which RageSourceEffect.OnTick announces on each. While RAGE is held the holder deals
        /// DAMAGE_PERCENT more damage, and RESIST_MODIFIER is added to its resistance rating against every type - the
        /// tooltip shows it without a percent sign, as the client shows resistance ratings, which the server converts
        /// with the client's own rating / (rating + 50). Ending the source ends every RAGE it gave.
        /// </summary>
        public static void Rage(MapChannel map, Game.Client client, ActionLevelInfo info)
        {
            var soldier = client?.Player;
            if (map == null || soldier == null)
                return;
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var interval = Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000;
            var bonus = info.Get(AbilityProperty.DamagePercentMax, info.Get(AbilityProperty.DamagePercentMin));
            var resist = info.Get(AbilityProperty.ResistModifier);
            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            var given = new System.Collections.Generic.Dictionary<Actor, GameEffect>();

            void Pulse(GameEffect source)
            {
                var targets = new System.Collections.Generic.List<Actor> { soldier };
                if (radius > 0 && PartyManager.Instance.PartyOf(client) is { } party)
                    foreach (var member in party.Members)
                        if (member.EntityId != soldier.EntityId &&
                            EntityManager.Instance.Players.TryGetValue(member.EntityId, out var mate) &&
                            ReferenceEquals(mate.MapChannel, map) && mate.State != CharacterState.Dead &&
                            Vector3.Distance(mate.Position, soldier.Position) <= radius)
                            targets.Add(mate);
                var remaining = (int)Math.Max(0, source.Duration - source.EffectTime);
                foreach (var target in targets)
                    if (!given.TryGetValue(target, out var held) || !target.ActiveEffects.ContainsKey(held.EffectId))
                    {
                        var rage = GameEffectManager.Instance.AttachEffect(map, target, RageEffectType, info.Level, remaining, soldier.EntityId, true,
                            new System.Collections.Generic.Dictionary<string, double> { ["dmgMod"] = bonus, ["resistMod"] = resist });
                        rage.DamageBonusPercent = bonus;
                        rage.ResistRating = resist;
                        given[target] = rage;
                    }
                CellManager.Instance.CellCallMethod(map, soldier, new Packets.MapChannel.Server.GameEffectTickPacket(source.EffectId,
                    targets.Select(t => t.EntityId).ToList()));
            }

            var sourceEffect = GameEffectManager.Instance.AttachEffect(map, soldier, RageSourceEffectType, info.Level, duration, soldier.EntityId,
                true, new System.Collections.Generic.Dictionary<string, double>(), interval, Pulse);
            sourceEffect.OnDetach = _ =>
            {
                foreach (var (holder, rage) in given)
                    GameEffectManager.Instance.DettachEffect(map, holder, rage);
            };
            Pulse(sourceEffect);
        }

        /// <summary>
        /// Bio Augmentation: a friendly target (bioaugmentation.py TARGET_FRIENDLY) carries BIO_AUGMENTATION_EFFECT for
        /// EFFECT_DURATION_MS, raising ATTRIBUTE_ID by EFFECT_MODIFIER - the effect's tooltip, "Increases %(attrId)s by
        /// %(amount)s", shows the amount without a percent sign, so it is a flat amount (inferred from that). The target's
        /// attributes are recalculated when it attaches and again when it ends.
        /// </summary>
        public static void BioAugmentation(MapChannel map, Manifestation source, Game.Client targetClient, ActionLevelInfo info)
        {
            var target = targetClient?.Player;
            if (map == null || source == null || target == null)
                return;
            var attribute = (Attributes)info.Get(AbilityProperty.AttributeId);
            var amount = info.Get(AbilityProperty.EffectModifier);
            // One at a time: a new augmentation replaces the running one.
            foreach (var old in target.ActiveEffects.Values.Where(e => e.TypeId == BioAugmentationEffectType).ToList())
                GameEffectManager.Instance.DettachEffect(map, target, old);
            var effect = GameEffectManager.Instance.AttachEffect(map, target, BioAugmentationEffectType, info.Level,
                info.Get(AbilityProperty.EffectDurationMs), source.EntityId, true,
                new System.Collections.Generic.Dictionary<string, double> { ["attrId"] = (int)attribute, ["amount"] = amount });
            effect.BonusAttribute = attribute;
            effect.AttributeBonus = amount;
            void Recalculate()
            {
                ManifestationManager.Instance.UpdateStatsValues(targetClient, false);
                targetClient.CallMethod(target.EntityId, new Packets.MapChannel.Server.AttributeInfoPacket(target.Attributes));
            }
            effect.OnDetach = _ => Recalculate();
            Recalculate();
        }

        /// <summary>
        /// Scourge: "Damages all enemies in a radius around the user at an interval over a set time" (the skill text).
        /// SCOURGE_EFFECT on the commando for DURATION seconds; every pulse, each hostile creature within EFFECT_RADIUS
        /// takes a DAMAGE_AMOUNT roll (scaled like any ability damage), announced through the effect's own
        /// Recv_AnnounceDamage - Scourge's effect has tickOnDamage, so the client marks each pulse.
        /// </summary>
        public static void Scourge(MapChannel map, Manifestation source, ActionLevelInfo info, Random random)
        {
            if (map == null || source == null)
                return;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), source.Level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), source.Level, scaleType));
            var radius = info.Get(AbilityProperty.EffectRadius);
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            GameEffectManager.Instance.AttachEffect(map, source, ScourgeEffectType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                source.EntityId, true, new System.Collections.Generic.Dictionary<string, double> { ["dmgMin"] = min, ["dmgMax"] = max, ["radius"] = radius },
                DefaultPulseMs, pulse =>
                {
                    var hits = new System.Collections.Generic.List<(ulong, HitData)>();
                    foreach (var creature in HostilesAround(map, source, source.Position, radius))
                        hits.Add((creature.EntityId, MissileManager.Instance.DamageTick(map, source, creature,
                            DamageModifiers.Outgoing(source, random.Next(min, max + 1)), damageType)));
                    if (hits.Count > 0)
                        CellManager.Instance.CellCallMethod(map, source, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(pulse.EffectId, hits));
                });
        }

        /// <summary>
        /// Shield Extender: "a reduction to incoming damage to the target and all nearby squad members for a set time or
        /// until the shield has absorbed a set amount of damage" (the skill text). SHIELD_EXTENDER_SOURCE on the friendly
        /// target for EFFECT_DURATION_MS; every pulse, the target and each squad member within EFFECT_RADIUS of it carry
        /// SHIELD_EXTENDER_SHIELDED, under which EFFECT_MODIFIER percent of each hit is absorbed from one pool of
        /// EFFECT_DAMAGE_MAX. When the pool runs dry the shield breaks and every SHIELDED goes with it.
        /// </summary>
        public static void ShieldExtender(MapChannel map, Manifestation sapper, Game.Client targetClient, ActionLevelInfo info)
        {
            var target = targetClient?.Player;
            if (map == null || sapper == null || target == null)
                return;
            var radius = info.Get(AbilityProperty.EffectRadius);
            var pool = new ShieldPool { Percent = info.Get(AbilityProperty.EffectModifier), Remaining = info.Get(AbilityProperty.EffectDamageMax) };
            var shielded = new System.Collections.Generic.Dictionary<Actor, GameEffect>();
            GameEffect source = null;

            void Pulse(GameEffect bubble)
            {
                var under = new System.Collections.Generic.List<Actor> { target };
                if (PartyManager.Instance.PartyOf(targetClient) is { } party)
                    foreach (var member in party.Members)
                        if (member.EntityId != target.EntityId && EntityManager.Instance.Players.TryGetValue(member.EntityId, out var mate) &&
                            ReferenceEquals(mate.MapChannel, map) && mate.State != CharacterState.Dead &&
                            Vector3.Distance(mate.Position, target.Position) <= radius)
                            under.Add(mate);
                // Leaving the bubble ends its protection.
                foreach (var (holder, effect) in shielded.ToList())
                    if (!under.Contains(holder))
                    {
                        GameEffectManager.Instance.DettachEffect(map, holder, effect);
                        shielded.Remove(holder);
                    }
                var remaining = (int)Math.Max(0, bubble.Duration - bubble.EffectTime);
                foreach (var holder in under)
                    if (!shielded.ContainsKey(holder))
                    {
                        var effect = GameEffectManager.Instance.AttachEffect(map, holder, ShieldExtenderShieldedType, info.Level, remaining,
                            sapper.EntityId, true, new System.Collections.Generic.Dictionary<string, double>());
                        effect.Shield = pool;
                        shielded[holder] = effect;
                    }
            }

            source = GameEffectManager.Instance.AttachEffect(map, target, ShieldExtenderSourceType, info.Level, info.Get(AbilityProperty.EffectDurationMs),
                sapper.EntityId, true, new System.Collections.Generic.Dictionary<string, double>(), DefaultPulseMs, Pulse);
            source.OnDetach = _ =>
            {
                foreach (var (holder, effect) in shielded.ToList())
                    GameEffectManager.Instance.DettachEffect(map, holder, effect);
                shielded.Clear();
            };
            pool.Broken = () => GameEffectManager.Instance.DettachEffect(map, target, source);
            Pulse(source);
        }

        /// <summary>
        /// Reconstruction: "Heals and Buffs the user and nearby squad members as well as Damages and Debuffs nearby
        /// enemies for a set time" (the skill text). Everyone within RADIUS_AROUND_SOURCE is reached - the biotechnician
        /// and squad members on the map get the HELP effect, hostile creatures the HARM effect - and the recovery lists
        /// (entityId, effectTypeId) for each, which reconstruction.py DoAbility announces. From the row:
        ///   - HEAL_AMOUNT / DAMAGE_AMOUNT once when the row has no INTERVAL (pumps 1, 3), else every INTERVAL for
        ///     DURATION (pumps 2, 4), with ADRENALINE_INCREASE added to the squad and drained from nothing (creatures
        ///     have no adrenaline) at each pulse (pump 4);
        ///   - ATTRIBUTE_MAX_CHANGE as the tooltips' "Spirit: +%(spiritBuff)s%%" on the squad for DURATION (pump 3);
        ///   - EFFECT_MODIFIER as "Maximum Health: +/-%(healthPoolMod)s%%" for DURATION (pump 5).
        /// </summary>
        public static List<(ulong, int)> Reconstruction(MapChannel map, Game.Client client, ActionLevelInfo info, Random random)
        {
            var biotech = client?.Player;
            var announced = new List<(ulong, int)>();
            if (map == null || biotech == null)
                return announced;
            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var interval = info.Get(AbilityProperty.Interval) * 1000;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var dmgMin = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), biotech.Level, scaleType);
            var dmgMax = Math.Max(dmgMin, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), biotech.Level, scaleType));
            var healMin = info.Get(AbilityProperty.HealAmountMin);
            var healMax = Math.Max(healMin, info.Get(AbilityProperty.HealAmountMax, healMin));
            var adrenaline = info.Get(AbilityProperty.AdrenalineIncreaseMin);
            var spirit = info.Get(AbilityProperty.AttributeMaxChange);
            var pool = info.Get(AbilityProperty.EffectModifier);
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);

            var squad = new List<Game.Client> { client };
            if (PartyManager.Instance.PartyOf(client) is { } party)
                foreach (var member in party.Members)
                    if (member.EntityId != biotech.EntityId &&
                        map.ClientList?.FirstOrDefault(c => c?.Player?.EntityId == member.EntityId) is { } mate &&
                        mate.Player.State != CharacterState.Dead && Vector3.Distance(mate.Player.Position, biotech.Position) <= radius)
                        squad.Add(mate);
            var enemies = HostilesAround(map, biotech, biotech.Position, radius);

            void Heal(Game.Client member)
            {
                if (healMax > 0)
                {
                    var health = member.Player.Attributes[Attributes.Health];
                    health.Current = Math.Min(health.CurrentMax, health.Current + random.Next(healMin, healMax + 1));
                    CellManager.Instance.CellCallMethod(map, member.Player, new Packets.MapChannel.Server.UpdateHealthPacket(health, member.Player.EntityId));
                }
                if (adrenaline > 0 && member.Player.Attributes.TryGetValue(Attributes.Chi, out var chi))
                {
                    chi.Current = Math.Min(chi.CurrentMax, chi.Current + adrenaline);
                    member.CallMethod(member.Player.EntityId, new Packets.MapChannel.Server.UpdateChiPacket(chi, member.Player.EntityId));
                }
            }

            void Harm(Creature enemy, GameEffect effect)
            {
                if (dmgMax <= 0 || enemy.State == CharacterState.Dead)
                    return;
                var hit = MissileManager.Instance.DamageTick(map, biotech, enemy, DamageModifiers.Outgoing(biotech, random.Next(dmgMin, dmgMax + 1)), damageType);
                if (effect != null)
                    CellManager.Instance.CellCallMethod(map, enemy, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(effect.EffectId,
                        new[] { (enemy.EntityId, hit) }));
            }

            foreach (var member in squad)
            {
                var helpType = pool > 0 ? ReconstructionHelpPoolType : ReconstructionHelpType;
                announced.Add((member.Player.EntityId, helpType));
                if (duration <= 0)
                {
                    Heal(member);
                    continue;
                }
                var tooltip = new Dictionary<string, double>
                {
                    ["healMin"] = healMin, ["healMax"] = healMax, ["interval"] = interval / 1000, ["spiritBuff"] = spirit,
                    ["adrenalineMin"] = adrenaline, ["adrenalineMax"] = info.Get(AbilityProperty.AdrenalineIncreaseMax, adrenaline),
                    ["healthPoolMod"] = pool
                };
                var help = GameEffectManager.Instance.AttachEffect(map, member.Player, helpType, info.Level, duration, biotech.EntityId, true,
                    tooltip, interval, interval > 0 ? _ => Heal(member) : null);
                if (interval <= 0 && pool <= 0)
                    Heal(member);
                if (spirit > 0 || pool > 0)
                {
                    help.BonusAttribute = pool > 0 ? Attributes.Health : Attributes.Spirit;
                    var baseline = member.Player.Attributes[help.BonusAttribute.Value].NormalMax;
                    help.AttributeBonus = baseline * (pool > 0 ? pool : spirit) / 100;
                    ManifestationManager.Instance.UpdateStatsValues(member, false);
                    member.CallMethod(member.Player.EntityId, new Packets.MapChannel.Server.AttributeInfoPacket(member.Player.Attributes));
                    help.OnDetach = _ =>
                    {
                        ManifestationManager.Instance.UpdateStatsValues(member, false);
                        member.CallMethod(member.Player.EntityId, new Packets.MapChannel.Server.AttributeInfoPacket(member.Player.Attributes));
                    };
                }
            }

            foreach (var enemy in enemies)
            {
                var harmType = pool > 0 ? ReconstructionHarmPoolType : ReconstructionHarmType;
                announced.Add((enemy.EntityId, harmType));
                if (duration <= 0)
                {
                    Harm(enemy, null);
                    continue;
                }
                GameEffect harm = null;
                harm = GameEffectManager.Instance.AttachEffect(map, enemy, harmType, info.Level, duration, biotech.EntityId, false,
                    new Dictionary<string, double> { ["dmgMin"] = dmgMin, ["dmgMax"] = dmgMax, ["interval"] = interval / 1000, ["spiritMod"] = spirit, ["healthPoolMod"] = pool },
                    interval, interval > 0 ? _ => Harm(enemy, harm) : null);
                if (interval <= 0)
                    Harm(enemy, harm);
                if (pool > 0)
                {
                    var health = enemy.Attributes[Attributes.Health];
                    var cut = health.CurrentMax * pool / 100;
                    health.CurrentMax -= cut;
                    health.Current = Math.Min(health.Current, health.CurrentMax);
                    CellManager.Instance.CellCallMethod(map, enemy, new Packets.MapChannel.Server.UpdateHealthPacket(health, enemy.EntityId));
                    harm.OnDetach = _ => health.CurrentMax += cut;
                }
            }
            return announced;
        }

        /// <summary>
        /// Tactical Evasion, whose PvE skill text reads "Pump 1: Clear Enemy Hate | Pump 2: -Damage Taken, +Radius | Pump 3:
        /// Clear Enemy Hate | Pump 4: -Damage Taken, +Radius | Pump 5: Delayed Teleport". From the row:
        ///   - RADIUS_AROUND_SOURCE (pumps 1, 3): every hostile within it fighting the ranger or their squad drops the fight,
        ///     and carries the mag flash for EFFECT_DURATION_MS; the recovery names them, as tacticalevasion.py reads;
        ///   - EFFECT_RADIUS with EFFECT_MODIFIER (pumps 2, 4): the smoke-screen aura on the ranger pulses every
        ///     EFFECT_INTERVAL_MS for EFFECT_DURATION_MS, keeping the smoke screen - "Incoming ranged damage reduced by
        ///     %(modAmt)s%%" - on the squad within the radius;
        ///   - otherwise (pump 5): "Tactical Retreat" marks where the ranger stands and returns them there when it runs out
        ///     after EFFECT_DURATION_MS; detaching it (the client allows that) cancels the return. That the return comes at
        ///     the end is inferred from "Delayed Teleport".
        /// Returns the entities the recovery names.
        /// </summary>
        public static List<ulong> TacticalEvasion(MapChannel map, Game.Client client, ActionLevelInfo info)
        {
            var ranger = client?.Player;
            var named = new List<ulong>();
            if (map == null || ranger == null)
                return named;
            var duration = info.Get(AbilityProperty.EffectDurationMs);
            var squad = SquadAround(map, client, ranger.Position, float.MaxValue);
            if (info.Has(AbilityProperty.RadiusAroundSource))
            {
                var fought = new HashSet<ulong>(squad.Select(c => c.Player.EntityId));
                foreach (var enemy in HostilesAround(map, ranger, ranger.Position, info.Get(AbilityProperty.RadiusAroundSource)))
                {
                    if (enemy.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting &&
                        fought.Contains(enemy.Controller.ActionFighting.TargetEntityId))
                        BehaviorManager.Instance.DropFight(enemy);
                    GameEffectManager.Instance.AttachEffect(map, enemy, MagFlashType, info.Level, duration, ranger.EntityId, false, new Dictionary<string, double>());
                    named.Add(enemy.EntityId);
                }
                return named;
            }
            if (info.Has(AbilityProperty.EffectModifier))
            {
                var radius = info.Get(AbilityProperty.EffectRadius);
                var reduction = info.Get(AbilityProperty.EffectModifier);
                var screened = new Dictionary<Actor, GameEffect>();
                var aura = GameEffectManager.Instance.AttachEffect(map, ranger, SmokeScreenAuraType, info.Level, duration, ranger.EntityId, true,
                    new Dictionary<string, double>(), Math.Max(1, info.Get(AbilityProperty.EffectIntervalMs, DefaultPulseMs)), pulse =>
                    {
                        var inside = SquadAround(map, client, ranger.Position, radius).Select(c => (Actor)c.Player).ToList();
                        foreach (var (holder, effect) in screened.ToList())
                            if (!inside.Contains(holder))
                            {
                                GameEffectManager.Instance.DettachEffect(map, holder, effect);
                                screened.Remove(holder);
                            }
                        foreach (var holder in inside.Where(h => !screened.ContainsKey(h)))
                        {
                            var smoke = GameEffectManager.Instance.AttachEffect(map, holder, SmokeScreenType, info.Level,
                                (int)Math.Max(0, pulse.Duration - pulse.EffectTime), ranger.EntityId, true,
                                new Dictionary<string, double> { ["modAmt"] = reduction });
                            smoke.RangedReductionPercent = reduction;
                            screened[holder] = smoke;
                        }
                    });
                aura.OnDetach = _ =>
                {
                    foreach (var (holder, effect) in screened.ToList())
                        GameEffectManager.Instance.DettachEffect(map, holder, effect);
                };
                aura.OnTick(aura);
                named.Add(ranger.EntityId);
                return named;
            }
            var home = ranger.Position;
            var location = GameEffectManager.Instance.AttachEffect(map, ranger, EvasionLocationType, info.Level, duration, ranger.EntityId, true, new Dictionary<string, double>());
            var retreat = GameEffectManager.Instance.AttachEffect(map, ranger, EvasionTeleportType, info.Level, duration, ranger.EntityId, true, new Dictionary<string, double>());
            retreat.OnDetach = effect =>
            {
                GameEffectManager.Instance.DettachEffect(map, ranger, location);
                if (effect.EffectTime >= effect.Duration && ranger.State != CharacterState.Dead && ReferenceEquals(ranger.MapChannel, map))
                    PlayerDeathManager.TeleportWithinMap(client, home);
            };
            named.Add(ranger.EntityId);
            return named;
        }

        /// <summary>The player and the squad members on this map within radius of a point.</summary>
        public static List<Game.Client> SquadAround(MapChannel map, Game.Client client, Vector3 centre, float radius)
        {
            var squad = new List<Game.Client> { client };
            if (PartyManager.Instance.PartyOf(client) is { } party)
                foreach (var member in party.Members)
                    if (member.EntityId != client.Player.EntityId &&
                        map.ClientList?.FirstOrDefault(c => c?.Player?.EntityId == member.EntityId) is { } mate &&
                        mate.Player.State != CharacterState.Dead && Vector3.Distance(mate.Player.Position, centre) <= radius)
                        squad.Add(mate);
            return squad;
        }

        /// <summary>Living, non-AFS creatures within radius metres of a point, from the cells around the performer.</summary>
        public static System.Collections.Generic.List<Creature> HostilesAround(MapChannel map, Actor performer, Vector3 centre, float radius)
        {
            var found = new System.Collections.Generic.List<Creature>();
            if (performer?.Cells == null || radius <= 0)
                return found;
            foreach (var seed in performer.Cells)
                if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    foreach (var creature in cell.CreatureList)
                        if (creature.State != CharacterState.Dead && creature.Faction != Factions.AFS && !found.Contains(creature) &&
                            (creature.MapChannel == null || ReferenceEquals(creature.MapChannel, map)) &&
                            Vector3.Distance(centre, creature.Position) <= radius)
                            found.Add(creature);
            return found;
        }

        public static void Stun(MapChannel map, Creature target, int durationMs)
        {
            target.StunnedUntil = Environment.TickCount64 + durationMs;
            GameEffectManager.Instance.AttachTimedDebuff(map, target, StunEffectType, 1, durationMs);
            map.CreaturesWithEffects.Add(target);
        }

        public static void Knockback(MapChannel map, Actor source, Creature target, int distance, int durationMs)
        {
            var away = new Vector3(target.Position.X - source.Position.X, 0, target.Position.Z - source.Position.Z);
            if (away.LengthSquared() < 0.0001f)
                return;
            away = Vector3.Normalize(away);
            var wanted = target.Position + away * distance;
            var path = NavMeshManager.FindPath(map, target.Position, wanted);
            var landing = path != null && path.Count > 0 ? path.Last() : target.Position;
            target.Position = NavMeshManager.SnapToGround(map, landing);
            CellManager.Instance.CellMoveObject(target, new Movement(target.Position, 0f, 0x08, new Vector2((float)target.Rotation, 0f)));
            GameEffectManager.Instance.AttachTimedDebuff(map, target, KnockbackEffectType, 1, durationMs);
            map.CreaturesWithEffects.Add(target);
        }
    }
}
