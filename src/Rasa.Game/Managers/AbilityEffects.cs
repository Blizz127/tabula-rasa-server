using System;
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
