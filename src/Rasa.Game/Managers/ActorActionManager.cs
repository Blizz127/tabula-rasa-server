using System;
using System.Collections.Generic;
using System.Linq;

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
            // The content-usable branch beside the creature check: a destroyable
            // placement is a valid Lightning target even though it is not a Creature.
            var contentTarget = target == null ? WeaponAttackManager.GetEligibleContentTarget(player, packet.Target ?? 0) : null;
            var rank = (uint)packet.ActionArgId;
            if (target == null && contentTarget == null || !player.Attributes.TryGetValue(Attributes.Power, out var power) ||
                power.Current < LightningAbilityData.GetPowerCost(rank))
                return false;

            WeaponActionManager.Instance.InterruptForAbility(client);
            var targetEntityId = target?.EntityId ?? contentTarget.EntityId;
            var action = new ActionData(player, packet.ActionId, rank, targetEntityId,
                LightningAbilityData.WindupMilliseconds)
            {
                TargetLocation = packet.TargetLocation, ItemId = packet.ItemId, ClientYaw = packet.ClientYaw
            };
            var windupEndsAt = now + LightningAbilityData.WindupMilliseconds;
            var recoveryEndsAt = windupEndsAt + LightningAbilityData.RecoveryMilliseconds;
            player.CurrentAbility = new AbilityExecution(action, map, target, contentTarget, windupEndsAt,
                recoveryEndsAt, recoveryEndsAt + LightningAbilityData.ReuseMilliseconds);
            CellManager.Instance.CellCallMethod(map, player,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, rank, targetEntityId));
            return true;
        }

        /// <summary>Metres past an ability's range a target may be: the client checks range before it asks.</summary>
        private const float AbilityRangeSlack = 2.5f;

        /// <summary>
        /// A damage ability from the client's action tables (ActionTableManager.TryGetDirectDamage): Force Blast,
        /// Shrapnel, Tectonic Strike, Vortex, Rushing Blow, the Explosive and Concussive Waves. The lifecycle is
        /// Lightning's - windup, then cost, reuse and resolution, then recovery - with every number taken from the
        /// ability's own row instead of LightningAbilityData. An ability aimed at a target needs a hostile creature or
        /// a destroyable placement within its range; one centred on the performer (RADIUS_AROUND_SOURCE, CONE_RADIUS)
        /// needs none.
        /// </summary>
        public bool TryStartDamageAbility(Client client, RequestPerformAbilityPacket packet)
        {
            var player = client.Player;
            var map = player?.MapChannel;
            var level = (uint)packet.ActionArgId;
            if (client.State != ClientState.Ingame || player == null || map == null || player.RemoveFromMap ||
                !ActionTableManager.Instance.TryGetResolvable(packet.ActionId, level, out var actionInfo, out var info) ||
                !AbilityRequirements.CanUseSkillAbility(player, packet.ActionId, packet.ActionArgId) ||
                !CanBeginAbility(player) || !CanPay(player, info))
                return false;

            var now = _getMonotonicMilliseconds();
            if (player.AbilityReuseDeadlines.TryGetValue(packet.ActionId, out var reuseUntil) && now < reuseUntil)
                return false;

            // Toggles (rage.py and selfdestruct.py isToggle; Sacrifice has no duration): asking again while it runs ends it.
            if (ActionTableManager.ToggleEffects.TryGetValue(actionInfo.Module, out var toggleType) &&
                player.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == toggleType) is { } running)
            {
                GameEffectManager.Instance.DettachEffect(map, player, running);
                return false;
            }

            Creature target = null;
            Manifestation friendly = null;
            DynamicObject contentTarget = null;
            if (actionInfo.Module == "abilities.cure")
            {
                // cure.py TARGET_FRIENDLY; the resuscitating pumps want a dead player, the others a living one, and the
                // squad pumps no target at all.
                if (!info.Has(AbilityProperty.RadiusAroundSource))
                {
                    var wanted = packet.Target is null or 0 ? player.EntityId : packet.Target.Value;
                    if (wanted == player.EntityId)
                        friendly = player;
                    else if (!EntityManager.Instance.Players.TryGetValue(wanted, out friendly))
                        return false;
                    if (!ReferenceEquals(friendly.MapChannel, map) ||
                        (friendly.State == CharacterState.Dead) != info.Has(AbilityProperty.AttributeMaxChange) ||
                        info.MaxRange > 0 && System.Numerics.Vector3.Distance(player.Position, friendly.Position) > info.MaxRange + AbilityRangeSlack)
                        return false;
                }
            }
            else if (actionInfo.Module == "abilities.firesupport")
            {
                // firesupport.py TARGET_LOCATION: a point on the ground, or (at the targeted pumps) an enemy.
                if (packet.TargetLocation is { } point)
                {
                    var spot = new System.Numerics.Vector3((float)point.X, (float)point.Y, (float)point.Z);
                    if (info.MaxRange > 0 && System.Numerics.Vector3.Distance(player.Position, spot) > info.MaxRange + AbilityRangeSlack)
                        return false;
                }
                else
                {
                    target = GetLightningTarget(map, player, packet.Target ?? 0);
                    if (target == null || info.MaxRange > 0 &&
                        System.Numerics.Vector3.Distance(player.Position, target.Position) > info.MaxRange + AbilityRangeSlack)
                        return false;
                }
            }
            else if (actionInfo.Module == "abilities.corpseexplode" || actionInfo.Module == "abilities.reanimation")
            {
                // corpseexplode.py canTargetDead, and only a creature's corpse.
                if (!EntityManager.Instance.Creatures.TryGetValue(packet.Target ?? 0, out var corpse) || corpse.State != CharacterState.Dead ||
                    !MapChannelManager.IsOnChannel(corpse, map) || info.MaxRange > 0 &&
                    System.Numerics.Vector3.Distance(player.Position, corpse.Position) > info.MaxRange + AbilityRangeSlack)
                    return false;
                target = corpse;
            }
            else if (ActionTableManager.EnemyModules.Contains(actionInfo.Module))
            {
                // A single enemy (decay.py TARGET_NON_FRIENDLY); a destroyable placement cannot decay.
                target = GetLightningTarget(map, player, packet.Target ?? 0);
                if (target == null || info.MaxRange > 0 &&
                    System.Numerics.Vector3.Distance(player.Position, target.Position) > info.MaxRange + AbilityRangeSlack)
                    return false;
            }
            else if (ActionTableManager.FriendlyModules.Contains(actionInfo.Module))
            {
                friendly = GetFriendlyTarget(map, player, packet.Target);
                if (friendly == null || info.MaxRange > 0 &&
                    System.Numerics.Vector3.Distance(player.Position, friendly.Position) > info.MaxRange + AbilityRangeSlack)
                    return false;
            }
            else if (!ActionTableManager.SelfModules.Contains(actionInfo.Module) && !AimedFromSource(info))
            {
                target = GetLightningTarget(map, player, packet.Target ?? 0);
                contentTarget = target == null ? WeaponAttackManager.GetEligibleContentTarget(player, packet.Target ?? 0) : null;
                if (target == null && contentTarget == null)
                    return false;
                if (target != null && info.MaxRange > 0 &&
                    System.Numerics.Vector3.Distance(player.Position, target.Position) > info.MaxRange + AbilityRangeSlack)
                    return false;
            }

            WeaponActionManager.Instance.InterruptForAbility(client);
            var targetEntityId = target?.EntityId ?? friendly?.EntityId ?? contentTarget?.EntityId ?? 0;
            var action = new ActionData(player, packet.ActionId, level, targetEntityId, info.WindupMs)
            {
                TargetLocation = packet.TargetLocation, ItemId = packet.ItemId, ClientYaw = packet.ClientYaw
            };
            var windupEndsAt = now + info.WindupMs;
            var recoveryEndsAt = windupEndsAt + info.RecoveryMs;
            player.CurrentAbility = new AbilityExecution(action, map, (Actor)target ?? friendly, contentTarget, windupEndsAt,
                recoveryEndsAt, recoveryEndsAt + info.ReuseMs) { Level = info };
            CellManager.Instance.CellCallMethod(map, player,
                new PerformWindupPacket(PerformType.ThreeArgs, action.ActionId, level, targetEntityId));
            return true;
        }

        /// <summary>A friendly ability's target: the performer when nothing (or they) is named, else a living player here.</summary>
        private static Manifestation GetFriendlyTarget(MapChannel map, Manifestation player, ulong? targetId)
        {
            if (targetId == null || targetId == 0 || targetId == player.EntityId)
                return player;
            return EntityManager.Instance.Players.TryGetValue(targetId.Value, out var other) &&
                ReferenceEquals(other.MapChannel, map) && other.State != CharacterState.Dead ? other : null;
        }

        /// <summary>Area-around-source and cone abilities are aimed from the performer, not at a target.</summary>
        public static bool AimedFromSource(ActionLevelInfo info)
            => info.Has(AbilityProperty.RadiusAroundSource) || info.Has(AbilityProperty.ConeRadius);

        /// <summary>The action's costs, scaled by CONSUMABLE_SCALE_TYPE where the row has one.</summary>
        private static int CostOf(Manifestation player, ActionLevelInfo info, ActionCost cost)
            => AbilityScaling.ScaleActorAmount(cost.Amount, player.Level,
                info.Has(AbilityProperty.ConsumableScaleType) ? info.Get(AbilityProperty.ConsumableScaleType) : (int?)null);

        private static bool CanPay(Manifestation player, ActionLevelInfo info)
        {
            foreach (var cost in info.Costs)
                if (!player.Attributes.TryGetValue(cost.Attribute, out var attribute) || attribute.Current < CostOf(player, info, cost))
                    return false;
            return true;
        }

        /// <summary>
        /// Who a damage ability reaches when it lands: the target alone; the target and every hostile within
        /// RADIUS_AROUND_TARGET of it; every hostile within RADIUS_AROUND_SOURCE of the performer; or, for a cone, every
        /// hostile within the ability's range and inside the cone.
        ///
        /// CONE_RADIUS is an angle, not a distance: client/targeting.py turns it into the cone reticle's width with
        /// radius * 2 * pi / 360, and takes the reach from the ability's max range. Whether the angle is the half-width
        /// or the full width is not in the client; it is taken as the half-width (inferred).
        /// </summary>
        private static List<Creature> DamageAbilityTargets(MapChannel map, Manifestation player, ActionLevelInfo info, Creature primary, double? yaw)
        {
            var targets = new List<Creature>();
            Func<Creature, bool> reaches = null;
            if (info.Has(AbilityProperty.ConeRadius))
            {
                var halfAngle = info.Get(AbilityProperty.ConeRadius) * Math.PI / 180.0;
                var facing = yaw ?? player.Rotation;
                var reach = info.MaxRange;
                reaches = creature =>
                {
                    var dx = creature.Position.X - player.Position.X;
                    var dz = creature.Position.Z - player.Position.Z;
                    var distance = Math.Sqrt(dx * dx + dz * dz);
                    if (distance > reach)
                        return false;
                    if (distance < 0.01)
                        return true;
                    var cos = (dx * Math.Sin(facing) + dz * Math.Cos(facing)) / distance;
                    return Math.Acos(Math.Clamp(cos, -1.0, 1.0)) <= halfAngle;
                };
            }
            else if (info.Has(AbilityProperty.RadiusAroundSource))
            {
                var radius = info.Get(AbilityProperty.RadiusAroundSource);
                reaches = creature => System.Numerics.Vector3.Distance(player.Position, creature.Position) <= radius;
            }
            else
            {
                if (primary != null)
                    targets.Add(primary);
                if (primary != null && info.Has(AbilityProperty.RadiusAroundTarget))
                {
                    var centre = primary.Position;
                    var radius = info.Get(AbilityProperty.RadiusAroundTarget);
                    reaches = creature => System.Numerics.Vector3.Distance(centre, creature.Position) <= radius;
                }
            }

            if (reaches != null && player.Cells != null)
                foreach (var seed in player.Cells)
                    if (map.MapCellInfo.Cells.TryGetValue(seed, out var cell))
                        foreach (var creature in cell.CreatureList)
                            if (!targets.Contains(creature) &&
                                ReferenceEquals(GetLightningTarget(map, player, creature.EntityId), creature) && reaches(creature))
                                targets.Add(creature);
            return targets;
        }

        private void ResolveDamageAbility(MapChannel map, Client client, AbilityExecution execution, long now)
        {
            var player = client.Player;
            var action = execution.Action;
            var info = execution.Level;
            var contentTargetValid = execution.OriginalContentTarget != null &&
                ReferenceEquals(WeaponAttackManager.GetEligibleContentTarget(player, action.TargetId), execution.OriginalContentTarget);
            ActionTableManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var rowAction, out _);
            var friendlyAbility = ActionTableManager.FriendlyModules.Contains(rowAction?.Module ?? "");
            var isCure = rowAction?.Module == "abilities.cure";
            var targeted = !friendlyAbility && !isCure && rowAction?.Module != "abilities.firesupport" && rowAction?.Module != "abilities.corpseexplode" && rowAction?.Module != "abilities.reanimation" &&
                (ActionTableManager.EnemyModules.Contains(rowAction?.Module ?? "") ||
                !ActionTableManager.SelfModules.Contains(rowAction?.Module ?? "") && !AimedFromSource(info));
            if (friendlyAbility && (execution.OriginalTarget == null || execution.OriginalTarget.State == CharacterState.Dead ||
                    !ReferenceEquals((execution.OriginalTarget as Manifestation)?.MapChannel, map)))
            {
                EndAbility(client, execution, false);
                return;
            }
            if (!AbilityRequirements.CanUseSkillAbility(player, action.ActionId, (int)action.ActionArgId) || !CanPay(player, info) ||
                (targeted && execution.OriginalContentTarget == null &&
                    !ReferenceEquals(GetLightningTarget(map, player, action.TargetId), execution.OriginalTarget)) ||
                (targeted && execution.OriginalContentTarget != null && !contentTargetValid))
            {
                EndAbility(client, execution, false);
                return;
            }

            // Any combat action ends Cloak Wave's stealth.
            AbilityEffects.BreakStealth(map, player);

            // Paid on landing, as Lightning is.
            foreach (var cost in info.Costs)
            {
                var attribute = player.Attributes[cost.Attribute];
                attribute.Current -= CostOf(player, info, cost);
                if (cost.Attribute == Attributes.Power)
                    CellManager.Instance.CellCallMethod(map, player, new UpdatePowerPacket(attribute, player.EntityId));
                else if (cost.Attribute == Attributes.Chi)
                    client.CallMethod(player.EntityId, new UpdateChiPacket(attribute, player.EntityId));
            }
            execution.Resolved = true;
            player.AbilityReuseDeadlines[action.ActionId] = execution.ReuseEndsAt;

            ActionTableManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out var actionInfo, out _);
            if (ActionTableManager.EffectModules.Contains(actionInfo?.Module ?? ""))
            {
                // BaseActorAbility.DoAbility reads no hit data: these abilities show their result through the game
                // effect they attach.
                switch (actionInfo.Module)
                {
                    case "abilities.decay":
                        AbilityEffects.Decay(map, player, execution.OriginalTarget as Creature, info, _damageRandom);
                        break;
                    case "abilities.rage":
                        AbilityEffects.Rage(map, client, info);
                        break;
                    case "abilities.bioaugmentation":
                    case "abilities.shieldextender":
                        var targetClient = execution.OriginalTarget == player ? client
                            : map.ClientList?.FirstOrDefault(c => c?.Player == execution.OriginalTarget);
                        if (actionInfo.Module == "abilities.bioaugmentation")
                            AbilityEffects.BioAugmentation(map, player, targetClient, info);
                        else
                            AbilityEffects.ShieldExtender(map, player, targetClient, info);
                        break;
                    case "abilities.scourge":
                        AbilityEffects.Scourge(map, player, info, _damageRandom);
                        break;
                    case "abilities.reflection":
                        AbilityEffects.Reflection(map, player, action.ActionId, info);
                        break;
                    case "abilities.conversion":
                        AbilityEffects.Conversion(map, client, info);
                        break;
                    case "abilities.shieldwave":
                        AbilityEffects.ShieldWave(map, client, info);
                        break;
                    case "abilities.regenerationwave":
                        AbilityEffects.RegenerationWave(map, client, info);
                        break;
                    case "abilities.resistance":
                        AbilityEffects.Resistance(map, client, info);
                        break;
                    case "abilities.disease":
                        AbilityEffects.Disease(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.damageconversion":
                        AbilityEffects.ViralConversion(map, player, info);
                        break;
                    case "abilities.sacrifice":
                        AbilityEffects.Sacrifice(map, player, info);
                        break;
                    case "abilities.selfdestruct":
                        AbilityEffects.SelfDestruct(map, client, info, _damageRandom);
                        break;
                    case "abilities.scatterbombs":
                        AbilityEffects.Scatterbombs(map, player, info, _damageRandom);
                        break;
                    case "abilities.weaponenhancement":
                        var enhanced = execution.OriginalTarget == player ? client
                            : map.ClientList?.FirstOrDefault(c => c?.Player == execution.OriginalTarget);
                        AbilityEffects.ShredderAmmo(map, player, enhanced, info);
                        break;
                    case "abilities.calledshot":
                        AbilityEffects.CalledShot(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.controlledfission":
                        AbilityEffects.ControlledFission(map, player, execution.OriginalTarget as Creature, info, _damageRandom);
                        break;
                    case "abilities.explodingnanites":
                        AbilityEffects.ExplosiveNanites(map, player, execution.OriginalTarget as Creature, info, _damageRandom);
                        break;
                    case "abilities.polarityfield":
                        AbilityEffects.PolarityField(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.feedback":
                        AbilityEffects.Feedback(map, player, execution.OriginalTarget as Creature, info, _damageRandom);
                        break;
                    case "abilities.realityripper":
                        AbilityEffects.RealityRipper(map, player,
                            action.TargetLocation is { } rip ? new System.Numerics.Vector3((float)rip.X, (float)rip.Y, (float)rip.Z) : player.Position,
                            info, _damageRandom);
                        break;
                    case "abilities.cloakwave":
                        AbilityEffects.CloakWave(map, client, info);
                        break;
                    case "abilities.traitor":
                        AbilityEffects.Traitor(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.hack":
                        AbilityEffects.Hack(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.mindcontrol":
                        var controlled = AbilityEffects.MindControl(map, player, execution.OriginalTarget as Creature, action.ActionId, info, _damageRandom);
                        CellManager.Instance.CellCallMethod(map, player, new Packets.MapChannel.Server.PerformRecovery.EffectListRecovery(
                            action.ActionId, action.ActionArgId, controlled == null ? Array.Empty<(ulong, int)>()
                                : new[] { (execution.OriginalTarget.EntityId, AbilityEffects.MindControlType) }));
                        client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                        {
                            (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                        }));
                        return;
                    case "abilities.reanimation":
                        AbilityEffects.Reanimate(map, player, execution.OriginalTarget as Creature, info);
                        break;
                    case "abilities.reanimationwave":
                        AbilityEffects.ReanimationWave(map, player, info);
                        break;
                    case "abilities.corpseexplode":
                        AbilityEffects.CadaverImmolation(map, player, execution.OriginalTarget as Creature, info, _damageRandom);
                        break;
                    case "abilities.firesupport":
                        AbilityEffects.FireSupport(map, player, info,
                            action.TargetLocation is { } spot ? new System.Numerics.Vector3((float)spot.X, (float)spot.Y, (float)spot.Z) : null,
                            execution.OriginalTarget as Creature, _damageRandom);
                        break;
                    case "abilities.cure":
                        var cured = map.ClientList?.FirstOrDefault(c => c?.Player != null && c.Player == execution.OriginalTarget);
                        CellManager.Instance.CellCallMethod(map, player, AbilityEffects.Cure(map, client, cured, info, action.ActionId, action.ActionArgId));
                        client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                        {
                            (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                        }));
                        return;
                    case "abilities.tacticalevasion":
                        var evaded = AbilityEffects.TacticalEvasion(map, client, info);
                        CellManager.Instance.CellCallMethod(map, player, new Packets.MapChannel.Server.PerformRecovery.IdListRecovery(
                            action.ActionId, action.ActionArgId, evaded));
                        client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                        {
                            (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                        }));
                        return;
                    case "abilities.reconstruction":
                        var entries = AbilityEffects.Reconstruction(map, client, info, _damageRandom);
                        CellManager.Instance.CellCallMethod(map, player, new Packets.MapChannel.Server.PerformRecovery.EffectListRecovery(
                            action.ActionId, action.ActionArgId, entries));
                        client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                        {
                            (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                        }));
                        return;
                }
                CellManager.Instance.CellCallMethod(map, player, new Packets.MapChannel.Server.PerformRecovery.DamageAbilityRecovery(action.ActionId, action.ActionArgId,
                    new[] { execution.OriginalTarget?.EntityId ?? player.EntityId }, Array.Empty<HitData>()));
                client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
                {
                    (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
                }));
                return;
            }

            var scaleType = info.Has(AbilityProperty.DamageScaleType) ? info.Get(AbilityProperty.DamageScaleType) : (int?)null;
            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = Math.Max(min, info.Get(AbilityProperty.DamageAmountMax, min));
            int Roll() => AbilityScaling.ScaleActorAmount(_damageRandom.Next(min, max + 1), player.Level, scaleType);
            var damageType = (DamageType)info.Get(AbilityProperty.DamageType, (int)DamageType.Physical);
            var hits = DamageAbilityTargets(map, player, info, execution.OriginalTarget as Creature, action.ClientYaw)
                .Select(target => (target, Roll())).ToList();
            MissileManager.Instance.AbilityStrike(map, player, action.ActionId, action.ActionArgId, damageType, hits,
                targeted ? execution.OriginalContentTarget : null, Roll());
            foreach (var (target, _) in hits)
                AbilityEffects.Apply(map, player, target, info);
            client.CallMethod(player.EntityId, new ActionReuseTimesPacket(new[]
            {
                (action.ActionId, Math.Max(0, execution.ReuseEndsAt - now))
            }));
        }

        private static Creature GetLightningTarget(MapChannel map, Manifestation player, ulong targetId)
        {
            var entities = EntityManager.Instance;
            if (map.MapInfo == null || player.MapContextId != map.MapInfo.MapContextId ||
                !entities.RegisteredEntities.TryGetValue(targetId, out var type) || type != EntityType.Creature ||
                !entities.Creatures.TryGetValue(targetId, out var target) ||
                target.MapContextId != map.MapInfo.MapContextId || !MapChannelManager.IsOnChannel(target, map) ||
                target.State == CharacterState.Dead ||
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

            if (!execution.Resolved && now >= execution.WindupEndsAt && execution.Level != null)
            {
                ResolveDamageAbility(map, client, execution, now);
                if (!execution.Resolved)
                    return;
            }
            else if (!execution.Resolved && now >= execution.WindupEndsAt)
            {
                var action = execution.Action;
                var contentTargetValid = execution.OriginalContentTarget != null &&
                    ReferenceEquals(WeaponAttackManager.GetEligibleContentTarget(player, action.TargetId), execution.OriginalContentTarget);
                if (!AbilityRequirements.CanUseSkillAbility(player, action.ActionId, (int)action.ActionArgId) ||
                    (execution.OriginalContentTarget == null &&
                        !ReferenceEquals(GetLightningTarget(map, player, action.TargetId), execution.OriginalTarget)) ||
                    (execution.OriginalContentTarget != null && !contentTargetValid) ||
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

        /// <summary>
        /// Forgets every queued action of an actor that is leaving the world. An action fires
        /// after its wait time on the world loop, and looks its actor's client up when it does;
        /// a player who disconnected in the meantime is no longer in the client list, and a
        /// reload that found nobody used to take the whole server down with it. Every map is
        /// swept rather than the actor's own, since which map the actor thinks it is on is not
        /// always the one its actions were queued on.
        /// </summary>
        public void RemoveActor(Actor actor)
        {
            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                mapChannel.PerformRecovery.RemoveAll(action => action.Actor == actor);
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
                case ActionId.Gesture:
                    GestureManager.Instance.PerformRecovery(mapChannel, action);
                    break;
                case ActionId.AaRecruitLightning:
                    if (action.Actor is Manifestation player && player.Level >= 1 &&
                        action.ActionArgId >= 1 && action.ActionArgId <= 5)
                    {
                        var range = LightningAbilityData.GetBaseDamageRange(action.ActionArgId, player.Level);
                        MissileManager.Instance.MissileLaunch(mapChannel, action,
                            _damageRandom.Next(range.Minimum, range.Maximum + 1));
                        // Ranks 4 and 5 stun: 50% for 3 s in the client's row (194, 4-5).
                        if (EntityManager.Instance.GetCreature(action.TargetId) is { } struck &&
                            ActionTableManager.Instance.TryGetLevel(action.ActionId, action.ActionArgId, out _, out var lightningRow))
                            AbilityEffects.Apply(mapChannel, player, struck, lightningRow);
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
                        case KraftwerksManager.UseObjectArgId:
                            KraftwerksManager.Instance.UseRecovery(mapChannel, action);
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
                case ActionId.ToolHealingDisc:
                case ActionId.ToolFieldRepair:
                case ActionId.ToolArmorAugmentation:
                case ActionId.ToolHarvest:
                case ActionId.ToolCipher:
                    ToolActionManager.Instance.PerformRecovery(mapChannel, action);
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
