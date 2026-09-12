using System;
using Rasa.Data;
using Rasa.Game;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;

namespace Rasa.Managers
{
    public sealed class WeaponAttackManager
    {
        private static readonly Lazy<WeaponAttackManager> Singleton = new(() =>
            new WeaponAttackManager(Server.GameUnitOfWorkFactory, () => Environment.TickCount64));
        public static WeaponAttackManager Instance => Singleton.Value;
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly Func<long> _clock;
        private readonly Random _random = new();

        public WeaponAttackManager(IGameUnitOfWorkFactory factory, Func<long> clock)
        {
            _factory = factory;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool TryAutoFire(Client client)
        {
            if (!WeaponActionManager.CanAct(client))
                return false;
            var now = _clock();
            Update(client, now);
            if (ActorActionManager.Instance.HasActiveAction(client.Player))
                return false;
            var weapon = WeaponActionManager.CurrentWeapon(client);
            var info = WeaponActionManager.ClassInfo(weapon);
            if (info == null)
                return false;
            if (!client.Player.WeaponReady)
            {
                WeaponActionManager.Instance.Request(client, ActionId.WeaponDraw, false);
                return false;
            }
            if (info.WeaponAttackActionId != ActionId.WeaponMelee && (uint)info.AmmoClassId != 0 &&
                weapon.CurrentAmmo < weapon.ItemTemplate.WeaponInfo.AmmoPerShot)
            {
                WeaponActionManager.Instance.Request(client, ActionId.WeaponReload, false);
                return false;
            }
            return TryStart(client, new RequestWeaponAttackPacket
            {
                ActionId = info.WeaponAttackActionId,
                ActionArgId = (int)info.WeaponAttackArgId,
                TargetId = client.Player.Target == 0 ? null : client.Player.Target
            }, false);
        }

        public bool TryStart(Client client, RequestWeaponAttackPacket request, bool clientRequested = true)
        {
            var player = client.Player;
            var now = _clock();
            Update(client, now);
            var current = player.CurrentWeaponAttack;
            if (current != null && current.ActionId == request.ActionId &&
                current.ArgumentId == (uint)request.ActionArgId)
                return false; // The original failure receiver removes the oldest unresolved request for this action pair.

            var weapon = WeaponActionManager.CurrentWeapon(client);
            var info = WeaponActionManager.ClassInfo(weapon);
            if (!WeaponActionManager.CanAct(client) || info == null || !player.WeaponReady || weapon.IsJammed ||
                request.ActionArgId < 0 || request.IsAltAction || request.TargetLocation.HasValue ||
                request.ActionId != info.WeaponAttackActionId || (uint)request.ActionArgId != info.WeaponAttackArgId ||
                !WeaponAttackData.TryGet(request.ActionId, (uint)request.ActionArgId, out var timing) ||
                current != null || player.CurrentAbility != null || player.CurrentAction != 0 ||
                player.CurrentWeaponAction != null && (!clientRequested || player.CurrentWeaponAction.ActionId != ActionId.WeaponReload) ||
                GetReuseRemaining(player, request.ActionId, now) > 0)
            {
                Reject(client, request, clientRequested, now);
                return false;
            }

            // Action 174 explicitly has useAmmoInAction=0. Weapons without an
            // ammunition class also have no magazine debit.
            var ammoCost = request.ActionId == ActionId.WeaponMelee || (uint)info.AmmoClassId == 0
                ? 0 : weapon.ItemTemplate.WeaponInfo.AmmoPerShot;
            if ((uint)info.AmmoClassId != 0 && weapon.CurrentAmmo == 0 || ammoCost > weapon.CurrentAmmo)
            {
                Reject(client, request, clientRequested, now);
                return false;
            }

            var target = GetEligibleTarget(player, request.TargetId ?? 0);
            WeaponActionManager.Instance.InterruptForAbility(client);
            var execution = new WeaponAttackExecution(request.ActionId, (uint)request.ActionArgId,
                weapon, player.MapChannel, target, ammoCost, now, timing, clientRequested);
            player.CurrentWeaponAttack = execution;
            var packet = new PerformWindupPacket(PerformType.ThreeArgs, execution.ActionId,
                execution.ArgumentId, target?.EntityId ?? 0);
            if (clientRequested)
                client.CellIgnoreSelfCallMethod(client, packet);
            else
                CellManager.Instance.CellCallMethod(execution.Map, player, packet);
            Update(client, now);
            return true;
        }

        private static Creature GetEligibleTarget(Manifestation player, ulong id)
        {
            var map = player.MapChannel;
            var entities = EntityManager.Instance;
            if (map?.MapInfo == null || player.MapContextId != map.MapInfo.MapContextId ||
                !entities.RegisteredEntities.TryGetValue(id, out var type) || type != EntityType.Creature ||
                !entities.Creatures.TryGetValue(id, out var target) || target.MapContextId != player.MapContextId ||
                target.State == CharacterState.Dead || target.Faction == Factions.AFS)
                return null;
            // Invalid targets become blind shots in the original base attack.
            // Native geometry, neutral/object categories and wargames still need reconstruction.
            return target;
        }

        public void Update(Client client, long now)
        {
            var player = client.Player;
            var current = player?.CurrentWeaponAttack;
            if (current == null)
                return;
            var info = WeaponActionManager.ClassInfo(current.Weapon);
            if (!WeaponActionManager.CanAct(client) || !ReferenceEquals(player.MapChannel, current.Map) ||
                !ReferenceEquals(WeaponActionManager.CurrentWeapon(client), current.Weapon) ||
                !player.WeaponReady || current.Weapon.IsJammed || info == null ||
                info.WeaponAttackActionId != current.ActionId || info.WeaponAttackArgId != current.ArgumentId)
            {
                Cancel(client, false, now);
                return;
            }

            if (!current.Resolved && now >= current.WindupEndsAt)
            {
                if (!SpendAmmo(client, current))
                {
                    Cancel(client, false, now);
                    return;
                }
                current.Resolved = true;
                if (current.StartsReuse)
                    player.AbilityReuseDeadlines[current.ActionId] = current.ReuseEndsAt;

                var target = current.Target != null &&
                    ReferenceEquals(GetEligibleTarget(player, current.Target.EntityId), current.Target)
                        ? current.Target : null;
                // The existing damage amount model is retained; original modifier,
                // hit-chance and impact/flight timing remain separate reconstruction.
                var damage = info.MinDamage + _random.Next(0, info.MaxDamage - info.MinDamage + 1);
                MissileManager.Instance.MissileTrigger(current.Map, new Missile
                {
                    Source = player, ActionId = current.ActionId, ActionArgId = current.ArgumentId,
                    TargetActor = target, TargetEntityId = current.Target?.EntityId ?? 0,
                    DamageA = damage, DamageType = (DamageType)info.DamageType
                });
                SendReuse(client, current.ActionId, now);
            }
            if (current.Resolved && now >= current.RecoveryEndsAt)
                player.CurrentWeaponAttack = null;
        }

        private bool SpendAmmo(Client client, WeaponAttackExecution current)
        {
            if (current.AmmoCost == 0)
                return true;
            var weapon = current.Weapon;
            if (client.AccountEntry == null || weapon.CurrentAmmo < current.AmmoCost)
                return false;
            try
            {
                using var work = _factory.CreateChar();
                if (!work.Items.TrySpendWeaponAmmo(client.AccountEntry.Id, client.Player.Id, weapon.Id,
                    weapon.CurrentAmmo, current.AmmoCost))
                    return false;
            }
            catch (System.Data.Common.DbException exception)
            {
                Logger.WriteLog(LogType.Error, exception);
                return false;
            }
            // Successful windup-resolution debit is an ordering inference, not a
            // recovered original server transaction boundary.
            weapon.CurrentAmmo -= current.AmmoCost;
            client.CallMethod(weapon.EntityId, new WeaponAmmoInfoPacket(weapon.CurrentAmmo));
            return true;
        }

        public bool Interrupt(Client client, ActionId actionId, uint argumentId)
        {
            var current = client.Player?.CurrentWeaponAttack;
            if (current == null || current.ActionId != actionId || current.ArgumentId != argumentId)
                return false;
            Cancel(client, true, _clock());
            return true;
        }

        public void Cancel(Client client, bool interrupted) => Cancel(client, interrupted, _clock());

        private void Cancel(Client client, bool interrupted, long now)
        {
            var current = client.Player?.CurrentWeaponAttack;
            if (current == null)
                return;
            client.Player.CurrentWeaponAttack = null;
            if (ReferenceEquals(client.Player.MapChannel, current.Map))
            {
                if (interrupted)
                    CellManager.Instance.CellCallMethod(current.Map, client.Player,
                        new ActionInterruptPacket(client.Player.EntityId, current.ActionId, current.ArgumentId));
                else
                    CellManager.Instance.CellCallMethod(current.Map, client.Player,
                        new ActionFailedPacket(current.ActionId, current.ArgumentId));
            }
            if (current.ClientRequested && !current.Resolved)
                client.CallMethod(client.Player.EntityId,
                    new UserActionFailedPacket(current.ActionId, (int)current.ArgumentId));
            SendReuse(client, current.ActionId, now);
        }

        private static void Reject(Client client, RequestWeaponAttackPacket request, bool clientRequested, long now)
        {
            if (clientRequested)
            {
                client.CallMethod(client.Player.EntityId, new ActionFailedPacket(request.ActionId, (uint)request.ActionArgId));
                client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(request.ActionId, request.ActionArgId));
                SendReuse(client, request.ActionId, now);
            }
        }

        private static long GetReuseRemaining(Manifestation player, ActionId actionId, long now)
            => player.AbilityReuseDeadlines.TryGetValue(actionId, out var deadline) ? Math.Max(0, deadline - now) : 0;

        private static void SendReuse(Client client, ActionId actionId, long now)
            => client.CallMethod(client.Player.EntityId, new ActionReuseTimesPacket(new[]
                { (actionId, GetReuseRemaining(client.Player, actionId, now)) }));

        public long GetNextAttemptDelay(Client client)
        {
            var current = client.Player.CurrentWeaponAttack;
            return current == null ? 0 : Math.Max(0,
                (current.StartsReuse ? current.ReuseEndsAt : current.RecoveryEndsAt) - _clock());
        }
    }
}
