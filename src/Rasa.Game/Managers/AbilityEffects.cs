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
