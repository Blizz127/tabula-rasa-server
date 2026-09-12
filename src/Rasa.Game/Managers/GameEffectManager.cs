using System;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;

    public class GameEffectManager
    {
        public const int SprintEffectType = 247;
        private static readonly int[] SprintChiCost = { 0, 30, 27, 25, 20, 18 };
        private static GameEffectManager _instance;
        private static readonly object InstanceLock = new object();
        public static GameEffectManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new GameEffectManager();
                    }
                }

                return _instance;
            }
        }

        private GameEffectManager()
        {
        }

        public void AddToList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Add(gameEffect.EffectId, gameEffect);
        }

        public bool CanAttachSprint(Actor actor, uint effectLevel)
        {
            return actor != null && actor.State != CharacterState.Dead &&
                effectLevel >= 1 && effectLevel <= 5 &&
                !actor.ActiveEffects.Values.Any(effect => effect.TypeId == SprintEffectType) &&
                actor.Attributes.TryGetValue(Attributes.Chi, out var chi) &&
                chi.Current >= SprintChiCost[effectLevel];
        }

        public bool TryAttachSprint(MapChannel mapChannel, Actor actor, uint effectLevel)
        {
            if (mapChannel == null || !CanAttachSprint(actor, effectLevel))
                return false;

            // Original actiondata: 401/ranks 1–5. See docs/sprint-client-evidence.md.
            var cost = SprintChiCost[effectLevel];
            var gameEffect = new GameEffect
            {
                Duration = (effectLevel == 5 ? 5000 : 3600) * 1000,
                TypeId = SprintEffectType,
                EffectId = ++mapChannel.CurrentEffectId,
                EffectLevel = effectLevel,
                DrainInterval = 2000,
                NextDrainTime = 2000,
                AdrenalineDrain = cost,
                MovementBeforeAttach = actor.MovementSpeed,
                MovementMultiplier = (110 + effectLevel * 10) / 100.0
            };

            actor.Attributes[Attributes.Chi].Current -= cost;
            AddToList(actor, gameEffect);
            AnnounceChi(mapChannel, actor);
            CellManager.Instance.CellCallMethod(mapChannel, actor, new GameEffectAttachedPacket
            {
                EffectTypeId = gameEffect.TypeId,
                EffectId = gameEffect.EffectId,
                EffectLevel = gameEffect.EffectLevel,
                SourceId = actor.EntityId,
                Announced = true,
                IsActive = true,
                IsBuff = true,
                // Authored description is open-ended. Do not display the internal cap.
                // EFFECT_FAST_MAXBEAD_MODIFIER=100 gives a neutral accuracy multiplier.
                EffectArguments = new[] { 1.0 }
            });
            UpdateMovementMod(mapChannel, actor);
            return true;
        }

        public bool TryDetachRequestedEffect(MapChannel mapChannel, Actor actor, int effectId)
        {
            if (mapChannel == null || actor == null ||
                !actor.ActiveEffects.TryGetValue(effectId, out var effect) ||
                effect.TypeId != SprintEffectType)
                return false;
            DettachEffect(mapChannel, actor, effect);
            return true;
        }

        public void DettachEffect(MapChannel mapChannel, Actor actor, GameEffect gameEffect)
        {
            if (!actor.ActiveEffects.Remove(gameEffect.EffectId))
                return;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new GameEffectDetachedPacket { EffectId = gameEffect.EffectId });
            if (gameEffect.TypeId == SprintEffectType)
            {
                actor.MovementSpeed = gameEffect.MovementBeforeAttach;
                UpdateMovementMod(mapChannel, actor);
            }
        }

        public void DoWork(MapChannel mapChannel, long passedTime)
        {
            if (passedTime <= 0 || mapChannel.ClientList == null)
                return;
            foreach (var client in mapChannel.ClientList)
            {
                if (client?.Player == null)
                    continue;

                var actor = client.Player;
                // Detach every due effect safely, including multiple expirations in one update.
                foreach (var effect in actor.ActiveEffects.Values.ToArray())
                {
                    effect.EffectTime = passedTime > long.MaxValue - effect.EffectTime
                        ? long.MaxValue : effect.EffectTime + passedTime;
                    if (effect.TypeId == SprintEffectType)
                    {
                        var chiChanged = false;
                        while (effect.NextDrainTime <= effect.EffectTime &&
                            effect.NextDrainTime < effect.Duration)
                        {
                            if (!actor.Attributes.TryGetValue(Attributes.Chi, out var chi) ||
                                chi.Current < effect.AdrenalineDrain)
                            {
                                DettachEffect(mapChannel, actor, effect);
                                break;
                            }
                            chi.Current -= effect.AdrenalineDrain;
                            chiChanged = true;
                            effect.NextDrainTime += effect.DrainInterval;
                        }
                        if (chiChanged)
                            AnnounceChi(mapChannel, actor);
                    }
                    if (effect.Duration > 0 && effect.EffectTime >= effect.Duration)
                        DettachEffect(mapChannel, actor, effect);
                }
            }
        }
        

        public void RemoveFromList(Actor actor, GameEffect gameEffect)
        {
            actor.ActiveEffects.Remove(gameEffect.EffectId);
        }

        public void UpdateMovementMod(MapChannel mapChannel, Actor actor)
        {
            var sprint = actor.ActiveEffects.Values.FirstOrDefault(effect => effect.TypeId == SprintEffectType);
            if (sprint != null)
                actor.MovementSpeed = sprint.MovementBeforeAttach * sprint.MovementMultiplier;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new MovementModChangePacket(actor.MovementSpeed));
        }

        private static void AnnounceChi(MapChannel mapChannel, Actor actor)
        {
            CellManager.Instance.CellCallMethod(mapChannel, actor,
                new UpdateChiPacket(actor.Attributes[Attributes.Chi], actor.EntityId));
        }
    }
}
