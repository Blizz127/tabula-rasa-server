using System;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Timer;
    using Structures;

    public class ActorActionManager
    {
        private static ActorActionManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Timer Timer = new Timer();
        private readonly Random _damageRandom = new Random();
        private readonly Func<long> _getMonotonicMilliseconds;
        public static ActorActionManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new ActorActionManager();
                    }
                }

                return _instance;
            }
        }

        private ActorActionManager() : this(() => Environment.TickCount64)
        {
        }

        public ActorActionManager(Func<long> getMonotonicMilliseconds)
        {
            _getMonotonicMilliseconds = getMonotonicMilliseconds ?? throw new ArgumentNullException(nameof(getMonotonicMilliseconds));
        }

        public bool CanBeginAbility(Manifestation player)
        {
            var current = player.CurrentAbility;
            if (current != null && current.Resolved && _getMonotonicMilliseconds() >= current.RecoveryEndsAt)
                player.CurrentAbility = null;
            return player.CurrentAbility == null && player.CurrentWeaponAttack == null && player.CurrentAction == 0 &&
                (player.CurrentWeaponAction == null || player.CurrentWeaponAction.ActionId == ActionId.WeaponReload);
        }

        public long GetAbilityReuseRemaining(Manifestation player, ActionId actionId)
            => player.AbilityReuseDeadlines.TryGetValue(actionId, out var deadline)
                ? Math.Max(0, deadline - _getMonotonicMilliseconds()) : 0;

        public bool TryStartLightning(Client client, RequestPerformAbilityPacket packet)
        {
            var player = client.Player;
            var map = player?.MapChannel;
            if (client.State != ClientState.Ingame || player == null || map == null || player.RemoveFromMap ||
                packet.ActionId != ActionId.AaRecruitLightning || packet.TargetLocation.HasValue ||
                !AbilityRequirements.CanUseSkillAbility(player, packet.ActionId, packet.ActionArgId) ||
                !CanBeginAbility(player))
                return false;

            var now = _getMonotonicMilliseconds();
            if (player.AbilityReuseDeadlines.TryGetValue(packet.ActionId, out var reuseUntil) && now < reuseUntil)
                return false;

            var target = GetLightningTarget(map, player, packet.Target ?? 0);
            var rank = (uint)packet.ActionArgId;
            if (target == null || !player.Attributes.TryGetValue(Attributes.Power, out var power) ||
                power.Current < LightningAbilityData.GetPowerCost(rank))
                return false;

            WeaponActionManager.Instance.InterruptForAbility(client);
            var action = new ActionData(player, packet.ActionId, rank, target.EntityId,
                LightningAbilityData.WindupMilliseconds)
            {
                TargetLocation = packet.TargetLocation, ItemId = packet.ItemId, ClientYaw = packet.ClientYaw
            };
            var windupEndsAt = now + LightningAbilityData.WindupMilliseconds;
            var recoveryEndsAt = windupEndsAt + LightningAbilityData.RecoveryMilliseconds;
            player.CurrentAbility = new AbilityExecution(action, map, target, windupEndsAt,
                recoveryEndsAt, recoveryEndsAt + LightningAbilityData.ReuseMilliseconds);
            CellManager.Instance.CellCallMethod(map, player,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, rank, target.EntityId));
            return true;
        }

        private static Creature GetLightningTarget(MapChannel map, Manifestation player, ulong targetId)
        {
            var entities = EntityManager.Instance;
            if (map.MapInfo == null || player.MapContextId != map.MapInfo.MapContextId ||
                !entities.RegisteredEntities.TryGetValue(targetId, out var type) || type != EntityType.Creature ||
                !entities.Creatures.TryGetValue(targetId, out var target) ||
                target.MapContextId != map.MapInfo.MapContextId || target.State == CharacterState.Dead ||
                target.Faction == Factions.AFS)
                return null;
            // Current TargetCategory packets advertise AFS as friendly and other
            // creatures as hostile. Wargames, damageable objects, body-distance
            // range and native line-of-sight remain separate reconstruction work.
            return target;
        }

        public bool InterruptAbility(Client client, ActionId actionId, uint rank)
        {
            var execution = client.Player?.CurrentAbility;
            if (execution == null || execution.Action.ActionId != actionId || execution.Action.ActionArgId != rank)
                return false;
            EndAbility(client, execution, true);
            return true;
        }

        private void EndAbility(Client client, AbilityExecution execution, bool interrupted)
        {
            var player = client.Player;
            player.CurrentAbility = null;
            var action = execution.Action;
            if (ReferenceEquals(player.MapChannel, execution.Map))
            {
                if (interrupted)
                    CellManager.Instance.CellCallMethod(execution.Map, player,
                        new ActionInterruptPacket(player.EntityId, action.ActionId, action.ActionArgId));
                else
                    CellManager.Instance.CellCallMethod(execution.Map, player,
                        new ActionFailedPacket(action.ActionId, action.ActionArgId));
            }
            // Cancellation of the current animation does not remove the original
            // client's unresolved request. A successful recovery already removed it.
            if (!execution.Resolved)
            {
                client.CallMethod(player.EntityId, new UserActionFailedPacket(action.ActionId, (int)action.ActionArgId));
                client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                {
                    (action.ActionId, GetAbilityReuseRemaining(player, action.ActionId))
                }));
            }
        }

        private void UpdateAbility(MapChannel map, Client client, long now)
        {
            var player = client.Player;
            var execution = player?.CurrentAbility;
            if (execution == null)
                return;
            if (!ReferenceEquals(player.MapChannel, map) || !ReferenceEquals(execution.Map, map) ||
                player.RemoveFromMap || player.State == CharacterState.Dead)
            {
                EndAbility(client, execution, false);
                return;
            }

            if (!execution.Resolved && now >= execution.WindupEndsAt)
            {
                var action = execution.Action;
                if (!AbilityRequirements.CanUseSkillAbility(player, action.ActionId, (int)action.ActionArgId) ||
                    !ReferenceEquals(GetLightningTarget(map, player, action.TargetId), execution.OriginalTarget) ||
                    !player.Attributes.TryGetValue(Attributes.Power, out var power) ||
                    power.Current < LightningAbilityData.GetPowerCost(action.ActionArgId))
                {
                    EndAbility(client, execution, false);
                    return;
                }

                // The data establishes the cost. Spending at successful resolution
                // is an explicit ordering inference, documented with the lifecycle.
                power.Current -= LightningAbilityData.GetPowerCost(action.ActionArgId);
                execution.Resolved = true;
                player.AbilityReuseDeadlines[action.ActionId] = execution.ReuseEndsAt;
                CellManager.Instance.CellCallMethod(map, player, new UpdatePowerPacket(power, player.EntityId));
                PerformRecovery(map, action);
                client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                {
                    (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                }));
            }
            if (execution.Resolved && now >= execution.RecoveryEndsAt)
                player.CurrentAbility = null;
        }

        public bool HasActiveAction(Actor actor)
        {
            return actor.CurrentAction != 0 || actor is Manifestation { CurrentAbility: not null } ||
                actor is Manifestation { CurrentWeaponAction: not null } ||
                actor is Manifestation { CurrentWeaponAttack: not null };
        }

        public void DoWork(MapChannel mapChannel, long delta)
        {
            var now = _getMonotonicMilliseconds();
            if (mapChannel.ClientList != null)
                foreach (var client in mapChannel.ClientList)
                    if (client?.Player != null)
                    {
                        WeaponAttackManager.Instance.Update(client, now);
                        WeaponActionManager.Instance.Update(client, now);
                        UpdateAbility(mapChannel, client, now);
                    }

            if (mapChannel.PerformRecovery.Count > 0)
            {
                if (mapChannel.PerformRecovery.Count > 1)
                    Logger.WriteLog(LogType.Debug, $"PerformRecovery count = { mapChannel.PerformRecovery.Count}");

                // iterate backwards through list
                for (var i = mapChannel.PerformRecovery.Count - 1; i >= 0; i--)
                {
                    var action = mapChannel.PerformRecovery[i];

                    if (action.IsInrerrupted)
                    {
                        mapChannel.PerformRecovery.RemoveAt(i);
                        DynamicObjectManager.CancelPendingUse(mapChannel, action);
                        CellManager.Instance.CellCallMethod(mapChannel, action.Actor,
                            new ActionInterruptPacket(action.Actor.EntityId, action.ActionId, action.ActionArgId));
                        if (mapChannel.ClientList != null)
                            foreach (var client in mapChannel.ClientList)
                                if (client?.Player == action.Actor)
                                    client.CallMethod(action.Actor.EntityId,
                                        new UserActionFailedPacket(action.ActionId, (int)action.ActionArgId));
                        continue;
                    }

                    // skip if client is busy
                    if (HasActiveAction(action.Actor))
                        continue;

                    action.PassedTime += delta;

                    if (action.WaitTime <= action.PassedTime)
                    {
                        // perform action
                        PerformRecovery(mapChannel, action);
                        // remove action
                        mapChannel.PerformRecovery.Remove(action);
                    }
                }
            }
        }

        public void PerformRecovery(MapChannel mapChannel, ActionData action)
        {
            switch (action.ActionId)
            {
                case ActionId.AaRecruitLightning:
                    if (action.Actor is Manifestation player && player.Level >= 1 &&
                        action.ActionArgId >= 1 && action.ActionArgId <= 5)
                    {
                        var range = LightningAbilityData.GetBaseDamageRange(action.ActionArgId, player.Level);
                        MissileManager.Instance.MissileLaunch(mapChannel, action,
                            _damageRandom.Next(range.Minimum, range.Maximum + 1));
                    }
                    break;
                case ActionId.AaRecruitSprint:
                    if (!GameEffectManager.Instance.TryAttachSprint(mapChannel, action.Actor, action.ActionArgId))
                        CellManager.Instance.CellCallMethod(mapChannel, action.Actor,
                            new UserActionFailedPacket(action.ActionId, (int)action.ActionArgId));
                    else
                        CellManager.Instance.CellCallMethod(mapChannel, action.Actor,
                            new SprintRecoveryPacket(action.ActionArgId, action.Actor.EntityId));
                    break;
                case ActionId.UseObject:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    // Content usables dispatch by the server-side placement kind, before
                    // the legacy argId branches: a content container's use never reaches them.
                    if (MissionManager.Instance.Content.IsContentUsableSource(mapChannel, action.SourceId))
                    {
                        MissionManager.Instance.Content.ContentUsableRecovery(mapChannel, action);
                        break;
                    }
                    switch (action.ActionArgId)
                    {
                        case 1:
                            DynamicObjectManager.Instance.FootlockerRecovery(mapChannel, action);
                            break;
                        case 6:
                            DynamicObjectManager.Instance.LogosRecovery(mapChannel, action);
                            break;
                        case 7:
                            DynamicObjectManager.Instance.CaptureControlPointRecovery(mapChannel, action);
                            break;
                        default:
                            Logger.WriteLog(LogType.Debug, $"PerformRecovery.UseObject: unsuported actionArgId {action.ActionArgId}");
                            break;
                    }
                    break;
                case ActionId.WeaponAttack:
                    Logger.WriteLog(LogType.Debug, $"PerformRecovery {action.ActionArgId} {action.ActionId} {action.Args}");
                    /*
                    PlayerManager.Instance.StartAutoFire(action.Client, 0D);
                    action.Client.CellCallMethod(action.Client, action.Client.MapClient.Player.Actor.EntityId, new PerformRecoveryPacket(action.ActionId, action.ActionArgId, new List<int> { 1 }));
                    */
                    break;
                case ActionId.WeaponDraw:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    action.Actor.WeaponReady = true;
                    break;
                case ActionId.WeaponStow:
                    CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformRecoveryPacket(PerformType.TwoArgs, action.ActionId, action.ActionArgId));
                    action.Actor.WeaponReady = false;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"PerformAction: unsuported {action.ActionId}");
                    break;
            };
        }
    }
}
