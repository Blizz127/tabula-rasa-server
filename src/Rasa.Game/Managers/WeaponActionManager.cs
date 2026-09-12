using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Game;
using Rasa.Packets.Inventory.Server;
using Rasa.Packets.MapChannel.Server;
using Rasa.Repositories.Char.Items;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;

namespace Rasa.Managers
{
    public sealed class WeaponActionManager
    {
        private static readonly Lazy<WeaponActionManager> Singleton = new(() =>
            new WeaponActionManager(Server.GameUnitOfWorkFactory, () => Environment.TickCount64));
        public static WeaponActionManager Instance => Singleton.Value;
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly Func<long> _clock;

        public WeaponActionManager(IGameUnitOfWorkFactory factory, Func<long> clock)
        {
            _factory = factory;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public static bool CanAct(Client client) => client?.State == ClientState.Ingame &&
            client.Player?.MapChannel != null && !client.Player.RemoveFromMap &&
            !client.Player.Disconected && client.Player.State != CharacterState.Dead;

        public static Item CurrentWeapon(Client client)
        {
            var inventory = client?.Player?.Inventory;
            return inventory?.EquippedInventory.Count > 13 &&
                EntityManager.Instance.Items.TryGetValue(inventory.EquippedInventory[13], out var weapon) &&
                weapon.ItemTemplate?.WeaponInfo != null ? weapon : null;
        }

        public static WeaponClassInfo ClassInfo(Item weapon) => weapon?.ItemTemplate != null &&
            EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(weapon.ItemTemplate.Class, out var info)
                ? info.WeaponClassInfo : null;

        public bool Request(Client client, ActionId actionId, bool clientRequested)
        {
            var weapon = CurrentWeapon(client);
            var info = ClassInfo(weapon);
            if (info == null)
                return false;
            var argumentId = actionId switch
            {
                ActionId.WeaponDraw => info.DrawActionId,
                ActionId.WeaponStow => info.StowActionId,
                ActionId.WeaponReload => info.ReloadActionId,
                _ => 0u
            };
            var current = client.Player.CurrentWeaponAction;
            if (current?.ActionId == actionId && current.ArgumentId == argumentId)
                return false; // A same-pair failure could discard the accepted request.
            if (!CanAct(client) || client.Player.CurrentAbility != null || client.Player.CurrentAction != 0 ||
                current != null && current.ActionId != ActionId.WeaponReload ||
                GetReuseRemaining(client.Player, actionId) > 0 ||
                !WeaponActionData.TryGet(actionId, argumentId, out var timing) ||
                actionId == ActionId.WeaponReload && (!client.Player.WeaponReady ||
                    CreateReloadPlan(client, weapon, info).Transfers.Count == 0))
            {
                Reject(client, actionId, argumentId, clientRequested);
                return false;
            }

            InterruptForAbility(client); // Reload's actionInterrupts flag also permits other weapon actions.
            var windup = actionId == ActionId.WeaponReload
                ? weapon.ItemTemplate.WeaponInfo.ReloadTime : (long)timing.WindupMilliseconds;
            var windupEnd = _clock() + windup;
            var recoveryEnd = windupEnd + timing.RecoveryMilliseconds;
            var execution = new WeaponActionExecution(actionId, argumentId, weapon, client.Player.MapChannel,
                windupEnd, recoveryEnd, recoveryEnd + timing.ReuseMilliseconds, clientRequested);
            client.Player.CurrentWeaponAction = execution;
            var packet = new PerformWindupPacket(PerformType.TwoArgs, actionId, argumentId);
            if (clientRequested)
                client.CellIgnoreSelfCallMethod(client, packet);
            else
                CellManager.Instance.CellCallMethod(execution.Map, client.Player, packet);
            // Draw/stow windup is zero in the original table. Their recovery
            // animation starts now; the actor remains busy for that full duration.
            Update(client, _clock());
            return true;
        }

        public void Update(Client client) => Update(client, _clock());

        public void Update(Client client, long now)
        {
            var player = client.Player;
            var execution = player?.CurrentWeaponAction;
            if (execution == null)
                return;
            if (!CanAct(client) || !ReferenceEquals(player.MapChannel, execution.Map) ||
                !ReferenceEquals(CurrentWeapon(client), execution.Weapon) ||
                execution.ActionId == ActionId.WeaponReload && !player.WeaponReady)
            {
                Cancel(client, false);
                return;
            }

            if (!execution.Resolved && now >= execution.WindupEndsAt)
            {
                if (execution.ActionId == ActionId.WeaponReload)
                {
                    var completed = false;
                    try
                    {
                        completed = CompleteReload(client, execution);
                    }
                    catch (System.Data.Common.DbException exception)
                    {
                        Logger.WriteLog(LogType.Error, exception);
                    }
                    if (!completed)
                    {
                        Cancel(client, false);
                        return;
                    }
                    execution.BeginReloadRecovery(now);
                }
                else
                    CellManager.Instance.CellCallMethod(execution.Map, player,
                        new PerformRecoveryPacket(PerformType.TwoArgs, execution.ActionId, execution.ArgumentId));
                execution.Resolved = true;
                player.AbilityReuseDeadlines[execution.ActionId] = execution.ReuseEndsAt;
                SendReuse(client, execution.ActionId, now);
            }

            if (execution.Resolved && now >= execution.RecoveryEndsAt)
            {
                // The client sets readiness at its native animation strike, with
                // a forced strike at Finish. No action may fire before recovery ends.
                if (execution.ActionId == ActionId.WeaponDraw || execution.ActionId == ActionId.WeaponStow)
                    player.WeaponReady = execution.ActionId == ActionId.WeaponDraw;
                player.CurrentWeaponAction = null;
            }
        }

        public bool Interrupt(Client client, ActionId actionId, uint argumentId)
        {
            var current = client.Player?.CurrentWeaponAction;
            if (current == null || current.ActionId != actionId || current.ArgumentId != argumentId)
                return false;
            Cancel(client, true);
            return true;
        }

        public void InterruptForAbility(Client client)
        {
            if (client.Player.CurrentWeaponAction?.ActionId == ActionId.WeaponReload)
                Cancel(client, true);
        }

        public void Cancel(Client client, bool interrupted)
        {
            var current = client.Player?.CurrentWeaponAction;
            if (current == null)
                return;
            client.Player.CurrentWeaponAction = null;
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
            SendReuse(client, current.ActionId, _clock());
        }

        private void Reject(Client client, ActionId actionId, uint argumentId, bool clientRequested)
        {
            client.CallMethod(client.Player.EntityId, new ActionFailedPacket(actionId, argumentId));
            if (clientRequested)
                client.CallMethod(client.Player.EntityId, new UserActionFailedPacket(actionId, (int)argumentId));
            SendReuse(client, actionId, _clock());
        }

        private long GetReuseRemaining(Manifestation player, ActionId actionId)
            => player.AbilityReuseDeadlines.TryGetValue(actionId, out var deadline)
                ? Math.Max(0, deadline - _clock()) : 0;

        private static void SendReuse(Client client, ActionId actionId, long now)
        {
            var remaining = client.Player.AbilityReuseDeadlines.TryGetValue(actionId, out var deadline)
                ? Math.Max(0, deadline - now) : 0;
            client.CallMethod(client.Player.EntityId,
                new ActionReuseTimesPacket(new[] { (actionId, remaining) }));
        }

        private static WeaponReloadPlan CreateReloadPlan(Client client, Item weapon, WeaponClassInfo info)
        {
            var ammunition = new List<(uint SlotId, Item Item)>();
            if (info != null && client.AccountEntry != null)
                for (var slot = 50; slot < 100 && slot < client.Player.Inventory.PersonalInventory.Count; slot++)
                    if (EntityManager.Instance.Items.TryGetValue(client.Player.Inventory.PersonalInventory[slot], out var item) &&
                        item.ItemTemplate?.Class == info.AmmoClassId &&
                        item.OwnerId == client.Player.Id && item.OwnerSlotId == slot)
                        ammunition.Add(((uint)slot, item));
            return new WeaponReloadPlan(weapon, info?.ClipSize ?? 0, ammunition);
        }

        private bool CompleteReload(Client client, WeaponActionExecution execution)
        {
            var weapon = execution.Weapon;
            var plan = CreateReloadPlan(client, weapon, ClassInfo(weapon));
            if (plan.Transfers.Count == 0)
                return false;
            using var work = _factory.CreateChar();
            var changes = plan.Transfers.Select(t =>
                new ReloadAmmoChange(t.Item.Id, t.SlotId, t.StackBefore, t.Consumed)).ToArray();
            if (!work.Items.TryReloadWeapon(client.AccountEntry.Id, client.Player.Id, weapon.Id,
                plan.AmmoBefore, plan.AmmoAfter, changes))
                return false;

            foreach (var transfer in plan.Transfers)
            {
                var item = transfer.Item;
                item.StackSize = transfer.StackBefore - transfer.Consumed;
                if (item.StackSize == 0)
                {
                    client.Player.Inventory.PersonalInventory[(int)transfer.SlotId] = 0;
                    EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                    client.CallMethod(SysEntity.ClientInventoryManagerId,
                        new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId));
                }
                else
                    client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));
            }
            weapon.CurrentAmmo = plan.AmmoAfter;
            CellManager.Instance.CellCallMethod(execution.Map, client.Player,
                new WeaponReloadRecoveryPacket(execution.ArgumentId, plan.AmmoAfter));
            return true;
        }
    }
}
