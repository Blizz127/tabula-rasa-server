using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Packets.MapChannel.Server;
    using Timer;
    using Structures;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server.PerformRecovery;

    public class MissileManager
    {
        private static MissileManager _instance;
        private static readonly object InstanceLock = new object();
        public readonly Timer Timer = new Timer();

        public static MissileManager Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                lock (InstanceLock)
                {
                    if (_instance == null)
                        _instance = new MissileManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Furthest a missile may be aimed. Cells are 25.6 units and a client is only ever told
        /// about the 5x5 cells around it, so nothing past ~64 units is even on its screen; this
        /// is twice that, which no weapon reaches and no honest client asks for.
        /// </summary>
        private const float MaxTargetDistance = 128f;

        private MissileManager()
        {
        }

        /// <summary>
        /// Entity ids are global, but cells are per map: CellCallMethod indexes this map's cell
        /// table with the target's cell seeds, and a target on another map throws
        /// KeyNotFoundException on the main loop.
        /// </summary>
        private static bool IsOnMap(MapChannel mapChannel, Actor actor)
        {
            // A channel without MapInfo (tests, uninitialized channels) cannot be checked
            // against; treat it as matching rather than rejecting every shot.
            if (actor == null)
                return false;
            if (mapChannel?.MapInfo == null)
                return true;
            // Two instances of a context share its id; the channel itself decides.
            return actor switch
            {
                Creature creature => MapChannelManager.IsOnChannel(creature, mapChannel),
                Manifestation player when player.MapChannel != null => ReferenceEquals(player.MapChannel, mapChannel),
                _ => actor.MapContextId == mapChannel.MapInfo.MapContextId
            };
        }

        private static Actor GetTargetActor(ulong entityId)
        {
            var entities = EntityManager.Instance;
            if (!entities.RegisteredEntities.TryGetValue(entityId, out var type))
                return null;

            switch (type)
            {
                case EntityType.Creature:
                    return entities.Creatures.TryGetValue(entityId, out var creature) ? creature : null;
                case EntityType.Character:
                    return entities.Players.TryGetValue(entityId, out var player) ? player : null;
                default:
                    return null;
            }
        }

        private void DoDamageToCreature(MapChannel mapChannel, Missile missile, Creature creature, HitData hit)
        {
            if (creature.State == CharacterState.Dead)
                return;

            // decrease armor first
            var armorDecrease = Math.Min(missile.DamageA, creature.Attributes[Attributes.Armor].Current);
            // Client combat messages add finalAmt + absorbed. Report the existing
            // partition without counting armor absorption again as final damage.
            // Overkill remains unclamped in this report, as in the previous code.
            hit.Absorbed = (uint)armorDecrease;
            hit.FinalAmt = missile.DamageA - armorDecrease;
            creature.Attributes[Attributes.Armor].Current -= armorDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateArmorPacket(creature.Attributes[Attributes.Armor], creature.EntityId));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(missile.DamageA - armorDecrease, creature.Attributes[Attributes.Health].Current);
            creature.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, creature, new UpdateHealthPacket(creature.Attributes[Attributes.Health], creature.EntityId));
            
            if (creature.Attributes[Attributes.Health].Current <= 0)
            {
                // fix health so it dont regenerate after death
                creature.Attributes[Attributes.Health].Current = 0;
                creature.Attributes[Attributes.Health].RefreshAmount = 0;
                creature.Attributes[Attributes.Health].RefreshPeriod = 0;

                // fix armor so it dont regenerate after death
                creature.Attributes[Attributes.Armor].Current = 0;
                creature.Attributes[Attributes.Armor].RefreshAmount = 0;
                creature.Attributes[Attributes.Armor].RefreshPeriod = 0;
                // kill craeture
                CreatureManager.Instance.HandleCreatureKill(mapChannel, creature, missile.Source);
            }
            else
            {
                // shooting at wandering creatures makes them ANGRY
                if (creature.Controller.CurrentAction == BehaviorManager.BehaviorActionWander || creature.Controller.CurrentAction == BehaviorManager.BehaviorActionFollowingPath)
                    BehaviorManager.Instance.SetActionFighting(creature, missile.Source.EntityId);
            }
        }

        private void DoDamageToPlayer(MapChannel mapChannel, Missile missile, Actor actor, HitData hit)
        {
            if (actor.State == CharacterState.Dead)
                return;

            // Resistance to the missile's damage type scales the hit before armour absorbs it, through the
            // conversion the client's own shared/damageresistance.pyo uses (Deployment 14's diminishing returns:
            // resistance / (resistance + 50)). A missile with no damage type, or a target with no resistance of
            // that type, takes it in full.
            var damage = missile.DamageA;
            if (actor is Manifestation target)
            {
                var resistance = DamageResistance.ResistanceFor(target.ResistanceData, missile.DamageType);
                if (resistance > 0)
                    damage = DamageResistance.ScaleDamage(damage, resistance);
            }

            // decrease armor first
            var armorDecrease = Math.Min(damage, actor.Attributes[Attributes.Armor].Current);
            hit.Absorbed = (uint)armorDecrease;
            hit.FinalAmt = damage - armorDecrease;

            actor.Attributes[Attributes.Armor].Current -= armorDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateArmorPacket(actor.Attributes[Attributes.Armor], 0));

            // decrease health (if armor is depleted)
            var healthDecrease = Math.Min(damage - armorDecrease, actor.Attributes[Attributes.Health].Current);

            actor.Attributes[Attributes.Health].Current -= healthDecrease;
            CellManager.Instance.CellCallMethod(mapChannel, actor, new UpdateHealthPacket(actor.Attributes[Attributes.Health], 0));

            // Zero health is death: control state Dead at once, so later hits, actions and
            // autofire skip the player. The announcement follows the killing recovery
            // (PlayerDeathManager.AnnounceDeath, called from MissileTrigger).
            if (actor.Attributes[Attributes.Health].Current == 0)
            {
                actor.State = CharacterState.Dead;
                hit.DeathBlow = 1;
            }
        }

        public void RequestWeaponAttack(Client client, RequestWeaponAttackPacket packet)
            => WeaponAttackManager.Instance.TryStart(client, packet);

        public void DoWork(MapChannel mapChannel, long delta)
        {
            // ToDo: add check for triggerMissile timer
            foreach (var missile in mapChannel.QueuedMissiles)
                MissileTrigger(mapChannel, missile);

            // empty List
            mapChannel.QueuedMissiles.Clear();
        }

        public void MissileLaunch(MapChannel mapChannel, ActionData action, int damage, DamageType? damageType = null)
        {
            var missile = new Missile
            {
                DamageA = damage,
                DamageType = damageType,
                Source = action.Actor
            };

            // get distance between actors
            Actor targetActor = null;
            var triggerTime = 0; // time between windup and recovery

            if (action.TargetId != 0)
            {
                targetActor = GetTargetActor(action.TargetId);
                if (targetActor == null)
                {
                    // A destroyable content placement is a valid missile target even
                    // though it is not an Actor.
                    if (MissionManager.Instance.Content.IsContentUsableSource(mapChannel, action.TargetId) &&
                        mapChannel.DynamicObjects.FirstOrDefault(candidate => candidate.EntityId == action.TargetId) is { } contentTarget)
                    {
                        missile.TargetEntityId = action.TargetId;
                        missile.TargetActor = null;
                        missile.TriggerTime = 0;
                        missile.ActionId = action.ActionId;
                        missile.ActionArgId = action.ActionArgId;
                        missile.IsAbility = action.ActionId == ActionId.AaRecruitLightning;

                        if (missile.IsAbility == false)
                        {
                            CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.ThreeArgs, missile.ActionId, missile.ActionArgId, missile.TargetEntityId));
                            mapChannel.QueuedMissiles.Add(missile);
                        }
                        else
                            MissileTrigger(mapChannel, missile);
                        return;
                    }

                    Logger.WriteLog(LogType.Error, $"The missile target is missing or not an actor: {action.TargetId}");
                    return;
                }
                missile.TargetEntityId = action.TargetId;

                if (targetActor == null || targetActor.State == CharacterState.Dead)
                    return; // actor is dead, cannot be shot at

                if (!IsOnMap(mapChannel, targetActor))
                {
                    Logger.WriteLog(LogType.Debug, $"MissileLaunch: {action.Actor.EntityId} aimed at {action.TargetId}, which is on map {targetActor.MapContextId}, not {mapChannel.MapInfo?.MapContextId}");
                    return;
                }

                var distance = Vector3.Distance(targetActor.Position, action.Actor.Position);

                if (distance > MaxTargetDistance)
                {
                    Logger.WriteLog(LogType.Debug, $"MissileLaunch: {action.Actor.EntityId} aimed at {action.TargetId} from {distance:F0} units away");
                    return;
                }

                triggerTime = (int)(distance * 0.5f);
            }
            else
            {
                // has no target -> Shoot towards looking angle
                targetActor = null;
                triggerTime = 0;
            }

            // is the missile/action an ability that need needs to use Recv_PerformAbility?
            var isAbility = false;
            if (action.ActionId == ActionId.AaRecruitLightning) // recruit lighting ability
                isAbility = true;
            
            missile.TargetActor = targetActor;
            missile.TriggerTime = triggerTime;
            missile.ActionId = action.ActionId;
            missile.ActionArgId = action.ActionArgId;
            missile.IsAbility = isAbility;

            // send windup and append to queue (only for non-abilities)
            if (isAbility == false)
            {
                CellManager.Instance.CellCallMethod(mapChannel, action.Actor, new PerformWindupPacket(PerformType.ThreeArgs, missile.ActionId, missile.ActionArgId, missile.TargetEntityId));
                
                // add to list
                mapChannel.QueuedMissiles.Add(missile);
            }
            else
            {
                // abilities get applied directly without delay
                MissileTrigger(mapChannel, missile);
            }
        }

        /// <summary>
        /// Lands a damage ability on every target it reached at once, and sends the single recovery the client's
        /// DamageBase.DoAbility expects - one hits list, one hitdata entry per hit. Each target takes its own roll
        /// through the same armour-then-health path a weapon hit takes, so kills, kill rewards and mission credit
        /// work as they do for Lightning. A destroyable content placement named as the target takes its roll too,
        /// reported with the ability's action so action-bound objectives can match it.
        /// </summary>
        public void AbilityStrike(MapChannel mapChannel, Actor source, ActionId actionId, uint actionArgId, DamageType damageType,
            IReadOnlyList<(Creature Target, int Damage)> targets, DynamicObject contentTarget = null, int contentDamage = 0)
        {
            var hitEntities = new List<ulong>();
            var hits = new List<HitData>();
            var killed = new List<Creature>();

            foreach (var (target, damage) in targets)
            {
                if (target == null || target.State == CharacterState.Dead)
                    continue;

                var missile = new Missile
                {
                    DamageA = damage, DamageType = damageType, Source = source, ActionId = actionId, ActionArgId = actionArgId,
                    IsAbility = true, TargetEntityId = target.EntityId, TargetActor = target
                };
                var hit = new HitData { DamageType = damageType, FinalAmt = damage, EntityId = target.EntityId };
                DoDamageToCreature(mapChannel, missile, target, hit);
                if (target.State == CharacterState.Dead)
                {
                    hit.DeathBlow = 1;
                    killed.Add(target);
                }
                hitEntities.Add(target.EntityId);
                hits.Add(hit);
            }

            if (contentTarget != null && MissionManager.Instance.Content.IsContentUsableSource(mapChannel, contentTarget.EntityId))
            {
                var attacker = mapChannel.ClientList?.FirstOrDefault(client => client?.Player == source);
                MissionManager.Instance.Content.DamageContentUsable(mapChannel, contentTarget.EntityId, contentDamage, attacker, (uint)actionId);
                hitEntities.Add(contentTarget.EntityId);
                hits.Add(new HitData { DamageType = damageType, FinalAmt = contentDamage, EntityId = contentTarget.EntityId });
            }

            CellManager.Instance.CellCallMethod(mapChannel, source, new DamageAbilityRecovery(actionId, actionArgId, hitEntities, hits));

            foreach (var creature in killed)
                CellManager.Instance.CellCallMethod(mapChannel, creature, new ActorKilledPacket());
        }

        public void MissileTrigger(MapChannel mapChannel, Missile missile)
        {
            // ToDo: Some weapons can hit multiple targets
            var targetActor = GetTargetActor(missile.TargetEntityId);
            Creature killedCreature = null;
            Manifestation killedPlayer = null;
            // Resolve again at impact: the target may have left, died, or its ID may
            // have been reused since windup. A shot without a target has no hits.
            if (targetActor != null && ReferenceEquals(targetActor, missile.TargetActor)
                && targetActor.State != CharacterState.Dead)
            {
                missile.Args.HitEntities.Add(missile.TargetEntityId);
                var hit = new HitData
                {
                    DamageType = missile.DamageType ??
                        (missile.ActionId == ActionId.AaRecruitLightning ? DamageType.Electrical : DamageType.Physical),
                    FinalAmt = missile.DamageA,
                    EntityId = missile.TargetEntityId
                };
                missile.Args.HitData.Add(hit);

                if (targetActor is Creature creature)
                {
                    DoDamageToCreature(mapChannel, missile, creature, hit);
                    if (creature.State == CharacterState.Dead)
                    {
                        hit.DeathBlow = 1;
                        killedCreature = creature;
                    }
                }
                else
                {
                    DoDamageToPlayer(mapChannel, missile, targetActor, hit);
                    if (targetActor.State == CharacterState.Dead && targetActor is Manifestation player)
                        killedPlayer = player;
                }
            }
            else if (!MissionManager.Instance.Content.IsContentUsableSource(mapChannel, missile.TargetEntityId) &&
                     missile.Source is Manifestation shooter)
            {
                // The shot resolved to neither a living actor nor a destroyable placement, so it hit
                // nothing the server knows about. Silence here reads exactly like a shot that landed.
                Logger.WriteLog(LogType.Debug,
                    $"{shooter.Name} fired action {missile.ActionId} at entity {missile.TargetEntityId}, " +
                    $"which is neither a live actor nor a content usable on map {mapChannel.MapInfo?.MapContextId}.");
            }
            else if (MissionManager.Instance.Content.IsContentUsableSource(mapChannel, missile.TargetEntityId))
            {
                // A destroyable content placement takes the damage; no HitData goes
                // into the recovery, the client learns the result from UpdateHitPoints.
                var attacker = mapChannel.ClientList.FirstOrDefault(client => client?.Player == missile.Source);
                var destroyed = MissionManager.Instance.Content.DamageContentUsable(mapChannel, missile.TargetEntityId, missile.DamageA, attacker, (uint)missile.ActionId);
                missile.Args.HitEntities.Add(missile.TargetEntityId);
            }

            switch (missile.ActionId)
            {
                case ActionId.WeaponAttack:
                // Melee (174) is resolved exactly like a ranged attack: the original server's
                // missile_ActionRecoveryHandler_WeaponMelee forwarded to the WeaponAttack
                // handler "until there is better handling for melee weapons", and the recovery
                // packet is the same shape. It fell through to the default here, which did the
                // right thing but logged every swing as an unsupported action.
                case ActionId.WeaponMelee:
                // The Bane Mortar's launcher (411): client weapons.groundtarget.GroundTargetAttack is a
                // RocketLauncherAttack, itself a BaseWeaponAttack at a target entity, so it resolves the same way.
                case ActionId.WeaponGroundtarget:
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new WeaponAttackRecovery(missile));
                    break;
                case ActionId.AaRecruitLightning:
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new LightningRecovery(missile));
                    break;
                //else if (missile->actionId == 203)
                //    missile_ActionHandler_CR_FOREAN_LIGHTNING(mapChannel, missile);
                //else if (missile->actionId == 211)
                //    missile_ActionHandler_CR_AMOEBOID_SLIME(mapChannel, missile);
                //else if (missile->actionId == 397)
                //    missile_ActionRecoveryHandler_ThraxKick(mapChannel, missile);
                default:
                    Logger.WriteLog(LogType.Debug, $"MissileLaunch: unsupported missile actionId {missile.ActionId} - using default: WeaponAttackRecovery");
                    CellManager.Instance.CellCallMethod(mapChannel, missile.Source, new WeaponAttackRecovery(missile));
                    break;
            }
            // The killing damage announces death to source-visible observers.
            // ActorKilled covers victim-visible observers who missed that recovery;
            // its original client handler ignores an actor already announced dead.
            if (killedCreature != null)
                CellManager.Instance.CellCallMethod(mapChannel, killedCreature, new ActorKilledPacket());
            if (killedPlayer != null)
                PlayerDeathManager.Instance.AnnounceDeath(mapChannel, killedPlayer, missile.Source);
        }
    }
}
