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
        public const int AirStrikeType = 393;                 // FIRE_SUPPORT_AIR_STRIKE_EFFECT
        public const int NapalmPoolType = 394;                // FIRE_SUPPORT_NAPALM_POOL_EFFECT
        public const int IonStrikeType = 395;                 // FIRE_SUPPORT_ION_STRIKE_EFFECT
        public const int NapalmBombType = 396;                // FIRE_SUPPORT_NAPALM_BOMB_EFFECT
        public const int ReflectionType = 93;                 // REFLECTION
        public const int ConversionType = 199;                // CONVERSION
        public const int ShieldWaveType = 201;                // SHIELDWAVEEFFECT
        public const int RegenerationWaveType = 10000019;     // REGENERATIONWAVE
        public const int ResistanceType = 10000085;           // RESISTANCE, "Damage Resist: %(resistMod)s"
        public const int DiseaseType = 179;                   // DISEASE_EFFECT, "Spirit: %(modifier)s%%"
        public const int DamageConversionType = 354;          // DAMAGE_CONVERSION, "Virulent Damage dealt converted to: ..."
        public const int SacrificeType = 192;                 // SACRIFICE, "Damage Modifier / Resist Modifier / Threat Modifier"
        public const int SelfDestructLocationType = 10000031; // SELF_DESTRUCT_LOCATION
        public const int SelfDestructBombType = 10000032;     // SELF_DESTRUCT_BOMB
        public const int ScatterBombType = 355;               // SCATTER_BOMB_EXPLOSION
        public const int WeaponEnhancementType = 10000035;    // WEAPON_ENHANCEMENT (Shredder Ammo)
        public const int CalledShotType = 290;                // CALLED_SHOT_EFFECT, "A sniper is aiming a deadly shot at you!"
        public const int CalledLegType = 291;                 // "Run speed reduced by %(movementMod)s%%"
        public const int CalledArmType = 292;                 // "Your attack rate is slowed."
        public const int CalledEyeType = 293;                 // "Ranged attack damage reduced by %(damageMod)s%%"
        public const int CalledChestType = 294;               // "You are bleeding profusely!"
        public const int CalledHeadType = 295;                // CALLED_SHOT_HEAD
        public const int FeedbackType = 383;                  // FEEDBACK_EFFECT
        public const int RealityRipperSourceType = 203;       // REALITY_RIPPER_SOURCE
        public const int RealityRipperTargetType = 204;       // REALITY_RIPPER_TARGET
        public const int ControlledFissionType = 207;         // CONTROLLED_FISSION_EFFECT
        public const int ExplodingNanitesType = 10000020;     // EXPLODING_NANITES_EFFECT, "Increases damage taken by this target"
        public const int PolarityFieldType = 262;             // POLARITY_FIELD, "Converts one or more damage resistances into vulnerabilities"
        public const int StealthType = 123;                   // STEALTH_EFFECT (Cloak Wave's targetGameEffect)
        public const int TraitorType = 10000057;              // TRAITOR_EFFECT
        public const int CorpseImmolationType = 126;          // CORPSE_IMMOLATION
        public const int HackedType = 223;                    // HACKED_EFFECT
        public const int MindControlType = 168;               // MIND_CONTROL_EFFECT
        public const int CritWaveType = 202;                  // CRITWAVEEFFECT, "Crit Hit: +%(critAmt)s%%"
        public const int BaseWaveType = 206;                  // BASEWAVEEFFECT
        public const int HortimunculusBuffType = 225;         // HORTIMONCULUS_BUFF, "Improves various damage resistances and heals damage over time"
        public const int CrabMineExplosionType = 10000088;    // CRAB_MINE_EXPLOSION
        public const int TrapExplosionType = 10000042;        // TRAP_EXPLOSION_EFFECT
        public const int PolymorphType = 10000039;            // POLYMORPH_EFFECT
        public const int PaintTargetType = 10000045;          // PAINT_TARGET_EFFECT, "Reduced Cover / Armor Recharge / Armor Piercing"
        public const int CureReviveType = 167;                // CURE_REVIVE_EFFECT
        public const int CureDebuffGuardType = 181;           // CURE_DEBUFF_GUARD
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
                    AbilityEffects.Heal(map, member.Player, random.Next(healMin, healMax + 1));
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
                // The one teleport in this world whose effect the client names: teleportertype has
                // TACTICAL_EVASION = 7 -> (36108, None, 45518), and the effect this ability attaches
                // is TACTICAL_EVASION_TELEPORT_EFFECT (EvasionTeleportType above). Everything else
                // here teleports as DEFAULT.
                if (effect.EffectTime >= effect.Duration && ranger.State != CharacterState.Dead && ReferenceEquals(ranger.MapChannel, map))
                    PlayerDeathManager.TeleportWithinMap(client, home, TeleportType.TacticalEvasion);
            };
            named.Add(ranger.EntityId);
            return named;
        }

        /// <summary>
        /// Fire Support: "Damages a single enemy target or location after a set time" (the skill text: pumps 1 and 3 AoE
        /// at a location, 2 and 4 a single target with splash and stun, 5 AoE with a pool of fire). After DELAY_TIME_MS
        /// every hostile within EFFECT_RADIUS of the point (or of the target, where it still stands) takes a DAMAGE_AMOUNT
        /// roll; at the targeted pumps each is stunned for EFFECT_DURATION_MS; at the pool pump the napalm then burns
        /// every INTERVAL_MS for EFFECT_DURATION_MS. The strike is shown as the client's strike effect on the ranger,
        /// with its damage announced through it: air strike at a location, ion strike on a target, napalm bomb and pool
        /// for the fire (which effect goes with which pump is inferred from the skill text).
        /// </summary>
        public static void FireSupport(MapChannel map, Manifestation ranger, ActionLevelInfo info, Vector3? location, Creature target, Random random)
        {
            if (map == null || ranger == null || location == null && target == null)
                return;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), ranger.Level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), ranger.Level, scaleType));
            var radius = info.Get(AbilityProperty.EffectRadius);
            var delay = Math.Max(1, info.Get(AbilityProperty.DelayTimeMs));
            var after = info.Get(AbilityProperty.EffectDurationMs);
            var poolInterval = info.Get(AbilityProperty.IntervalMs);
            var napalm = poolInterval > 0 && after > 0;
            var stuns = target != null && !napalm && after > 0;
            var strikeType = napalm ? NapalmBombType : target != null ? IonStrikeType : AirStrikeType;

            List<(ulong, HitData)> Burn()
            {
                var centre = target != null && target.State != CharacterState.Dead ? target.Position : location ?? target.Position;
                var hits = new List<(ulong, HitData)>();
                foreach (var enemy in HostilesAround(map, ranger, centre, radius))
                {
                    hits.Add((enemy.EntityId, MissileManager.Instance.DamageTick(map, ranger, enemy,
                        DamageModifiers.Outgoing(ranger, random.Next(min, max + 1)), DamageType.Physical)));
                    if (stuns && enemy.State != CharacterState.Dead)
                        Stun(map, enemy, after);
                }
                return hits;
            }

            GameEffectManager.Instance.AttachEffect(map, ranger, strikeType, info.Level, delay, ranger.EntityId, true,
                new Dictionary<string, double>(), delay, strike =>
                {
                    var hits = Burn();
                    if (hits.Count > 0)
                        CellManager.Instance.CellCallMethod(map, ranger, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(strike.EffectId, hits));
                    if (!napalm)
                        return;
                    GameEffectManager.Instance.AttachEffect(map, ranger, NapalmPoolType, info.Level, after, ranger.EntityId, true,
                        new Dictionary<string, double>(), poolInterval, pool =>
                        {
                            var burned = Burn();
                            if (burned.Count > 0)
                                CellManager.Instance.CellCallMethod(map, ranger, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(pool.EffectId, burned));
                        });
                });
        }

        /// <summary>
        /// Cure: "Removing debuffs, Preventing debuffs for a time, and Resurrecting with no penalties and a summon to the
        /// user" (the skill text; pumps 1 single, 2 self and squad, 3 resuscitate single, 4 debuffs and protection,
        /// 5 resuscitate squad). From the row: ATTRIBUTE_MAX_CHANGE marks a resuscitation - the dead come back at that
        /// percent of their health beside the biotechnician; RADIUS_AROUND_SOURCE makes it the squad within it; DURATION
        /// adds the debuff guard for that long. Every other pump removes the debuffs of whoever it reaches - all harmful
        /// effects except Resuscitation Trauma, which is the death penalty rather than a combat debuff (inferred).
        /// </summary>
        public static Packets.MapChannel.Server.PerformRecovery.CureRecovery Cure(MapChannel map, Game.Client biotech, Game.Client target,
            ActionLevelInfo info, ActionId actionId, uint level)
        {
            var reached = new List<ulong>();
            var revived = new List<ulong>();
            var guarded = new List<ulong>();
            var reviveShare = info.Get(AbilityProperty.AttributeMaxChange);
            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            var members = new List<Game.Client>();
            if (radius > 0)
            {
                members.Add(biotech);
                if (PartyManager.Instance.PartyOf(biotech) is { } party)
                    foreach (var member in party.Members)
                        if (member.EntityId != biotech.Player.EntityId &&
                            map.ClientList?.FirstOrDefault(c => c?.Player?.EntityId == member.EntityId) is { } mate &&
                            Vector3.Distance(mate.Player.Position, biotech.Player.Position) <= radius)
                            members.Add(mate);
            }
            else if (target != null)
                members.Add(target);

            foreach (var member in members)
            {
                var player = member.Player;
                reached.Add(player.EntityId);
                if (reviveShare > 0)
                {
                    if (player.State == CharacterState.Dead)
                    {
                        PlayerDeathManager.Instance.Resuscitate(member, biotech.Player.Position, reviveShare);
                        revived.Add(player.EntityId);
                    }
                    continue;
                }
                if (player.State == CharacterState.Dead)
                    continue;
                foreach (var debuff in player.ActiveEffects.Values.Where(e => e.IsDebuff &&
                             e.TypeId != DeathPenaltyRules.RezSicknessEffectType && e.TypeId != DeathPenaltyRules.RezSicknessNoHealEffectType).ToList())
                    GameEffectManager.Instance.DettachEffect(map, player, debuff);
                if (info.Has(AbilityProperty.Duration))
                {
                    GameEffectManager.Instance.AttachEffect(map, player, CureDebuffGuardType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                        biotech.Player.EntityId, true, new Dictionary<string, double>());
                    guarded.Add(player.EntityId);
                }
            }
            return new Packets.MapChannel.Server.PerformRecovery.CureRecovery(actionId, level, reached, revived, guarded);
        }

        /// <summary>
        /// A player took damage: Reflection sends its percent of a hit of a reflected type back at a creature that dealt
        /// it (reflection.py Recv_AnnounceReflect(entityId, rawInfo, delayMs)), and Conversion turns its percent of the
        /// hit into healing for the squad within its radius (conversion.py Recv_AnnounceHealing([(entityId, amount)])).
        /// </summary>
        public static void OnPlayerDamaged(MapChannel map, Actor victim, Actor source, int damage, DamageType? type)
        {
            if (map == null || victim == null || damage <= 0)
                return;
            foreach (var effect in victim.ActiveEffects.Values.ToList())
            {
                if (effect.ReflectTypes != null && type is { } dealt && effect.ReflectTypes.Contains(dealt) &&
                    source is Creature attacker && attacker.State != CharacterState.Dead)
                {
                    var hit = MissileManager.Instance.DamageTick(map, victim, attacker, damage * effect.ReflectPercent / 100, dealt);
                    CellManager.Instance.CellCallMethod(map, victim, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceReflect(
                        effect.EffectId, attacker.EntityId, hit, 0));
                }
                if (effect.ConversionHealPercent > 0 && effect.ConversionClient != null)
                {
                    var amount = damage * effect.ConversionHealPercent / 100;
                    var healed = new List<(ulong, int)>();
                    foreach (var member in SquadAround(map, effect.ConversionClient, victim.Position, effect.ConversionRadius))
                    {
                        if (member.Player == victim)
                            continue;
                        Heal(map, member.Player, amount);
                        healed.Add((member.Player.EntityId, amount));
                    }
                    if (healed.Count > 0)
                        CellManager.Instance.CellCallMethod(map, victim, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceHealing(effect.EffectId, healed));
                }
            }
        }

        /// <summary>Heals a player, unless an effect on them prevents healing (Disease, pump 5).</summary>
        public static void Heal(MapChannel map, Actor player, int amount)
        {
            if (amount <= 0 || player.ActiveEffects.Values.Any(e => e.PreventsHealing) ||
                !player.Attributes.TryGetValue(Attributes.Health, out var health))
                return;
            health.Current = Math.Min(health.CurrentMax, health.Current + amount);
            CellManager.Instance.CellCallMethod(map, player, new Packets.MapChannel.Server.UpdateHealthPacket(health, player.EntityId));
        }

        /// <summary>
        /// Reflection: "a damage reflection effect that sends a portion of the damage back to the source" for DURATION;
        /// the types reflected accumulate over the pumps ("Pump 2: Adds Sonic ..."), so the effect reflects the
        /// DAMAGE_TYPE of every row up to the one used, at DAMAGE_PERCENT.
        /// </summary>
        public static void Reflection(MapChannel map, Manifestation guardian, ActionId actionId, ActionLevelInfo info)
        {
            var types = new HashSet<DamageType>();
            for (uint pump = 1; pump <= info.Level; pump++)
                if (ActionTableManager.Instance.TryGetLevel(actionId, pump, out _, out var row) && row.Has(AbilityProperty.DamageType))
                    types.Add((DamageType)row.Get(AbilityProperty.DamageType));
            var reflect = GameEffectManager.Instance.AttachEffect(map, guardian, ReflectionType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                guardian.EntityId, true, new Dictionary<string, double>());
            reflect.ReflectTypes = types;
            reflect.ReflectPercent = info.Get(AbilityProperty.DamagePercentMax, info.Get(AbilityProperty.DamagePercentMin));
        }

        /// <summary>
        /// Conversion: "Debuffs the user by increasing the amount of damage they take from all attacks for a set time.
        /// The damage is also converted to healing and applied to all nearby squad members" - DAMAGE_PERCENT more taken,
        /// HEAL_PERCENT of each hit healed on the squad within EFFECT_RADIUS, for DURATION.
        /// </summary>
        public static void Conversion(MapChannel map, Game.Client guardian, ActionLevelInfo info)
        {
            var conversion = GameEffectManager.Instance.AttachEffect(map, guardian.Player, ConversionType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                guardian.Player.EntityId, true, new Dictionary<string, double>());
            conversion.DamageTakenPercent = info.Get(AbilityProperty.DamagePercentMax, info.Get(AbilityProperty.DamagePercentMin));
            conversion.ConversionHealPercent = info.Get(AbilityProperty.HealPercentMax, info.Get(AbilityProperty.HealPercentMin));
            conversion.ConversionRadius = info.Get(AbilityProperty.EffectRadius);
            conversion.ConversionClient = guardian;
        }

        /// <summary>
        /// Shield Wave: "Buffs the user and nearby squad members with damage absorption for all attacks for a set amount
        /// of damage or a set time" - each within RADIUS_AROUND_SOURCE absorbs EFFECT_MODIFIER damage, scaled to the
        /// guardian's level by ATTR_SCALE_TYPE, for DURATION.
        /// </summary>
        public static List<ulong> ShieldWave(MapChannel map, Game.Client guardian, ActionLevelInfo info)
        {
            var amount = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.EffectModifier), guardian.Player.Level,
                info.Has(AbilityProperty.AttrScaleType) ? info.Get(AbilityProperty.AttrScaleType) : (int?)null);
            var shielded = new List<ulong>();
            foreach (var member in SquadAround(map, guardian, guardian.Player.Position, info.Get(AbilityProperty.RadiusAroundSource)))
            {
                GameEffect wave = null;
                var pool = new ShieldPool { Percent = 100, Remaining = amount };
                wave = GameEffectManager.Instance.AttachEffect(map, member.Player, ShieldWaveType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                    guardian.Player.EntityId, true, new Dictionary<string, double> { ["amount"] = amount });
                wave.Shield = pool;
                pool.Broken = () => GameEffectManager.Instance.DettachEffect(map, member.Player, wave);
                shielded.Add(member.Player.EntityId);
            }
            return shielded;
        }

        /// <summary>
        /// Regeneration Wave: "Buffs the user and nearby squad members with increased Health and Power regeneration for a
        /// time" - ATTRIBUTE_PERCENT added to the regeneration rate of each within RADIUS_AROUND_SOURCE, for DURATION.
        /// </summary>
        public static List<ulong> RegenerationWave(MapChannel map, Game.Client medic, ActionLevelInfo info)
        {
            var touched = new List<ulong>();
            foreach (var member in SquadAround(map, medic, medic.Player.Position, info.Get(AbilityProperty.RadiusAroundSource)))
            {
                var wave = GameEffectManager.Instance.AttachEffect(map, member.Player, RegenerationWaveType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                    medic.Player.EntityId, true, new Dictionary<string, double> { ["regenMod"] = info.Get(AbilityProperty.AttributePercent) });
                wave.RegenBonusPercent = info.Get(AbilityProperty.AttributePercent);
                void Refresh()
                {
                    ManifestationManager.Instance.UpdateStatsValues(member, false);
                    member.CallMethod(member.Player.EntityId, new Packets.MapChannel.Server.AttributeInfoPacket(member.Player.Attributes));
                }
                wave.OnDetach = _ => Refresh();
                Refresh();
                touched.Add(member.Player.EntityId);
            }
            return touched;
        }

        /// <summary>
        /// Resistance: "an aura that increases all resistances based on the ability's rank. This aura will effect all
        /// squad mates within it" - every INTERVAL for DURATION, the medic and squad within RADIUS_AROUND_SOURCE carry
        /// RESISTANCE with RESIST_MODIFIER added to their resistance rating against every type.
        /// </summary>
        public static void Resistance(MapChannel map, Game.Client medic, ActionLevelInfo info)
        {
            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            var rating = info.Get(AbilityProperty.ResistModifier);
            var given = new Dictionary<Actor, GameEffect>();
            var aura = GameEffectManager.Instance.AttachEffect(map, medic.Player, ResistanceType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                medic.Player.EntityId, true, new Dictionary<string, double> { ["resistMod"] = rating },
                Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000, pulse =>
                {
                    var inside = SquadAround(map, medic, medic.Player.Position, radius).Select(c => (Actor)c.Player).Where(a => a != medic.Player).ToList();
                    foreach (var (holder, effect) in given.ToList())
                        if (!inside.Contains(holder))
                        {
                            GameEffectManager.Instance.DettachEffect(map, holder, effect);
                            given.Remove(holder);
                        }
                    foreach (var holder in inside.Where(h => !given.ContainsKey(h)))
                    {
                        var resist = GameEffectManager.Instance.AttachEffect(map, holder, ResistanceType, info.Level,
                            (int)Math.Max(0, pulse.Duration - pulse.EffectTime), medic.Player.EntityId, true,
                            new Dictionary<string, double> { ["resistMod"] = rating });
                        resist.ResistRating = rating;
                        given[holder] = resist;
                    }
                });
            aura.ResistRating = rating;
            aura.OnDetach = _ =>
            {
                foreach (var (holder, effect) in given.ToList())
                    GameEffectManager.Instance.DettachEffect(map, holder, effect);
            };
            aura.OnTick(aura);
        }

        /// <summary>
        /// Disease on one enemy for DURATION: "Pump 1: -Spirit | Pump 2: -Body | Pump 3: -Mind | Pump 4: Stops Health
        /// and Power Regen | Pump 5: Prevents Healing, Stops Health Regen". ATTRIBUTE_ID loses ATTRIBUTE_MAX_CHANGE
        /// percent (shown as the tooltip's modifier); the regeneration and healing modifiers of 0 stop them. Creature
        /// attributes other than health do not enter this server's combat, so the attribute loss is shown, not felt.
        /// </summary>
        public static void Disease(MapChannel map, Manifestation medic, Creature target, ActionLevelInfo info)
        {
            if (target == null || target.State == CharacterState.Dead)
                return;
            var disease = GameEffectManager.Instance.AttachEffect(map, target, DiseaseType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                medic.EntityId, false, new Dictionary<string, double> { ["modifier"] = -info.Get(AbilityProperty.AttributeMaxChange) });
            disease.StopsRegeneration = info.Has(AbilityProperty.EffectHealthRegenModifier);
            disease.PreventsHealing = info.Has(AbilityProperty.HealingModifier);
        }

        /// <summary>
        /// Viral Conversion: "converts all virulent weapon damage done by the user into another damage type for a set
        /// time" - DAMAGE_TYPE, for DURATION.
        /// </summary>
        public static void ViralConversion(MapChannel map, Manifestation medic, ActionLevelInfo info)
        {
            var to = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var conversion = GameEffectManager.Instance.AttachEffect(map, medic, DamageConversionType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                medic.EntityId, true, new Dictionary<string, double> { ["damageType"] = (int)to });
            conversion.ConvertVirulentTo = to;
        }

        /// <summary>
        /// A creature is about to take a hit: Shredder Ammo adds its extra damage (once per interval, weapon hits only),
        /// a called shot springs on the sniper's next hit, and Sacrifice's threat turns the creature on the grenadier.
        /// Ability ticks are not weapon hits and trigger none of these.
        /// </summary>
        public static void OnCreatureHit(MapChannel map, Missile missile, Creature creature, HitData hit)
        {
            var source = missile.Source;
            if (map == null || source == null || missile.IsAbility)
                return;
            foreach (var effect in creature.ActiveEffects.Values.Where(e => e.CalledBy == source && e.OnCalledHit != null).ToList())
            {
                effect.OnCalledHit(missile);
                GameEffectManager.Instance.DettachEffect(map, creature, effect);
            }
            var now = Environment.TickCount64;
            foreach (var shredder in source.ActiveEffects.Values.Where(e => e.ShredderType != null && e.ShredderReadyAt <= now).ToList())
            {
                shredder.ShredderReadyAt = now + shredder.ShredderIntervalMs;
                var extra = MissileManager.Instance.DamageTick(map, source, creature, shredder.ShredderDamage, shredder.ShredderType.Value);
                CellManager.Instance.CellCallMethod(map, source, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(
                    shredder.EffectId, new[] { (creature.EntityId, extra) }));
            }
            if (source.ActiveEffects.Values.Any(e => e.DrawsThreat) && creature.State != CharacterState.Dead)
                BehaviorManager.Instance.SetActionFighting(creature, source.EntityId);
        }

        /// <summary>
        /// Sacrifice: "increasing damage by sacrificing mitigation or increasing mitigation by sacrificing damage.
        /// Sacrificing damage to raise mitigation will also increase the threat generated" - OFFENSIVE_DAMAGE_MODIFIER
        /// percent on the grenadier's damage, RESIST_MODIFIER on their resistance rating, and, where the row has threat,
        /// every creature they hit turns on them (this server has no threat list; turning the creature is inferred). The
        /// rows carry no duration: it holds until used again, which ends it (inferred, like Rage's toggle).
        /// </summary>
        public static void Sacrifice(MapChannel map, Manifestation grenadier, ActionLevelInfo info)
        {
            var damage = info.Get(AbilityProperty.OffensiveDamageModifier);
            var resist = info.Get(AbilityProperty.ResistModifier);
            var threat = info.Get(AbilityProperty.ThreatModifierPercent);
            var sacrifice = GameEffectManager.Instance.AttachEffect(map, grenadier, SacrificeType, info.Level, 0, grenadier.EntityId, true,
                new Dictionary<string, double> { ["dmgMod"] = damage, ["resistMod"] = resist, ["threatMod"] = threat });
            sacrifice.DamageBonusPercent = damage;
            sacrifice.ResistRating = resist;
            sacrifice.DrawsThreat = threat > 0;
        }

        /// <summary>
        /// Self Destruct: "marks their location and causes them to explode after a set time. User can self-detonate before
        /// time has elapsed by activating the ability again. The explosion will damage ... all enemies within the blast
        /// radius. User will be teleported back to the marked location." The bomb lasts DURATION; when it ends - run out
        /// or ended by the second activation - every hostile within EFFECT_RADIUS takes a DAMAGE_AMOUNT roll and the
        /// demolitionist returns to the mark. The damage the user takes is not in the row and is not dealt.
        /// </summary>
        public static void SelfDestruct(MapChannel map, Game.Client client, ActionLevelInfo info, Random random)
        {
            var demolitionist = client.Player;
            var home = demolitionist.Position;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), demolitionist.Level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), demolitionist.Level, scaleType));
            var radius = info.Get(AbilityProperty.EffectRadius);
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var mark = GameEffectManager.Instance.AttachEffect(map, demolitionist, SelfDestructLocationType, info.Level, duration, demolitionist.EntityId, true,
                new Dictionary<string, double>());
            var bomb = GameEffectManager.Instance.AttachEffect(map, demolitionist, SelfDestructBombType, info.Level, duration, demolitionist.EntityId, true,
                new Dictionary<string, double>());
            bomb.OnDetach = effect =>
            {
                if (demolitionist.State == CharacterState.Dead || !ReferenceEquals(demolitionist.MapChannel, map))
                    return;
                var hits = HostilesAround(map, demolitionist, demolitionist.Position, radius)
                    .Select(enemy => (enemy.EntityId, MissileManager.Instance.DamageTick(map, demolitionist, enemy,
                        DamageModifiers.Outgoing(demolitionist, random.Next(min, max + 1)), DamageType.Physical))).ToList();
                if (hits.Count > 0)
                    CellManager.Instance.CellCallMethod(map, demolitionist, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(effect.EffectId, hits));
                GameEffectManager.Instance.DettachEffect(map, demolitionist, mark);
                PlayerDeathManager.TeleportWithinMap(client, home);
            };
        }

        /// <summary>
        /// Scatterbombs: "Damages all enemies in an area around the user" - bombs scattered within RADIUS_AROUND_SOURCE,
        /// each bursting over EFFECT_RADIUS, so every hostile within their sum takes a DAMAGE_AMOUNT roll of the row's
        /// DAMAGE_TYPE (the reach as the sum is inferred), shown as SCATTER_BOMB_EXPLOSION on the grenadier.
        /// </summary>
        public static void Scatterbombs(MapChannel map, Manifestation grenadier, ActionLevelInfo info, Random random)
        {
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), grenadier.Level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), grenadier.Level, scaleType));
            var type = DamageModifiers.DealtType(grenadier, (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical)) ?? DamageType.Physical;
            var reach = info.Get(AbilityProperty.RadiusAroundSource) + info.Get(AbilityProperty.EffectRadius);
            var burst = GameEffectManager.Instance.AttachEffect(map, grenadier, ScatterBombType, info.Level, 1000, grenadier.EntityId, true, new Dictionary<string, double>());
            var hits = HostilesAround(map, grenadier, grenadier.Position, reach)
                .Select(enemy => (enemy.EntityId, MissileManager.Instance.DamageTick(map, grenadier, enemy,
                    DamageModifiers.Outgoing(grenadier, random.Next(min, max + 1)), type))).ToList();
            if (hits.Count > 0)
                CellManager.Instance.CellCallMethod(map, grenadier, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(burst.EffectId, hits));
        }

        /// <summary>
        /// Shredder Ammo: "additional damage for weapon attacks for a set time. Extra damage is applied once per interval
        /// when damage is done to the target" - DAMAGE_AMOUNT of DAMAGE_TYPE on the holder's weapon hits for DURATION_MS.
        /// The interval is not in the row (it shrinks with proficiency); one second is inferred.
        /// </summary>
        public static void ShredderAmmo(MapChannel map, Manifestation sniper, Game.Client targetClient, ActionLevelInfo info)
        {
            var holder = targetClient?.Player ?? sniper;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var shredder = GameEffectManager.Instance.AttachEffect(map, holder, WeaponEnhancementType, info.Level, info.Get(AbilityProperty.DurationMs),
                sniper.EntityId, true, new Dictionary<string, double>());
            shredder.ShredderType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            shredder.ShredderDamage = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), sniper.Level, scaleType);
            shredder.ShredderIntervalMs = DefaultPulseMs;
        }

        /// <summary>
        /// Called Shot: the target carries CALLED_SHOT_EFFECT ("A sniper is aiming a deadly shot at you!") for
        /// EFFECT_DURATION_MS, and the sniper's next weapon hit on it springs the called part for DURATION (that the next
        /// hit springs it is inferred from the effect's text). Which part is the row's: EFFECT_MOVEMENT_MODIFIER, the leg
        /// (run speed down that percent); EFFECT_MODIFIER, the arm (attacks that percent slower); INTERVAL, the chest
        /// (DAMAGE_MODIFIER_PERCENT of the hit bled every INTERVAL); a negative DAMAGE_MODIFIER_PERCENT, the eye (its damage
        /// down that much); a positive one, the head (that hit that much harder).
        /// </summary>
        public static void CalledShot(MapChannel map, Manifestation sniper, Creature target, ActionLevelInfo info)
        {
            if (target == null || target.State == CharacterState.Dead)
                return;
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var mark = GameEffectManager.Instance.AttachEffect(map, target, CalledShotType, info.Level, info.Get(AbilityProperty.EffectDurationMs),
                sniper.EntityId, false, new Dictionary<string, double>());
            mark.CalledBy = sniper;
            mark.OnCalledHit = missile =>
            {
                if (info.Has(AbilityProperty.EffectMovementModifier))
                {
                    var slow = info.Get(AbilityProperty.EffectMovementModifier);
                    var (walk, run) = (target.WalkSpeed, target.RunSpeed);
                    target.WalkSpeed = walk * (100 - slow) / 100f;
                    target.RunSpeed = run * (100 - slow) / 100f;
                    GameEffectManager.Instance.AttachEffect(map, target, CalledLegType, info.Level, duration, sniper.EntityId, false,
                        new Dictionary<string, double> { ["movementMod"] = slow }).OnDetach = _ => { target.WalkSpeed = walk; target.RunSpeed = run; };
                }
                else if (info.Has(AbilityProperty.EffectModifier))
                    GameEffectManager.Instance.AttachEffect(map, target, CalledArmType, info.Level, duration, sniper.EntityId, false,
                        new Dictionary<string, double>()).AttackDelayPercent = info.Get(AbilityProperty.EffectModifier);
                else if (info.Has(AbilityProperty.Interval))
                {
                    var bleed = Math.Max(1, missile.DamageA * info.Get(AbilityProperty.DamageModifierPercent) / 100);
                    GameEffectManager.Instance.AttachEffect(map, target, CalledChestType, info.Level, duration, sniper.EntityId, false,
                        new Dictionary<string, double>(), info.Get(AbilityProperty.Interval) * 1000, tick =>
                        {
                            if (target.State == CharacterState.Dead)
                                return;
                            var hit = MissileManager.Instance.DamageTick(map, sniper, target, bleed, DamageType.Physical);
                            CellManager.Instance.CellCallMethod(map, target, new Packets.MapChannel.Server.GameEffectTickPacket(tick.EffectId, new[] { (target.EntityId, hit) }));
                        });
                }
                else if (info.Get(AbilityProperty.DamageModifierPercent) < 0)
                    GameEffectManager.Instance.AttachEffect(map, target, CalledEyeType, info.Level, duration, sniper.EntityId, false,
                        new Dictionary<string, double> { ["damageMod"] = -info.Get(AbilityProperty.DamageModifierPercent) }).DamageBonusPercent =
                        info.Get(AbilityProperty.DamageModifierPercent);
                else
                {
                    missile.DamageA = missile.DamageA * (100 + info.Get(AbilityProperty.DamageModifierPercent)) / 100;
                    GameEffectManager.Instance.AttachEffect(map, target, CalledHeadType, info.Level, duration, sniper.EntityId, false, new Dictionary<string, double>());
                }
            };
        }

        /// <summary>A creature took a hit: Explosive Nanites go off (never from their own explosion).</summary>
        public static void OnCreatureDamaged(Creature creature)
        {
            foreach (var effect in creature.ActiveEffects.Values.Where(e => e.OnDamaged != null && !e.Busy).ToList())
            {
                effect.Busy = true;
                try { effect.OnDamaged(effect); }
                finally { effect.Busy = false; }
            }
        }

        /// <summary>A creature attacked: Feedback answers it.</summary>
        public static void OnCreatureAttack(Creature creature)
        {
            foreach (var effect in creature.ActiveEffects.Values.Where(e => e.OnAttack != null).ToList())
                effect.OnAttack(effect);
        }

        /// <summary>Cloak Wave: any combat action ends the holder's stealth.</summary>
        public static void BreakStealth(MapChannel map, Actor actor)
        {
            if (actor == null)
                return;
            foreach (var effect in actor.ActiveEffects.Values.Where(e => e.Stealth).ToList())
                GameEffectManager.Instance.DettachEffect(map, actor, effect);
        }

        private static (int Min, int Max) DamageRange(Actor source, ActionLevelInfo info)
        {
            var level = source is Manifestation player ? player.Level : 1;
            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), level, scaleType);
            var max = Math.Max(min, AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), level, scaleType));
            return (min, max);
        }

        private static void Blast(MapChannel map, Actor source, GameEffect shownOn, Actor shownAt, Vector3 centre, float radius,
            int min, int max, DamageType type, Random random)
        {
            var hits = HostilesAround(map, source, centre, radius)
                .Select(enemy => (enemy.EntityId, MissileManager.Instance.DamageTick(map, source, enemy,
                    DamageModifiers.Outgoing(source, random.Next(min, max + 1)), type))).ToList();
            if (hits.Count > 0 && shownOn != null)
                CellManager.Instance.CellCallMethod(map, shownAt, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(shownOn.EffectId, hits));
        }

        /// <summary>
        /// Controlled Fission: "causes them to explode once they have taken a set amount of damage, or after a set time.
        /// The explosion will damage all enemies within the blast radius." After DELAY_TIME_MS the target bursts,
        /// DAMAGE_AMOUNT to every hostile within EFFECT_RADIUS of it. The damage threshold that sets it off early is not in
        /// the row and is not applied.
        /// </summary>
        public static void ControlledFission(MapChannel map, Manifestation demolitionist, Creature target, ActionLevelInfo info, Random random)
        {
            if (target == null)
                return;
            var (min, max) = DamageRange(demolitionist, info);
            var delay = Math.Max(1, info.Get(AbilityProperty.DelayTimeMs));
            GameEffectManager.Instance.AttachEffect(map, target, ControlledFissionType, info.Level, delay, demolitionist.EntityId, false,
                new Dictionary<string, double>(), delay, fission =>
                    Blast(map, demolitionist, fission, target, target.Position, info.Get(AbilityProperty.EffectRadius), min, max, DamageType.Physical, random));
        }

        /// <summary>
        /// Explosive Nanites: "causes an explosion each time the enemy takes damage from any source. Effect lasts until all
        /// explosions are triggered or a set time has passed" - USE_COUNT explosions of DAMAGE_AMOUNT of the row's
        /// DAMAGE_TYPE on the target within DURATION, each USE_DROPOFF percent weaker than the last (the dropoff's reading
        /// is inferred).
        /// </summary>
        public static void ExplosiveNanites(MapChannel map, Manifestation demolitionist, Creature target, ActionLevelInfo info, Random random)
        {
            if (target == null)
                return;
            var (min, max) = DamageRange(demolitionist, info);
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var dropoff = info.Get(AbilityProperty.UseDropoff);
            var nanites = GameEffectManager.Instance.AttachEffect(map, target, ExplodingNanitesType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                demolitionist.EntityId, false, new Dictionary<string, double>());
            nanites.NanitesLeft = Math.Max(1, info.Get(AbilityProperty.UseCount, 1));
            var used = 0;
            nanites.OnDamaged = effect =>
            {
                if (target.State == CharacterState.Dead)
                    return;
                var amount = random.Next(min, max + 1) * Math.Max(0, 100 - dropoff * used) / 100;
                used++;
                var hit = MissileManager.Instance.DamageTick(map, demolitionist, target, DamageModifiers.Outgoing(demolitionist, amount), type);
                CellManager.Instance.CellCallMethod(map, target, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(effect.EffectId,
                    new[] { (target.EntityId, hit) }));
                if (--effect.NanitesLeft <= 0)
                    GameEffectManager.Instance.DettachEffect(map, target, effect);
            };
        }

        /// <summary>
        /// Polarity Field: "lowering resistance to a single damage type for a set time" - the row's DAMAGE_TYPE takes
        /// PER_PUMP_MOD (negative) as a resistance rating for DURATION_MS. The skill's proficiency adds to it for every pump
        /// the spy holds; the rating used here is that per-pump amount times the pump used (inferred from "Each pump level
        /// gains an increase to the resistance debuff amount").
        /// </summary>
        public static void PolarityField(MapChannel map, Manifestation spy, Creature target, ActionLevelInfo info)
        {
            if (target == null)
                return;
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var field = GameEffectManager.Instance.AttachEffect(map, target, PolarityFieldType, info.Level, info.Get(AbilityProperty.DurationMs),
                spy.EntityId, false, new Dictionary<string, double> { ["damageType"] = (int)type });
            field.VulnerableType = type;
            field.VulnerableRating = info.Get(AbilityProperty.PerPumpMod) * (int)Math.Max(1, info.Level);
        }

        /// <summary>
        /// Feedback: "Damages a single enemy target when they perform certain actions within a set time" (pumps: healing and
        /// item use, combat actions, healing AoE, attack AoE, all actions AoE). Creatures here neither heal nor use items,
        /// so the triggers that can fire are their attacks: at the rows with a radius (RADIUS_AROUND_TARGET or
        /// EFFECT_RADIUS) every hostile around the target is marked too. Each attack a marked creature makes costs it a
        /// DAMAGE_AMOUNT roll of the row's type, for DURATION.
        /// </summary>
        public static void Feedback(MapChannel map, Manifestation engineer, Creature target, ActionLevelInfo info, Random random)
        {
            if (target == null)
                return;
            var (min, max) = DamageRange(engineer, info);
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var radius = Math.Max(info.Get(AbilityProperty.RadiusAroundTarget), info.Get(AbilityProperty.EffectRadius));
            var marked = new List<Creature> { target };
            if (radius > 0)
                marked.AddRange(HostilesAround(map, engineer, target.Position, radius).Where(c => c != target));
            foreach (var creature in marked)
            {
                var feedback = GameEffectManager.Instance.AttachEffect(map, creature, FeedbackType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                    engineer.EntityId, false, new Dictionary<string, double> { ["level"] = info.Level });
                feedback.OnAttack = effect =>
                {
                    var hit = MissileManager.Instance.DamageTick(map, engineer, creature, DamageModifiers.Outgoing(engineer, random.Next(min, max + 1)), type);
                    CellManager.Instance.CellCallMethod(map, creature, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(effect.EffectId,
                        new[] { (creature.EntityId, hit) }));
                };
            }
        }

        /// <summary>
        /// Reality Ripper: "Creates an effect at the feet of the user that pulls enemies toward it and damages them at
        /// intervals over a set time." Every INTERVAL for DURATION, each hostile within EFFECT_RADIUS of where it was made
        /// takes a DAMAGE_AMOUNT roll and is drawn halfway to the centre (the pull's strength is not in the row; halfway
        /// is inferred), held by REALITY_RIPPER_TARGET while it lasts. The rip being a destroyable object is not built.
        /// </summary>
        public static void RealityRipper(MapChannel map, Manifestation demolitionist, Vector3 centre, ActionLevelInfo info, Random random)
        {
            var (min, max) = DamageRange(demolitionist, info);
            var radius = info.Get(AbilityProperty.EffectRadius);
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var held = new HashSet<Creature>();
            GameEffectManager.Instance.AttachEffect(map, demolitionist, RealityRipperSourceType, info.Level, duration, demolitionist.EntityId, true,
                new Dictionary<string, double>(), Math.Max(1, info.Get(AbilityProperty.Interval, 1)) * 1000, rip =>
                {
                    var hits = new List<(ulong, HitData)>();
                    foreach (var enemy in HostilesAround(map, demolitionist, centre, radius))
                    {
                        if (held.Add(enemy))
                            GameEffectManager.Instance.AttachEffect(map, enemy, RealityRipperTargetType, info.Level,
                                (int)Math.Max(0, rip.Duration - rip.EffectTime), demolitionist.EntityId, false, new Dictionary<string, double>());
                        enemy.Position = NavMeshManager.SnapToGround(map, Vector3.Lerp(enemy.Position, centre, 0.5f));
                        CellManager.Instance.CellMoveObject(enemy, new Movement(enemy.Position, 0f, 0x08, new Vector2((float)enemy.Rotation, 0f)));
                        hits.Add((enemy.EntityId, MissileManager.Instance.DamageTick(map, demolitionist, enemy,
                            DamageModifiers.Outgoing(demolitionist, random.Next(min, max + 1)), DamageType.Physical)));
                    }
                    if (hits.Count > 0)
                        CellManager.Instance.CellCallMethod(map, demolitionist, Packets.MapChannel.Server.CallGameEffectMethodPacket.AnnounceDamage(rip.EffectId, hits));
                });
        }

        /// <summary>
        /// Cloak Wave: "Buffs the user and nearby squad members with world and radar invisibility for a set time. Player
        /// movement speed is reduced, and any combat action will break the effect." Each within RADIUS_AROUND_SOURCE carries
        /// the client's STEALTH_EFFECT for EFFECT_DURATION_MS; creatures do not pick them as targets, and an attack or an
        /// ability ends it. The speed reduction is not in the row and is not applied.
        /// </summary>
        public static void CloakWave(MapChannel map, Game.Client spy, ActionLevelInfo info)
        {
            foreach (var member in SquadAround(map, spy, spy.Player.Position, info.Get(AbilityProperty.RadiusAroundSource)))
                GameEffectManager.Instance.AttachEffect(map, member.Player, StealthType, info.Level, info.Get(AbilityProperty.EffectDurationMs),
                    spy.Player.EntityId, true, new Dictionary<string, double>()).Stealth = true;
        }

        /// <summary>
        /// Traitor: "Changes a single enemy target to the user's faction for a time, causing it to attack and be attacked by
        /// former friendly targets. Mechanized creatures and bosses are immune." For DURATION the creature is AFS - the AI
        /// already sets creatures of different factions on each other - and told the client it is now friendly; it is set
        /// on the nearest former ally within EFFECT_RADIUS. Its faction returns when the effect ends.
        /// </summary>
        public static bool Traitor(MapChannel map, Manifestation spy, Creature target, ActionLevelInfo info)
            => Turn(map, spy, target, info, TraitorType, info.Get(AbilityProperty.Duration) * 1000) != null;

        /// <summary>
        /// Makes a creature AFS for a time (Traitor, Hack, Mind Control 4-5): the AI already sets creatures of different
        /// factions on each other, the client is told it is now friendly, and it is set on the nearest former ally within
        /// EFFECT_RADIUS. Its faction returns when the effect ends.
        /// </summary>
        private static GameEffect Turn(MapChannel map, Actor source, Creature target, ActionLevelInfo info, int effectType, int durationMs)
        {
            if (target == null || target.State == CharacterState.Dead || target.Faction == Factions.AFS)
                return null;
            var faction = target.Faction;
            target.Faction = Factions.AFS;
            CellManager.Instance.CellCallMethod(map, target, new Packets.MapChannel.Server.TargetCategoryPacket(TargetCategory.Friendly));
            if (target.Controller.CurrentAction == BehaviorManager.BehaviorActionFighting)
                BehaviorManager.Instance.DropFight(target);
            var radius = info.Get(AbilityProperty.EffectRadius, 20);
            var former = HostilesAround(map, target, target.Position, radius)
                .Where(c => c != target && c.Faction == faction).OrderBy(c => Vector3.Distance(c.Position, target.Position)).FirstOrDefault();
            if (former != null)
                BehaviorManager.Instance.SetActionFighting(target, former.EntityId);
            var effect = GameEffectManager.Instance.AttachEffect(map, target, effectType, info.Level, durationMs, source.EntityId, false, new Dictionary<string, double>());
            effect.OnDetach = _ =>
            {
                target.Faction = faction;
                CellManager.Instance.CellCallMethod(map, target, new Packets.MapChannel.Server.TargetCategoryPacket(TargetCategory.Hostile));
                BehaviorManager.Instance.DropFight(target);
            };
            return effect;
        }

        /// <summary>
        /// Hack: "Debuffs a single enemy mechanical target making it attack other enemy units within a given radius for a
        /// set time. Larger mechanicals such as Stalkers, Striders and Juggernauts are immune" (hack.py checks MECHANICAL or
        /// MACHINA). Turned for DURATION with HACKED_EFFECT.
        /// </summary>
        public static bool Hack(MapChannel map, Manifestation sapper, Creature target, ActionLevelInfo info)
        {
            var flags = CreatureManager.CreatureFlagsOf(target).Select(f => (CreatureFlag)f).ToList();
            if (!flags.Contains(CreatureFlag.Mechanical) && !flags.Contains(CreatureFlag.Machina) ||
                flags.Any(f => f is CreatureFlag.SpeciesStalker or CreatureFlag.SpeciesStrider or CreatureFlag.SpeciesJuggernaut))
                return false;
            return Turn(map, sapper, target, info, HackedType, info.Get(AbilityProperty.Duration) * 1000) != null;
        }

        /// <summary>
        /// Mind Control on one biological enemy for DURATION, deciding again every INTERVAL (the skill text, PvE: "Pump 1:
        /// Flee Combat | Pump 2: Attack Allies or Enemies randomly | Pump 3: Attack Allies randomly | Pump 4: Assist User,
        /// Attack Allies | Pump 5: Assists User, Apply Mind Control 2"). Fleeing is disengaging and holding still here
        /// (inferred); at pump 5 a creature that attacks the controlled one has PERCENTAGE_CHANCE of falling under pump 2.
        /// </summary>
        public static GameEffect MindControl(MapChannel map, Manifestation medic, Creature target, ActionId actionId, ActionLevelInfo info, Random random)
        {
            if (target == null || target.State == CharacterState.Dead)
                return null;
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var interval = Math.Max(1, info.Get(AbilityProperty.Interval, 4)) * 1000;
            if (info.Level >= 4)
                return Turn(map, medic, target, info, MindControlType, duration);
            var faction = target.Faction;
            return GameEffectManager.Instance.AttachEffect(map, target, MindControlType, info.Level, duration, medic.EntityId, false,
                new Dictionary<string, double>(), interval, control =>
                {
                    if (target.State == CharacterState.Dead)
                        return;
                    if (info.Level == 1)
                    {
                        BehaviorManager.Instance.DropFight(target);
                        target.StunnedUntil = Environment.TickCount64 + interval;
                        return;
                    }
                    var allies = HostilesAround(map, target, target.Position, 30).Where(c => c != target && c.Faction == faction).Cast<Actor>().ToList();
                    if (info.Level == 2)
                        allies.AddRange(map.ClientList?.Where(c => c?.Player != null && c.Player.State != CharacterState.Dead &&
                            Vector3.Distance(c.Player.Position, target.Position) <= 30).Select(c => (Actor)c.Player) ?? Enumerable.Empty<Actor>());
                    if (allies.Count > 0)
                        BehaviorManager.Instance.SetActionFighting(target, allies[random.Next(allies.Count)].EntityId);
                });
        }

        /// <summary>
        /// Reanimation: "Reanimates a single biological enemy corpse to assist the user in combat for a set time. Corpse is
        /// reanimated at a lower level than the user." The corpse stands up - announced with the client's own Revived and a
        /// friendly target category - at the user's level plus CREATURE_LEVEL_DIFFERENCE, AFS for DURATION so the AI sets
        /// it on the user's enemies, and falls again when the time runs out.
        /// </summary>
        public static bool Reanimate(MapChannel map, Manifestation exobiologist, Creature corpse, ActionLevelInfo info)
        {
            if (corpse == null || corpse.State != CharacterState.Dead || !ReferenceEquals(MapChannelManager.ChannelOf(corpse), map) ||
                !CreatureManager.CreatureFlagsOf(corpse).Contains((int)CreatureFlag.Biological))
                return false;
            var (faction, level) = (corpse.Faction, corpse.Level);
            corpse.State = CharacterState.Normal;
            corpse.Faction = Factions.AFS;
            corpse.Level = (uint)Math.Max(1, exobiologist.Level + info.Get(AbilityProperty.CreatureLevelDifference));
            corpse.Controller.DeadTime = 0;
            var health = corpse.Attributes[Attributes.Health];
            health.Current = health.CurrentMax;
            CellManager.Instance.CellCallMethod(map, corpse, new Packets.ClientMethod.Server.RevivedPacket(exobiologist.EntityId));
            CellManager.Instance.CellCallMethod(map, corpse, new Packets.MapChannel.Server.UpdateHealthPacket(health, corpse.EntityId));
            CellManager.Instance.CellCallMethod(map, corpse, new Packets.MapChannel.Server.TargetCategoryPacket(TargetCategory.Friendly));
            BehaviorManager.Instance.DropFight(corpse);
            // No client effect is named for the reanimated time, so it is a server-side timer.
            GameEffectManager.Instance.AttachServerTimer(map, corpse, info.Get(AbilityProperty.Duration) * 1000, _ =>
                {
                    corpse.Faction = faction;
                    corpse.Level = level;
                    if (corpse.State == CharacterState.Dead)
                        return;
                    health.Current = 0;
                    corpse.State = CharacterState.Dead;
                    CellManager.Instance.CellCallMethod(map, corpse, new Packets.MapChannel.Server.UpdateHealthPacket(health, corpse.EntityId));
                    CellManager.Instance.CellCallMethod(map, corpse, new Packets.MapChannel.Server.ActorKilledPacket());
                });
            return true;
        }

        /// <summary>Reanimation Wave: "Reanimates all nearby biological enemy corpses" within RADIUS_AROUND_SOURCE.</summary>
        public static List<ulong> ReanimationWave(MapChannel map, Manifestation exobiologist, ActionLevelInfo info)
        {
            var raised = new List<ulong>();
            var radius = info.Get(AbilityProperty.RadiusAroundSource);
            if (exobiologist.Cells == null)
                return raised;
            foreach (var seed in exobiologist.Cells)
                if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                    foreach (var corpse in cell.CreatureList.ToList())
                        if (corpse.State == CharacterState.Dead && corpse.Faction != Factions.AFS &&
                            Vector3.Distance(corpse.Position, exobiologist.Position) <= radius && Reanimate(map, exobiologist, corpse, info))
                            raised.Add(corpse.EntityId);
            return raised;
        }

        /// <summary>
        /// Paint Target on one enemy for DURATION, whose effect reads "Reduced Cover: %(coverMod)s%% | Armor Recharge:
        /// %(armorMod)s%% | Armor Piercing: +%(pierceMod)s%%". EFFECT_ARMOR_PIERCE_PERCENT of every hit on it goes past its
        /// armour. The cover and armour-recharge values are shown but have nothing to act on here: this server has no
        /// cover, and creatures do not recharge armour (only players regenerate).
        /// </summary>
        public static void PaintTarget(MapChannel map, Manifestation sniper, Creature target, ActionLevelInfo info)
        {
            if (target == null)
                return;
            GameEffectManager.Instance.AttachEffect(map, target, PaintTargetType, info.Level, info.Get(AbilityProperty.Duration) * 1000, sniper.EntityId, false,
                new Dictionary<string, double>
                {
                    ["coverMod"] = info.Get(AbilityProperty.EffectCoverModifier), ["armorMod"] = info.Get(AbilityProperty.EffectArmorRegenModifier),
                    ["pierceMod"] = info.Get(AbilityProperty.EffectArmorPiercePercent)
                }).ArmorPiercePercent = info.Get(AbilityProperty.EffectArmorPiercePercent);
        }

        // ---- OD-56 (owner decision 2026-09-19, "build them labelled"): stand-ins where the original data is lost ----

        /// <summary>
        /// World-seed creatures standing in for the summons, whose CREATURE_VARIANT_IDs index a creature-variant table no
        /// recovered source holds: the AFS mini turret (9) for Turret and Trap, the friendly Hominis Machina (8) for the
        /// engineer's bot, the military-surplus soldier (10) for the Spotter, and the human NPC of the player's gender
        /// (4 male, 6 female) wearing their appearance for the clone. Every one is an analogue.
        /// </summary>
        public const uint TurretStandIn = 9, BotStandIn = 8, SpotterStandIn = 10, CloneMaleStandIn = 4, CloneFemaleStandIn = 6;

        private static readonly Dictionary<(ulong Owner, string Kind), Creature> Summons = new Dictionary<(ulong, string), Creature>();

        /// <summary>For tests: makes the summoned creature instead of CreatureManager.SpawnSummon (which reads the database).</summary>
        public static Func<MapChannel, uint, Vector3, uint, Creature> SpawnOverride { get; set; }

        /// <summary>
        /// Spawns a stand-in beside the player - "Only 1 ... can be active at a time", so a new one of the same kind
        /// replaces the old - and removes it when its time is up.
        /// </summary>
        /// <param name="commandable">
        /// Whether the client may command it. The game calls these subordinates and its own Command System help
        /// names the three that can be ordered about - "Create Clone, Spotter, and Bot Construction" - so those
        /// three are adopted as minions (InfiniteRasa 492954a) and follow their owner; a turret, a trap and a crab
        /// mine stay where they are put. The lifetime stays with the timer below, so Adopt is given none.
        /// </param>
        public static Creature Summon(MapChannel map, Manifestation owner, string kind, uint template, Vector3 at, int lifetimeMs, int levelDifference,
            bool commandable = false)
        {
            if (Summons.TryGetValue((owner.EntityId, kind), out var previous))
                Unsummon(map, previous);
            var level = (uint)Math.Max(1, owner.Level + levelDifference);
            var summon = SpawnOverride != null ? SpawnOverride(map, template, at, level)
                : CreatureManager.Instance.SpawnSummon(map, template, at, owner.Rotation, level);
            if (summon == null)
                return null;
            Summons[(owner.EntityId, kind)] = summon;

            if (commandable)
            {
                var master = map.ClientList.FirstOrDefault(client => client?.Player == owner);
                if (master != null)
                    MinionManager.Instance.Adopt(master, summon);
            }

            GameEffectManager.Instance.AttachServerTimer(map, summon, lifetimeMs, _ =>
            {
                Summons.Remove((owner.EntityId, kind));
                Unsummon(map, summon);
            });
            return summon;
        }

        private static void Unsummon(MapChannel map, Creature summon)
        {
            map.CreaturesWithEffects.Remove(summon);
            foreach (var cell in map.MapCellInfo.Cells.Values)
                cell.CreatureList.Remove(summon);
            CellManager.Instance.RemoveCreatureFromWorld(map, summon);
        }

        private static Vector3 Beside(Manifestation player, float metres = 2f)
            => player.Position + new Vector3((float)Math.Sin(player.Rotation), 0, (float)Math.Cos(player.Rotation)) * metres;

        /// <summary>Turret: "Creates a turret at a location to assist the user in combat for a set time" - the stand-in turret, its shots set to the row's DAMAGE_AMOUNT (level-scaled), for DURATION.</summary>
        public static Creature Turret(MapChannel map, Manifestation engineer, Vector3? at, ActionLevelInfo info)
        {
            var turret = Summon(map, engineer, "turret", TurretStandIn, at ?? Beside(engineer), info.Get(AbilityProperty.Duration) * 1000, 0);
            if (turret == null)
                return null;
            var scale = info.Has(AbilityProperty.AttrScaleType) ? info.Get(AbilityProperty.AttrScaleType) : (int?)null;
            foreach (var action in turret.Actions)
            {
                action.MinDamage = (uint)AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMin), engineer.Level, scale);
                action.MaxDamage = (uint)AbilityScaling.ScaleActorAmount(info.Get(AbilityProperty.DamageAmountMax, info.Get(AbilityProperty.DamageAmountMin)), engineer.Level, scale);
            }
            return turret;
        }

        /// <summary>
        /// Trap: "a special turret ... It will fire at enemies and create a high amount of hate, drawing the enemy to attack
        /// it. When it is destroyed, it damages the enemy and then disappears." The stand-in turret deals DAMAGE_MODIFIER_PERCENT
        /// less; every second the hostiles within EFFECT_RADIUS are set on it; when it dies or its DURATION ends it bursts,
        /// DAMAGE_AMOUNT of the row's type to every hostile within EFFECT_RADIUS.
        /// </summary>
        public static Creature Trap(MapChannel map, Manifestation engineer, Vector3? at, ActionLevelInfo info, Random random)
        {
            var duration = info.Get(AbilityProperty.Duration) * 1000;
            var trap = Summon(map, engineer, "trap", TurretStandIn, at ?? Beside(engineer), duration + 1000, 0);
            if (trap == null)
                return null;
            foreach (var action in trap.Actions)
            {
                action.MinDamage = (uint)(action.MinDamage * (100 + info.Get(AbilityProperty.DamageModifierPercent)) / 100);
                action.MaxDamage = (uint)(action.MaxDamage * (100 + info.Get(AbilityProperty.DamageModifierPercent)) / 100);
            }
            var (min, max) = DamageRange(engineer, info);
            var radius = info.Get(AbilityProperty.EffectRadius);
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var burst = false;
            GameEffectManager.Instance.AttachEffect(map, engineer, TrapExplosionType, info.Level, duration, engineer.EntityId, true,
                new Dictionary<string, double>(), DefaultPulseMs, pulse =>
                {
                    if (burst)
                        return;
                    if (trap.State == CharacterState.Dead || pulse.EffectTime + DefaultPulseMs >= pulse.Duration)
                    {
                        burst = true;
                        Blast(map, engineer, pulse, engineer, trap.Position, radius, min, max, type, random);
                        return;
                    }
                    foreach (var enemy in HostilesAround(map, engineer, trap.Position, radius))
                        if (enemy.Controller.ActionFighting.TargetEntityId != trap.EntityId)
                            BehaviorManager.Instance.SetActionFighting(enemy, trap.EntityId);
                });
            return trap;
        }

        /// <summary>Bot Construction: "Creates a bot to assist the user in combat for a set time" - the stand-in bot for CREATURE_LIFETIME_MS at the user's level plus CREATURE_LEVEL_DIFFERENCE.</summary>
        public static Creature BotConstruction(MapChannel map, Manifestation engineer, ActionLevelInfo info)
            => Summon(map, engineer, "bot", BotStandIn, Beside(engineer), info.Get(AbilityProperty.CreatureLifetimeMs), info.Get(AbilityProperty.CreatureLevelDifference), commandable: true);

        /// <summary>Spotter: "Summons a helper to assist the user in combat for a set time" - the stand-in soldier for CREATURE_LIFETIME_MS.</summary>
        public static Creature Spotter(MapChannel map, Manifestation ranger, ActionLevelInfo info)
            => Summon(map, ranger, "spotter", SpotterStandIn, Beside(ranger), info.Get(AbilityProperty.CreatureLifetimeMs), info.Get(AbilityProperty.CreatureLevelDifference), commandable: true);

        /// <summary>
        /// Create Clone: "Creates a clone of the user to assist in combat for a set time" - a human NPC of the user's gender
        /// wearing their appearance, for CREATURE_LIFETIME_MS at the user's level plus CREATURE_LEVEL_DIFFERENCE. Its attacks are
        /// the stand-in's, not the user's.
        /// </summary>
        public static Creature CreateClone(MapChannel map, Manifestation exobiologist, ActionLevelInfo info)
        {
            var clone = Summon(map, exobiologist, "clone", exobiologist.Gender == 0 ? CloneMaleStandIn : CloneFemaleStandIn, Beside(exobiologist),
                info.Get(AbilityProperty.CreatureLifetimeMs), info.Get(AbilityProperty.CreatureLevelDifference), commandable: true);
            if (clone != null && exobiologist.AppearanceData != null)
            {
                clone.AppearanceData = new Dictionary<EquipmentData, AppearanceData>(exobiologist.AppearanceData);
                CreatureManager.Instance.UpdateCreatureAppearance(clone);
            }
            return clone;
        }

        /// <summary>
        /// Crab Mines: "Creates a mobile mine that will seek out a nearby single enemy target and explode. The explosion will
        /// damage all enemies within the blast radius. The user may have up to 3 Crab Mines active at once." No mine creature
        /// survives, so the mine is not a creature: it picks the nearest hostile within 30 m, reaches it after two seconds and
        /// bursts there, DAMAGE_AMOUNT of the row's type within EFFECT_RADIUS (the seek range and travel time are stand-ins).
        /// </summary>
        public static bool CrabMine(MapChannel map, Manifestation sapper, ActionLevelInfo info, Random random)
        {
            if (sapper.ActiveEffects.Values.Count(e => e.TypeId == CrabMineExplosionType) >= 3)
                return false;
            var prey = HostilesAround(map, sapper, sapper.Position, 30).OrderBy(c => Vector3.Distance(c.Position, sapper.Position)).FirstOrDefault();
            if (prey == null)
                return false;
            var (min, max) = DamageRange(sapper, info);
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            GameEffectManager.Instance.AttachEffect(map, sapper, CrabMineExplosionType, info.Level, 2000, sapper.EntityId, true,
                new Dictionary<string, double>(), 2000, mine =>
                    Blast(map, sapper, mine, sapper, prey.Position, info.Get(AbilityProperty.EffectRadius), min, max, type, random));
            return true;
        }

        /// <summary>
        /// Hortimunculus: "Creates a plant lifeform from a single enemy corpse that will heal the user and nearby squad members
        /// at a set interval for as long as the plant is alive. Plant will increase player resistances ... Plant will decay over
        /// time." The plant stands where the corpse lay, starting at HEALTH_PERCENTAGE and losing DECAY_PERCENTAGE every
        /// INTERVAL; while it lives, every INTERVAL the squad within EFFECT_RADIUS carries HORTIMONCULUS_BUFF with
        /// RESIST_PERCENTAGE added to every resistance and heals 5% of their health (the heal amount is a stand-in: the row
        /// gives none). The plant is not a destroyable creature.
        /// </summary>
        public static bool Hortimunculus(MapChannel map, Game.Client exobiologist, Creature corpse, ActionLevelInfo info)
        {
            if (corpse == null || corpse.State != CharacterState.Dead ||
                !CreatureManager.CreatureFlagsOf(corpse).Contains((int)CreatureFlag.Biological))
                return false;
            var at = corpse.Position;
            var interval = Math.Max(1, info.Get(AbilityProperty.Interval, 5)) * 1000;
            var decay = Math.Max(1, info.Get(AbilityProperty.DecayPercentage, 20));
            var life = info.Get(AbilityProperty.HealthPercentage, 100) * interval / decay;
            var resist = info.Get(AbilityProperty.ResistPercentage);
            GameEffectManager.Instance.AttachEffect(map, exobiologist.Player, HortimunculusBuffType, info.Level, life, exobiologist.Player.EntityId, true,
                new Dictionary<string, double>(), interval, pulse =>
                {
                    foreach (var member in SquadAround(map, exobiologist, at, info.Get(AbilityProperty.EffectRadius)))
                    {
                        var health = member.Player.Attributes[Attributes.Health];
                        Heal(map, member.Player, Math.Max(1, health.CurrentMax * 5 / 100));
                        if (!member.Player.ActiveEffects.Values.Any(e => e.TypeId == HortimunculusBuffType && e != pulse))
                            GameEffectManager.Instance.AttachEffect(map, member.Player, HortimunculusBuffType, info.Level, interval, exobiologist.Player.EntityId, true,
                                new Dictionary<string, double>()).ResistRating = resist;
                    }
                });
            return true;
        }

        /// <summary>
        /// Base Wave: "Buffs the user and nearby squad members with increased damage resistance and armor regeneration for a
        /// set time" - RESIST_MODIFIER on every resistance and EFFECT_ARMOR_REGEN_MODIFIER percent of the normal armour
        /// recharge (500: five times) for DURATION to each within RADIUS_AROUND_SOURCE.
        /// </summary>
        public static void BaseWave(MapChannel map, Game.Client engineer, ActionLevelInfo info)
        {
            foreach (var member in SquadAround(map, engineer, engineer.Player.Position, info.Get(AbilityProperty.RadiusAroundSource)))
            {
                var wave = GameEffectManager.Instance.AttachEffect(map, member.Player, BaseWaveType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                    engineer.Player.EntityId, true, new Dictionary<string, double> { ["resistMod"] = info.Get(AbilityProperty.ResistModifier) });
                wave.ResistRating = info.Get(AbilityProperty.ResistModifier);
                if (info.Has(AbilityProperty.EffectArmorRegenModifier))
                    wave.ArmorRegenPercent = info.Get(AbilityProperty.EffectArmorRegenModifier);
                void Refresh()
                {
                    ManifestationManager.Instance.UpdateStatsValues(member, false);
                    member.CallMethod(member.Player.EntityId, new Packets.MapChannel.Server.AttributeInfoPacket(member.Player.Attributes));
                }
                wave.OnDetach = _ => Refresh();
                Refresh();
            }
        }

        /// <summary>Crit Wave: "improved chances for critical hits for a set time" - EFFECT_MODIFIER percentage points for each within RADIUS_AROUND_SOURCE, for DURATION.</summary>
        public static void CritWave(MapChannel map, Game.Client sniper, ActionLevelInfo info)
        {
            foreach (var member in SquadAround(map, sniper, sniper.Player.Position, info.Get(AbilityProperty.RadiusAroundSource)))
                GameEffectManager.Instance.AttachEffect(map, member.Player, CritWaveType, info.Level, info.Get(AbilityProperty.Duration) * 1000,
                    sniper.Player.EntityId, true, new Dictionary<string, double> { ["critAmt"] = info.Get(AbilityProperty.EffectModifier) })
                    .CritBonusPercent = info.Get(AbilityProperty.EffectModifier);
        }

        /// <summary>
        /// Polymorph: "Transforms the user into a specific enemy for a set time. The user will obtain all combat actions of an
        /// equal level enemy, including their faction." The creature form (CREATURE_VARIANT_ID) is lost; the stand-in keeps the
        /// user's own body and actions and only lends the faction - creatures do not pick them as a target - for DURATION.
        /// Using it again ends it.
        /// </summary>
        public static void Polymorph(MapChannel map, Manifestation spy, ActionLevelInfo info)
            => GameEffectManager.Instance.AttachEffect(map, spy, PolymorphType, info.Level, info.Get(AbilityProperty.Duration) * 1000, spy.EntityId, true,
                new Dictionary<string, double>()).Disguised = true;

        /// <summary>
        /// Cadaver Immolation: "Explodes a single enemy corpse after a set time and damages all enemies within the blast
        /// radius" - after DELAY seconds, DAMAGE_AMOUNT of the row's type to every hostile within EFFECT_RADIUS of the corpse.
        /// </summary>
        public static void CadaverImmolation(MapChannel map, Manifestation exobiologist, Creature corpse, ActionLevelInfo info, Random random)
        {
            if (corpse == null)
                return;
            var (min, max) = DamageRange(exobiologist, info);
            var type = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var delay = Math.Max(1, info.Get(AbilityProperty.Delay)) * 1000;
            var at = corpse.Position;
            GameEffectManager.Instance.AttachEffect(map, exobiologist, CorpseImmolationType, info.Level, delay, exobiologist.EntityId, true,
                new Dictionary<string, double>(), delay, burn => Blast(map, exobiologist, burn, exobiologist, at, info.Get(AbilityProperty.EffectRadius), min, max, type, random));
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
