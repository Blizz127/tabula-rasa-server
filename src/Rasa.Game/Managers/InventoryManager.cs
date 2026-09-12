using System;
using System.Collections.Generic;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Inventory.Client;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Client;
    using Packets.MapChannel.Server;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    public class InventoryManager
    {
        /*    Inventory Packets:
         *      Done:
         *  - AddBuybackItem
         *  - InventoryAddItem
         *  - InventoryCreate
         *  - InventoryRemoveItem
         *  - LockboxTabPermissions
         *  - RemoveBuybackItem
         *  
         *      ToDo:
         *  - AddAuctionItem
         *  - AddInboxItem
         *  - AddOverflowItem
         *  - AddWagerItem
         *  - InventoryDestroy
         *  - InventoryMoveFailed
         *  - InventoryReload
         *  - RemoveAuctionItem
         *  - RemoveInboxItem
         *  - RemoveOverflowItem
         *  - RemoveWagerItem
         *  - ResetAuctionInventory
         *  - ResetBuybackInventory
         *  - ResetInboxInventory
         *  - ResetOverflowInventory
         *  - ResetWagerInventory
         *  
         *    Inventory Handlers:
         *  - ClanLockbox_DepositItemInSlot         => implemented
         *  - ClanLockbox_DepositItemInTab          => implemented
         *  - ClanLockbox_DestroyItem               => implemented
         *  - ClanLockbox_MoveItem                  => implemented
         *  - ClanLockbox_WithdrawItem              => implemented
         *  - HomeInventory_DestroyItem             => implemented
         *  - HomeInventory_MoveItem                => implemented
         *  - OverflowTransfer                      => ToDo
         *  - PersonalInventory_DestroyItem         => implemented
         *  - PersonalInventory_MoveItem            => implemented
         *  - PurchaseClanLockboxTab                => ToDo
         *  - PurchaseLockboxTab                    => implemented
         *  - RequestEquipArmor                     => implemented
         *  - RequestEquipWeapon                    => implemented
         *  - RequestLockboxTabPermissions          => implemented
         *  - RequestMoveItemToHomeInventory        => implemented
         *  - RequestTakeItemFromHomeInventory      => implemented
         *  - RequestTakeItemFromInboxInventory     => ToDo
         *  - TransferCreditToLockbox               => implemented
         *  - WeaponDrawerInventory_MoveItem        => implemented
         */

        private static InventoryManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        public static InventoryManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new InventoryManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private InventoryManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        #region Handlers

        public void HomeInventory_DestroyItem(Client client, HomeInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            ReduceStackCount(client, InventoryType.HomeInventory, tempItem, packet.Quantity);
        }

        public void HomeInventory_MoveItem(Client client, HomeInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 480)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 480)
                return;

            var entityId = client.Player.Inventory.HomeInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.HomeInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.Player.Inventory.HomeInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        public void PersonalInventory_DestroyItem(Client client, PersonalInventory_DestroyItemPacket packet)
        {
            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            ReduceStackCount(client, InventoryType.Personal, tempItem, packet.Quantity);
        }

        public void PersonalInventory_MoveItem(Client client, PersonalInventory_MoveItemPacket packet)
        {
            // remove item
            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot > 250)
            {
                Logger.WriteLog(LogType.Debug, $"SrcSlot out of range => {packet.SrcSlot}");
                return;
            }

            if (packet.DestSlot < 0 || packet.DestSlot > 250)
            {
                Logger.WriteLog(LogType.Debug, $"DestSlot out of range => {packet.DestSlot}");
                return;
            }

            var entityId = client.Player.Inventory.PersonalInventory[packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.Personal, (uint)packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.PersonalInventory[packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.Personal, client.Player.Inventory.PersonalInventory[packet.DestSlot], (uint)packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.Personal, entityId, (uint)packet.DestSlot, true);
        }

        public void PurchaseLockboxTab(Client client, PurchaseLockboxTabPacket packet)
        {
            if (client?.State != ClientState.Ingame || client.AccountEntry == null || client.Player == null ||
                client.Player.State == CharacterState.Dead || packet == null ||
                !client.Player.Credits.TryGetValue(CurencyType.Credits, out var wallet))
                return;
            var price = LockboxTabs.PurchasePrice(packet.TabId);
            if (!price.HasValue)
                return;
            try
            {
                using var work = _gameUnitOfWorkFactory.CreateChar();
                if (!work.CharacterLockboxes.TryPurchaseTab(client.AccountEntry.Id, client.Player.Id,
                        wallet, client.Player.LockboxTabs, packet.TabId, price.Value))
                    return;
            }
            catch (System.Data.Common.DbException exception)
            {
                Logger.WriteLog(LogType.Error, exception);
                return;
            }
            client.Player.Credits[CurencyType.Credits] = wallet - price.Value;
            client.Player.LockboxTabs = packet.TabId;
            client.CallMethod(client.Player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, wallet - price.Value, 0));
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(packet.TabId));
        }

        public void RequestEquipArmor(Client client, RequestEquipArmorPacket packet)
        {
            var inventory = client?.Player?.Inventory;
            if (!CanEquipFrom(client, packet.SrcInventory, packet.SrcSlot) ||
                packet.DestSlot >= inventory.EquippedInventory.Count || packet.DestSlot == 13)
                return;
            var incoming = EntityManager.Instance.GetItem(EquipmentSourceSlots(client, packet.SrcInventory)[(int)packet.SrcSlot]);
            var outgoing = EntityManager.Instance.GetItem(inventory.EquippedInventory[(int)packet.DestSlot]);
            if (incoming == null && outgoing == null)
                return;
            if (incoming != null && (!TryGetEquipmentClass(incoming, out var incomingClass) ||
                    (uint)incomingClass.EquipableClassInfo.EquipmentSlotId != packet.DestSlot) ||
                outgoing != null && (!TryGetEquipmentClass(outgoing, out var outgoingClass) ||
                    (uint)outgoingClass.EquipableClassInfo.EquipmentSlotId != packet.DestSlot) ||
                !ValidateItemEquip(client, incoming))
                return;
            if (!TrySwapCharacterSlots(client, packet.SrcInventory, packet.SrcSlot, incoming,
                    InventoryType.EquipedInventory, packet.DestSlot, outgoing))
                return;

            if (incoming == null)
            {
                var slot = (EquipmentData)packet.DestSlot;
                if (client.Player.AppearanceData.ContainsKey(slot))
                    ManifestationManager.Instance.RemoveAppearanceItem(client, slot);
            }
            else
                ManifestationManager.Instance.SetAppearanceItem(client, incoming);
            ManifestationManager.Instance.UpdateAppearance(client);
            ManifestationManager.Instance.UpdateStatsValues(client, false);
            ManifestationManager.Instance.NotifyEquipmentUpdate(client);
            client.CallMethod(client.Player.EntityId, new AttributeInfoPacket(client.Player.Attributes));
        }

        public void RequestEquipWeapon(Client client, RequestEquipWeaponPacket packet)
        {
            var inventory = client?.Player?.Inventory;
            if (!CanEquipFrom(client, packet.InventoryType, packet.SrcSlot) ||
                packet.DestSlot >= inventory.WeaponDrawer.Count || inventory.EquippedInventory.Count <= 13)
                return;
            var incoming = EntityManager.Instance.GetItem(EquipmentSourceSlots(client, packet.InventoryType)[(int)packet.SrcSlot]);
            var outgoing = EntityManager.Instance.GetItem(inventory.WeaponDrawer[(int)packet.DestSlot]);
            if (incoming == null && outgoing == null)
                return;
            if (incoming != null && !IsDrawerWeapon(incoming) || outgoing != null && !IsDrawerWeapon(outgoing) ||
                !ValidateItemEquip(client, incoming))
                return;
            var previousWeaponEntityId = inventory.EquippedInventory[13];
            if (!TrySwapCharacterSlots(client, packet.InventoryType, packet.SrcSlot, incoming,
                    InventoryType.WeaponDrawerInventory, packet.DestSlot, outgoing))
                return;
            if (packet.DestSlot == client.Player.ActiveWeapon)
                ManifestationManager.Instance.RefreshArmedWeapon(client, previousWeaponEntityId);
        }

        private static List<ulong> EquipmentSourceSlots(Client client, InventoryType type)
            => type == InventoryType.Personal ? client?.Player?.Inventory.PersonalInventory :
                type == InventoryType.HomeInventory ? client?.Player?.Inventory.HomeInventory : null;

        private static bool CanEquipFrom(Client client, InventoryType type, uint slot)
        {
            var slots = EquipmentSourceSlots(client, type);
            return client?.State == ClientState.Ingame && slots != null && client.Player.State != CharacterState.Dead &&
                slot < slots.Count && (type != InventoryType.Personal || slot < 50);
        }

        private static bool TryGetEquipmentClass(Item item, out EntityClass classInfo)
        {
            classInfo = null;
            return item?.ItemTemplate != null &&
                EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(item.ItemTemplate.Class, out classInfo) &&
                classInfo.EquipableClassInfo != null;
        }

        private static bool IsDrawerWeapon(Item item)
            => TryGetEquipmentClass(item, out var classInfo) &&
                classInfo.EquipableClassInfo.EquipmentSlotId == EquipmentData.Weapon;

        private bool TrySwapCharacterSlots(Client client, InventoryType sourceType, uint sourceSlot, Item source,
            InventoryType destinationType, uint destinationSlot, Item destination)
        {
            if (client.AccountEntry == null || !MatchesCharacterSlot(client, sourceType, sourceSlot, source) ||
                !MatchesCharacterSlot(client, destinationType, destinationSlot, destination))
                return false;
            try
            {
                using var work = _gameUnitOfWorkFactory.CreateChar();
                if (!work.CharacterInventories.TrySwapItems(client.AccountEntry.Id, client.Player.Id,
                        (uint)sourceType, sourceSlot, source?.Id ?? 0, (uint)destinationType, destinationSlot, destination?.Id ?? 0))
                    return false;
            }
            catch (System.Data.Common.DbException exception)
            {
                Logger.WriteLog(LogType.Error, exception);
                return false;
            }
            // Both persisted locations commit before the in-memory slots and
            // ordered remove/add notifications change. Appearance follows below.
            if (source != null)
                RemoveItemBySlot(client, sourceType, sourceSlot);
            if (destination != null)
                RemoveItemBySlot(client, destinationType, destinationSlot);
            if (destination != null)
                AddItemBySlot(client, sourceType, destination.EntityId, sourceSlot, false);
            if (source != null)
                AddItemBySlot(client, destinationType, source.EntityId, destinationSlot, false);
            return true;
        }

        private static bool MatchesCharacterSlot(Client client, InventoryType type, uint slot, Item item)
        {
            var slots = type == InventoryType.Personal ? client.Player.Inventory.PersonalInventory :
                type == InventoryType.HomeInventory ? client.Player.Inventory.HomeInventory :
                type == InventoryType.EquipedInventory ? client.Player.Inventory.EquippedInventory : client.Player.Inventory.WeaponDrawer;
            var entityId = slots[(int)slot];
            return item == null ? entityId == 0 : item.Id != 0 && item.OwnerId == (type == InventoryType.HomeInventory ? 0u : client.Player.Id) &&
                item.OwnerSlotId == slot && entityId == item.EntityId && item.StackSize > 0 &&
                EntityManager.Instance.Items.TryGetValue(entityId, out var registered) && ReferenceEquals(registered, item);
        }

        public void RequestLockboxTabPermissions(Client client)
        {
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));
        }

        public void RequestMoveItemToClanLockbox(Client client, RequestMoveItemToClanLockboxPacket packet)
        {
            Logger.WriteLog(LogType.Debug, $"ToDO: RequestMoveItemToClanLockboxPacket");
        }

        public void RequestMoveItemToHomeInventory(Client client, RequestMoveItemToHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 480)
                return;

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.HomeInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.Personal, client.Player.Inventory.HomeInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.HomeInventory, entityId, packet.DestSlot, true);
        }

        public void ClanLockbox_DepositItemInSlot(Client client, ClanLockbox_DepositItemInSlotPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 500)
                return;

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.Personal, packet.SrcSlot);

            // If DestSlot is not empty, move current item to SrcSlot (item swap)
            bool wasSwap = client.Player.Inventory.ClanInventory[(int)packet.DestSlot] != 0;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            if (wasSwap)
            {
                unitOfWork.CharacterInventories.DeleteInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.Personal, packet.SrcSlot);
                AddItemBySlot(client, InventoryType.Personal, client.Player.Inventory.ClanInventory[(int)packet.DestSlot], packet.SrcSlot, true, true);

                RemoveItemBySlotForClan(client.Player.ClanId, packet.DestSlot, 0);
                unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.DestSlot);
            }

            AddItemBySlot(client, InventoryType.ClanInventory, entityId, packet.DestSlot, true, true);

            if (!wasSwap)
                unitOfWork.CharacterInventories.DeleteInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.Personal, packet.SrcSlot);

            EntityManager.Instance.GetItem(entityId).OwnerSlotId = packet.DestSlot;
            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, packet.DestSlot, ref client.Player.Inventory.ClanInventory, true);
        }

        public void ClanLockbox_DepositItemInTab(Client client, ClanLockbox_DepositItemInTabPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot > 250)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot > 500)
                return;

            var entityId = client.Player.Inventory.PersonalInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(entityId);

            RemoveItemBySlot(client, InventoryType.Personal, (uint)packet.SrcSlot);

            Item item = AddItemToClanInventory(client, tempItem);
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            unitOfWork.Items.UpdateItemStackSize(tempItem);
            if (item == null)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            unitOfWork.CharacterInventories.DeleteInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.Personal, (uint)packet.SrcSlot);

            if (EntityManager.Instance.GetItem(entityId) == null)
                return;

            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, item.OwnerSlotId, ref client.Player.Inventory.ClanInventory, true);
        }

        public void ClanLockbox_MoveItem(Client client, ClanLockbox_MoveItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.SrcSlot == packet.DestSlot)
                return;

            if (packet.SrcSlot < 0 || packet.SrcSlot >= 500)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot >= 500)
                return;

            var entityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            // If DestSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.ClanInventory[(int)packet.DestSlot] != 0)
            {
                // Todo swap items
                return;
            }
            RemoveItemBySlot(client, InventoryType.ClanInventory, packet.SrcSlot); // Put this above swap if check once swap is implemented

            EntityManager.Instance.GetItem(entityId).OwnerSlotId = packet.DestSlot;
            AddItemBySlot(client, InventoryType.ClanInventory, entityId, packet.DestSlot, true, false);

            RemoveItemBySlotForClan(client.Player.ClanId, packet.SrcSlot, client.Player.Id);
            RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, packet.DestSlot, ref client.Player.Inventory.ClanInventory, true);
        }

        public void ClanLockbox_WithdrawItem(Client client, ClanLockbox_WithdrawItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            // Only the leader and the rank below them can withdraw items from the clan lockbox.
            ClanMemberEntry member = ClanManager.Instance.GetClanMember(client.Player.ClanId, client.Player.Id);
            if (member.Rank < 2)
            {
                client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmClanInsufficientPermissions, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                return;
            }

            if (packet.SrcSlot < 0 || packet.SrcSlot > 500)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot > 250)
                return;

            var entityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(entityId);
            bool wasSwap = client.Player.Inventory.PersonalInventory[(int)packet.DestSlot] != 0;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (packet.ManagePersonalSlot)
            {
                wasSwap = false;
                Item item = AddItemToInventory(client, tempItem);
                unitOfWork.Items.UpdateItemStackSize(tempItem);
                if (item == null)
                {
                    RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);
                    client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInventoryFull, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                    return;
                }
            }
            else
            {
                if (wasSwap)
                {
                    RemoveItemBySlot(client, InventoryType.ClanInventory, packet.SrcSlot);
                    unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.SrcSlot);
                    AddItemBySlot(client, InventoryType.ClanInventory, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot], packet.SrcSlot, true, true);

                    var newEntityId = client.Player.Inventory.ClanInventory[(int)packet.SrcSlot];
                    RefreshClanLockbox(client.Player.ClanId, newEntityId, client.Player.Id, packet.SrcSlot, ref client.Player.Inventory.ClanInventory, true);

                    RemoveItemBySlot(client, InventoryType.Personal, packet.DestSlot);
                    unitOfWork.CharacterInventories.DeleteInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.Personal, packet.DestSlot);
                }
                AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true, true);
            }

            if (!wasSwap)
            {
                RemoveItemBySlotForClan(client.Player.ClanId, packet.SrcSlot, 0);
                unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, packet.SrcSlot);

                RefreshClanLockbox(client.Player.ClanId, entityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);
            }
        }

        public void ClanLockbox_DestroyItem(Client client, ClanLockbox_DestroyItemPacket packet)
        {
            if (client.Player.ClanId == 0)
                return;

            if (packet.EntityId == 0)
                return;

            var tempItem = EntityManager.Instance.GetItem(packet.EntityId);

            //TODO: Support deleting portions
            if ((tempItem.StackSize - packet.Quantity) > 0)
                return;

            RemoveItemBySlotForClan(client.Player.ClanId, tempItem.OwnerSlotId, 0);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            unitOfWork.ClanInventories.DeleteInvItem(client.Player.ClanId, tempItem.OwnerSlotId);

            RefreshClanLockbox(client.Player.ClanId, packet.EntityId, client.Player.Id, 0, ref client.Player.Inventory.ClanInventory, false);
        }

        public void RequestTakeItemFromHomeInventory(Client client, RequestTakeItemFromHomeInventoryPacket packet)
        {
            // remove item
            if (packet.SrcSlot < 0 || packet.SrcSlot > 480)
                return;

            if (packet.DestSlot < 0 || packet.DestSlot > 250)
                return;

            var entityId = client.Player.Inventory.HomeInventory[(int)packet.SrcSlot];

            if (entityId == 0)
                return;

            RemoveItemBySlot(client, InventoryType.HomeInventory, packet.SrcSlot);
            // if toSlot is not empty, move current item to SrcSlot (item swap)
            if (client.Player.Inventory.PersonalInventory[(int)packet.DestSlot] != 0)
                AddItemBySlot(client, InventoryType.HomeInventory, client.Player.Inventory.PersonalInventory[(int)packet.DestSlot], packet.SrcSlot, true);

            AddItemBySlot(client, InventoryType.Personal, entityId, packet.DestSlot, true);
        }

        public void TransferCreditToLockbox(Client client, int amount)
        {
            if (client?.State != ClientState.Ingame || client.AccountEntry == null || client.Player == null ||
                !client.Player.Credits.TryGetValue(CurencyType.Credits, out var wallet) || amount == 0)
                return;
            var lockbox = client.Player.LockboxCredits;
            try
            {
                using var work = _gameUnitOfWorkFactory.CreateChar();
                if (!work.CharacterLockboxes.TryTransferCredits(client.AccountEntry.Id, client.Player.Id, wallet, lockbox, amount))
                    return;
            }
            catch (System.Data.Common.DbException exception)
            {
                Logger.WriteLog(LogType.Error, exception);
                return;
            }
            // Publish only after both persisted balances commit. A withdrawal is
            // a transfer of existing funds, so it must not use the loot helper.
            client.Player.Credits[CurencyType.Credits] = (int)((long)wallet - amount);
            client.Player.LockboxCredits = (int)((long)lockbox + amount);
            client.CallMethod(client.Player.EntityId, new UpdateCreditsPacket(CurencyType.Credits, client.Player.Credits[CurencyType.Credits], 0));
            client.CallMethod(client.Player.EntityId, new LockboxFundsPacket(client.Player.LockboxCredits));
        }

        public void ClanCreditTransfer(Client client, long amount, uint creditType)
        {
            if (client.Player.ClanId == 0)
                return;

            //-amount means withdraw from lockbox +amount means deposit to lockbox

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var clanInfo = unitOfWork.Clans.GetClanById(client.Player.ClanId);

            long remainderOfCredits = (creditType == 1 ? clanInfo.Credits : clanInfo.Prestige) + amount;

            if (amount >= 500 || amount <= -500)
            {
                if ((creditType == 1 && client.Player.Credits[CurencyType.Credits] < amount) ||
                    (creditType == 2 && client.Player.Credits[CurencyType.Prestige] < amount))
                {
                    if (amount > 0)
                        client.CallMethod(SysEntity.CommunicatorId, new DisplayClientMessagePacket(PlayerMessage.PmInsufficientDepositFunds, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
                    return;
                }

                if (remainderOfCredits >= 0)
                {
                    if (creditType == 1)
                        unitOfWork.Clans.UpdateCredits(client.Player.ClanId, (uint)remainderOfCredits);
                    else
                        unitOfWork.Clans.UpdatePrestige(client.Player.ClanId, (uint)remainderOfCredits);

                    CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Credits, amount * -1);
                    var augmentationsList = EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.UsableClanLockboxV01].Augmentations;

                    foreach (var dynamicObj in EntityManager.Instance.DynamicObjects)
                    {
                        DynamicObject dynamicObject = dynamicObj.Value;

                        if (dynamicObject.EntityClassId == EntityClasses.UsableClanLockboxV01)
                            ClanManager.Instance.CallMethodForOnlineMembers(client.Player.ClanId, dynamicObject.EntityId, new UpdateClanLockboxCreditsPacket(creditType == 1 ? (uint)remainderOfCredits : clanInfo.Credits, creditType == 2 ? (uint)remainderOfCredits : clanInfo.Prestige));
                    }
                }
                else
                    CommunicatorManager.Instance.SystemMessage(client, "Not enough credit's");
            }
            else
                CommunicatorManager.Instance.SystemMessage(client, "Minimum transfer value is 500 credits");
        }

        public void WeaponDrawerInventory_MoveItem(Client client, WeaponDrawerInventory_MoveItemPacket packet)
        {
            if (packet.SrcSlot >= client.Player.Inventory.WeaponDrawer.Count ||
                packet.DestSlot >= client.Player.Inventory.WeaponDrawer.Count || packet.SrcSlot == packet.DestSlot)
                return;
            var srcEntityId = client.Player.Inventory.WeaponDrawer[(int)packet.SrcSlot];
            var destEntityId = client.Player.Inventory.WeaponDrawer[(int)packet.DestSlot];
            if (srcEntityId == 0)
                return;
            var previousWeaponEntityId = client.Player.Inventory.EquippedInventory[13];
            // swap items on the client and server
            if (destEntityId != 0)
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.DestSlot);
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, destEntityId, packet.SrcSlot, true);
            }
            else
            {
                RemoveItemBySlot(client, InventoryType.WeaponDrawerInventory, packet.SrcSlot);
                AddItemBySlot(client, InventoryType.WeaponDrawerInventory, srcEntityId, packet.DestSlot, true);
            }
            if (packet.SrcSlot == client.Player.ActiveWeapon || packet.DestSlot == client.Player.ActiveWeapon)
                ManifestationManager.Instance.RefreshArmedWeapon(client, previousWeaponEntityId);
        }

        #endregion

        #region Helper Functions

        public void UpdateItemSlot(Client client, ulong entityId)
        {
            Item tempItem = EntityManager.Instance.GetItem(entityId);
            ItemManager.Instance.SendItemDataToClient(client, tempItem, false);
        }

        public void AddItemBySlot(Client client, InventoryType inventoryType, ulong entityId, uint slotId, bool updateDB, bool actuallyAdd = false)
        {
            var tempItem = EntityManager.Instance.GetItem(entityId);

            if (tempItem == null)
                return;

            // set entityId in slot
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    client.Player.Inventory.PersonalInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.HomeInventory:
                    client.Player.Inventory.HomeInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    client.Player.Inventory.EquippedInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    client.Player.Inventory.WeaponDrawer[(int)slotId] = tempItem.EntityId; // update slot
                    if (slotId == client.Player.ActiveWeapon)
                        client.Player.Inventory.EquippedInventory[13] = tempItem.EntityId;
                    break;
                case InventoryType.ClanInventory:
                    client.Player.Inventory.ClanInventory[(int)slotId] = tempItem.EntityId; // update slot
                    break;
                default:
                    Console.WriteLine("Unsuported inventory type");
                    break;
            }
            // send inventoryAddItem
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(inventoryType, tempItem.EntityId, slotId));
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            if (inventoryType == InventoryType.HomeInventory)
                tempItem.OwnerId = 0;
            else if (inventoryType == InventoryType.Personal || inventoryType == InventoryType.EquipedInventory ||
                inventoryType == InventoryType.WeaponDrawerInventory)
                tempItem.OwnerId = client.Player.Id;
            tempItem.OwnerSlotId = slotId;

            // update item in database
            if (updateDB)
            {
                if (inventoryType == InventoryType.ClanInventory)
                {
                    if (actuallyAdd)
                    {
                        unitOfWork.ClanInventories.AddInvItem(client.Player.ClanId, slotId, tempItem.Id);
                    }
                    else
                    {
                        unitOfWork.ClanInventories.MoveInvItem(client.Player.ClanId, slotId, tempItem.Id);
                    }
                }
                else
                {
                    if (actuallyAdd)
                    {
                        unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, tempItem.OwnerId, (uint)inventoryType, slotId, tempItem.Id);
                    }
                    else
                    {
                        unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, tempItem.OwnerId, (uint)inventoryType, slotId, tempItem.Id);
                    }
                }
            }
        }

        public Item AddItemToInventory(Client client, Item item)
        {
            if (item == null)
                return null;

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);

            // get item category offset
            var itemCategoryOffset = (int)item.ItemTemplate.InventoryCategory - 1;

            if (itemCategoryOffset < 0 || itemCategoryOffset >= 5)
            {
                Logger.WriteLog(LogType.Error, $"AddItemToInventory: ItemTemplateId = {item.ItemTemplate.ItemTemplateId} inventory category = {item.ItemTemplate.InventoryCategory} is invalid");
                return null;
            }

            itemCategoryOffset *= 50;
            var stackSizeChanged = false;
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            // see if we can merge the item into an already existing item
            for (var i = 0; i < 50; i++)
                if (client.Player.Inventory.PersonalInventory[itemCategoryOffset + i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(client.Player.Inventory.PersonalInventory[itemCategoryOffset + i]);

                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != item.ItemTemplate.ItemTemplateId)
                        continue;

                    // calculate how many items we can add to the stack
                    var stackAdd = itemClassInfo.StackSize - slotItem.StackSize;
                    if (stackAdd == 0)
                        continue;

                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, item.StackSize);
                    slotItem.StackSize += stackMove;
                    unitOfWork.Items.UpdateItemStackSize(slotItem);

                    // remove stack's from source item
                    item.StackSize -= stackMove;
                    stackSizeChanged = true;

                    // notify client of changed stack count
                    client.CallMethod(slotItem.EntityId, new SetStackCountPacket(slotItem.StackSize));

                    if (item.StackSize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                        // remove from DB
                        unitOfWork.Items.DeleteItem(item.Id);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
                client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));

            // find free slot
            for (var i = 0; i < 50; i++)
            {
                if (client.Player.Inventory.PersonalInventory[itemCategoryOffset + i] == 0)
                {
                    item.OwnerId = client.Player.Id;
                    item.OwnerSlotId = (uint)(itemCategoryOffset + i);
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(client, item, false);
                    // add item to empty slot
                    AddItemBySlot(client, InventoryType.Personal, item.EntityId, (uint)(itemCategoryOffset + i), true, true);
                    return item;
                }
            }

            return null;
        }

        public Item AddItemToClanInventory(Client client, Item item)
        {
            if (item == null)
                return null;

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var stackSizeChanged = false;
            // see if we can merge the item into an already existing item
            for (var i = 0; i < 500; i++)
                if (client.Player.Inventory.ClanInventory[i] != 0)
                {
                    // get item
                    var slotItem = EntityManager.Instance.GetItem(client.Player.Inventory.ClanInventory[i]);

                    // same item template?
                    if (slotItem.ItemTemplate.ItemTemplateId != item.ItemTemplate.ItemTemplateId)
                        continue;

                    // calculate how many items we can add to the stack
                    var stackAdd = itemClassInfo.StackSize - slotItem.StackSize;
                    if (stackAdd == 0)
                        continue;

                    // add item to existing stack
                    var stackMove = Math.Min(stackAdd, item.StackSize);
                    slotItem.StackSize += stackMove;
                    unitOfWork.Items.UpdateItemStackSize(slotItem);

                    // remove stack's from source item
                    item.StackSize -= stackMove;
                    stackSizeChanged = true;

                    // notify client of changed stack count
                    //client.CallMethod(slotItem.EntityId, new SetStackCountPacket(slotItem.Stacksize));
                    ClanManager.Instance.CallMethodForOnlineMembers(client.Player.ClanId, slotItem.EntityId, new SetStackCountPacket(slotItem.StackSize));

                    if (item.StackSize == 0)
                    {
                        // destroy the item
                        EntityManager.Instance.DestroyPhysicalEntity(client, item.EntityId, EntityType.Item);
                        // remove from DB
                        unitOfWork.Items.DeleteItem(item.Id);
                        // return the 'new' item instead
                        return slotItem;
                    }

                }

            // item have new stackSize?
            if (stackSizeChanged)
                client.CallMethod(item.EntityId, new SetStackCountPacket(item.StackSize));

            // find free slot
            for (var i = 0; i < 500; i++)
            {
                if (client.Player.Inventory.ClanInventory[i] == 0)
                {
                    item.OwnerId = client.AccountEntry.SelectedSlot;
                    item.OwnerSlotId = (uint)(i);
                    item.CurrentHitPoints = itemClassInfo.MaxHitPoints;
                    // send data to client
                    ItemManager.Instance.SendItemDataToClient(client, item, false);
                    // add item to empty slot
                    AddItemBySlot(client, InventoryType.ClanInventory, item.EntityId, (uint)(i), true, true);
                    return item;
                }
            }

            return null;
        }

        public Item CurrentWeapon(Client client)
        {
            return EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[13]);
        }

        public uint FreeSlotIndex(Manifestation player, InventoryType inventoryType, uint slotIndex)
        {
            switch (inventoryType)
            {
                case InventoryType.Personal:
                    player.Inventory.PersonalInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.HomeInventory:
                    player.Inventory.HomeInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.EquipedInventory:
                    player.Inventory.EquippedInventory[(int)slotIndex] = 0; // update slot
                    break;
                case InventoryType.WeaponDrawerInventory:
                    player.Inventory.WeaponDrawer[(int)slotIndex] = 0;    // update slot
                    break;
                default:
                    Console.WriteLine("RemoveItemBySlot: Invalid inventoryType{0}/slotIndex{1}\n", inventoryType, slotIndex);
                    break;
            }

            return slotIndex;
        }

        public void InitForClient(Client client)
        {
            InitCharacterInventory(client);

            // init LockboxTabPermissions
            client.CallMethod(SysEntity.ClientInventoryManagerId, new LockboxTabPermissionsPacket(client.Player.LockboxTabs));

            // it seems  that InventoryCreatePacket dont need to be called, ToDo; investigate more
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.Personal, client.MapClient.Inventory.PersonalInventory, 250));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.HomeInventory, client.MapClient.Inventory.HomeInventory, 480));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.WeaponDrawerInventory, client.MapClient.Inventory.WeaponDrawer, 5));
            //client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.EquipedInventory, client.MapClient.Inventory.EquippedInventory, 22));
        }

        public void SetupLocalClanInventory(Client client)
        {
            if (client.Player.ClanId == 0)
                return;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            List<ClanInventoryEntry> getClanInventoryData = unitOfWork.ClanInventories.GetItems(client.Player.ClanId);

            foreach (var item in getClanInventoryData)
            {
                var itemData = unitOfWork.Items.GetItem(item.ItemId);
                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                    return;

                Item tempItem = null;

                foreach (var entities in EntityManager.Instance.Items)
                {
                    Item existingItem = entities.Value;

                    if (existingItem.Id == item.ItemId)
                    {
                        tempItem = existingItem;
                    }
                }

                // check if item is weapon
                if (tempItem.ItemTemplate.WeaponInfo != null)
                    tempItem.CurrentAmmo = itemData.AmmoCount;

                // fill invenoty slot
                ItemManager.Instance.SendItemDataToClient(client, tempItem, false);

                AddItemBySlot(client, InventoryType.ClanInventory, tempItem.EntityId, tempItem.OwnerSlotId, false);
            }

            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryCreatePacket(InventoryType.ClanInventory, client.Player.Inventory.ClanInventory, 500));
        }

        public void InitClanInventory(Client client)
        {
            for (uint i = 0; i < 500; i++)
                client.Player.Inventory.ClanInventory.Add(0);

            SetupLocalClanInventory(client);
        }

        public void InitCharacterInventory(Client client)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var getInventoryData = unitOfWork.CharacterInventories.GetItems(client.AccountEntry.Id, client.Player.Id);

            // Map transfer retains this Manifestation, but the original client
            // removes all physical entities while loading the next map. Retire
            // each old item once (the active weapon also occupies slot 13), then
            // rebuild the fixed slot lists and publish the persisted inventory.
            var previousItems = new Dictionary<uint, Item>();
            var previousEntityIds = new HashSet<ulong>(client.Player.Inventory.PersonalInventory);
            previousEntityIds.UnionWith(client.Player.Inventory.HomeInventory);
            previousEntityIds.UnionWith(client.Player.Inventory.EquippedInventory);
            previousEntityIds.UnionWith(client.Player.Inventory.WeaponDrawer);
            foreach (var entityId in previousEntityIds)
            {
                var previousItem = EntityManager.Instance.GetItem(entityId);
                if (previousItem == null)
                    continue;
                previousItems[previousItem.Id] = previousItem;
                EntityManager.Instance.DestroyPhysicalEntity(client, entityId, EntityType.Item);
            }
            client.Player.Inventory.PersonalInventory.Clear();
            client.Player.Inventory.HomeInventory.Clear();
            client.Player.Inventory.EquippedInventory.Clear();
            client.Player.Inventory.WeaponDrawer.Clear();

            // init for server inventory
            for (uint i = 0; i < 22; i++)
                client.Player.Inventory.EquippedInventory.Add(0);

            for (uint i = 0; i < 480; i++)
                client.Player.Inventory.HomeInventory.Add(0);

            for (uint i = 0; i < 250; i++)
                client.Player.Inventory.PersonalInventory.Add(0);

            for (uint i = 0; i < 5; i++)
                client.Player.Inventory.WeaponDrawer.Add(0);

            foreach (var item in getInventoryData)
            {
                var inventoryType = (InventoryType)item.InventoryType;
                var slots = inventoryType switch
                {
                    InventoryType.Personal => client.Player.Inventory.PersonalInventory,
                    InventoryType.HomeInventory => client.Player.Inventory.HomeInventory,
                    InventoryType.EquipedInventory => client.Player.Inventory.EquippedInventory,
                    InventoryType.WeaponDrawerInventory => client.Player.Inventory.WeaponDrawer,
                    _ => null
                };
                if (slots == null || item.SlotId >= slots.Count || slots[(int)item.SlotId] != 0 ||
                    (inventoryType == InventoryType.EquipedInventory && item.SlotId == 13))
                {
                    Logger.WriteLog(LogType.Error, $"Invalid inventory location for item {item.ItemId}: type {item.InventoryType}, slot {item.SlotId}");
                    continue;
                }

                var itemData = unitOfWork.Items.GetItem(item.ItemId);
                if (itemData == null)
                {
                    Logger.WriteLog(LogType.Error, $"Missing item {item.ItemId} in character inventory");
                    continue;
                }
                var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                if (itemTemplate == null)
                    continue;

                var newItem = new Item
                {
                    OwnerId = item.CharacterId,
                    OwnerSlotId = item.SlotId,
                    ItemTemplate = itemTemplate,
                    ItemTemplateId = itemData.ItemTemplateId,
                    StackSize = itemData.StackSize,
                    CurrentHitPoints = itemData.CurrentHitPoints,
                    Color = itemData.Color,
                    Id = item.ItemId,
                    Crafter = itemData.CrafterName
                };
                // These existing instance fields have no database columns.
                // Rebuilding network entities must not reset them during travel.
                if (previousItems.TryGetValue(item.ItemId, out var previousItem))
                {
                    newItem.IsJammed = previousItem.IsJammed;
                    newItem.CammeraProfile = previousItem.CammeraProfile;
                }

                // check if item is weapon
                if (newItem.ItemTemplate.WeaponInfo != null)
                    newItem.CurrentAmmo = itemData.AmmoCount;

                // register item
                EntityManager.Instance.RegisterEntity(newItem.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(newItem.EntityId, newItem);

                // fill invenoty slot
                ItemManager.Instance.SendItemDataToClient(client, newItem, false);

                AddItemBySlot(client, inventoryType, newItem.EntityId, newItem.OwnerSlotId, false);
            }

            client.Player.Inventory.EquippedInventory[13] = client.Player.ActiveWeapon < client.Player.Inventory.WeaponDrawer.Count
                ? client.Player.Inventory.WeaponDrawer[client.Player.ActiveWeapon] : 0;
            // Character appearance is loaded independently and older drawer
            // changes could leave it stale. Reconcile before actor publication.
            var selectedWeapon = EntityManager.Instance.GetItem(client.Player.Inventory.EquippedInventory[13]);
            client.Player.AppearanceData.TryGetValue(EquipmentData.Weapon, out var weaponAppearance);
            if (selectedWeapon == null)
            {
                if (weaponAppearance != null)
                    weaponAppearance.Class = 0;
                client.Player.WeaponReady = false;
            }
            else
            {
                if (weaponAppearance == null)
                {
                    weaponAppearance = new AppearanceData { SlotId = EquipmentData.Weapon };
                    client.Player.AppearanceData.Add(EquipmentData.Weapon, weaponAppearance);
                }
                weaponAppearance.Class = (uint)selectedWeapon.ItemTemplate.Class;
                weaponAppearance.Color = new Color(selectedWeapon.Color);
                // Keep the existing loaded hue, or the existing appearance
                // loader fallback. The original second-hue persistence is unknown.
                weaponAppearance.Hue2 ??= new Color(2139062144);
            }
        }

        public bool ReduceStackCount(Client client, InventoryType inventoryType, Item tempItem, uint stackDecreaseCount)
        {
            if (client?.State != ClientState.Ingame || client.AccountEntry == null || client.Player == null ||
                client.Player.Disconected || client.Player.RemoveFromMap || tempItem == null || tempItem.Id == 0 ||
                !ReferenceEquals(EntityManager.Instance.GetItem(tempItem.EntityId), tempItem) ||
                stackDecreaseCount == 0 || stackDecreaseCount > tempItem.StackSize)
                return false;

            var slots = inventoryType switch
            {
                InventoryType.Personal => client.Player.Inventory.PersonalInventory,
                InventoryType.HomeInventory => client.Player.Inventory.HomeInventory,
                _ => null
            };
            var ownerId = inventoryType == InventoryType.HomeInventory ? 0u : client.Player.Id;
            var slotId = tempItem.OwnerSlotId;
            if (slots == null || tempItem.OwnerId != ownerId || slotId >= slots.Count ||
                slots[(int)slotId] != tempItem.EntityId)
                return false;

            var newStackCount = tempItem.StackSize - stackDecreaseCount;
            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                if (!unitOfWork.Items.TryConsumeItemStack(client.AccountEntry.Id, ownerId, (uint)inventoryType,
                    slotId, tempItem.Id, tempItem.StackSize, stackDecreaseCount))
                    return false;
            }
            catch (System.Data.Common.DbException exception)
            {
                Logger.WriteLog(LogType.Error, $"Item consumption failed for item {tempItem.Id}: {exception.Message}");
                return false;
            }

            tempItem.StackSize = newStackCount;
            if (newStackCount == 0)
            {
                FreeSlotIndex(client.Player, inventoryType, slotId);
                EntityManager.Instance.DestroyPhysicalEntity(client, tempItem.EntityId, EntityType.Item);
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(inventoryType, tempItem.EntityId));
            }
            else
                client.CallMethod(tempItem.EntityId, new SetStackCountPacket(newStackCount));
            return true;
        }

        public void RemoveItemBySlot(Client client, InventoryType inventoryType, uint slotIndex)
        {
            var entityId = 0ul;

            switch (inventoryType)
            {
                case InventoryType.Personal:
                    entityId = client.Player.Inventory.PersonalInventory[(int)slotIndex];
                    client.Player.Inventory.PersonalInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.HomeInventory:
                    entityId = client.Player.Inventory.HomeInventory[(int)slotIndex];
                    client.Player.Inventory.HomeInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.EquipedInventory:
                    entityId = client.Player.Inventory.EquippedInventory[(int)slotIndex];
                    client.Player.Inventory.EquippedInventory[(int)slotIndex] = 0;
                    break;
                case InventoryType.WeaponDrawerInventory:
                    entityId = client.Player.Inventory.WeaponDrawer[(int)slotIndex];
                    client.Player.Inventory.WeaponDrawer[(int)slotIndex] = 0;
                    if (slotIndex == client.Player.ActiveWeapon)
                        client.Player.Inventory.EquippedInventory[13] = 0;
                    break;
                case InventoryType.ClanInventory:
                    entityId = client.Player.Inventory.ClanInventory[(int)slotIndex];
                    client.Player.Inventory.ClanInventory[(int)slotIndex] = 0;
                    break;
                default:
                    Logger.WriteLog(LogType.Error, $"RemoveItemBySlot: Unsuported Inventory type {inventoryType}");
                    return;
            }

            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(inventoryType, entityId));
        }

        public void RequestTooltipForItemTemplateId(Client client, uint itemTemplateId)
        {

            var itemTemplate = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            var classInfo = EntityClassManager.Instance.GetClassInfo(itemTemplate.Class);

            if (itemTemplate == null)
            {
                Logger.WriteLog(LogType.Error, $"RequestTooltipForItemTemplateId: Unknown itemTemplateId {itemTemplateId}");
                return; // todo: even answer on a unknown template, else the client will continue to spam us with requests
            }
            client.CallMethod(SysEntity.ClientGameUIManagerId, new ItemTemplateTooltipInfoPacket(itemTemplate, classInfo));
        }

        public void RequestTooltipForModuleId(Client client, int moduleId)
        {
            Logger.WriteLog(LogType.Debug, $"ToDo: RequestTooltipForModuleId");
            //var moduleInfo = new ItemModule(moduleId, 1, new ModuleInfo(1, 1, 1, 1, 1, 1, 1, 1, 1));

            //client.SendPacket(12, new ModuleTooltipInfoPacket(moduleInfo));
        }

        public bool ValidateItemEquip(Client client, Item itemToEquip)
        {
            if (itemToEquip == null)
                return true;
            if (!TryGetEquipmentClass(itemToEquip, out var classInfo))
                return false;
            var failure = EquipmentRequirements.Check(client.Player, itemToEquip, classInfo.ItemClassInfo);
            if (failure == EquipmentRequirementFailure.None)
                return true;
            // Existing system-message transport; exact original localized failure
            // notifications are recorded separately from these eligibility rules.
            var message = failure switch
            {
                EquipmentRequirementFailure.MinimumLevel => "Level too low, cannot equip item.",
                EquipmentRequirementFailure.MaximumLevel => "Level too high, cannot equip item.",
                EquipmentRequirementFailure.Body => "Body attribute too low, cannot equip item.",
                EquipmentRequirementFailure.Mind => "Mind attribute too low, cannot equip item.",
                EquipmentRequirementFailure.Spirit => "Spirit attribute too low, cannot equip item.",
                EquipmentRequirementFailure.Race => "Item is not for your race, cannot equip it.",
                EquipmentRequirementFailure.Skill => "Skill level too low, cannot equip item.",
                _ => "Item is broken, cannot equip it."
            };
            CommunicatorManager.Instance.SystemMessage(client, message);
            return false;
        }

        public void RefreshClanLockbox(uint clanId, ulong entityId, uint characterId, uint slotId, ref List<ulong> clanInventory, bool addBySlot)
        {
            if (addBySlot)
                ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => AddItemBySlot(client, InventoryType.ClanInventory, entityId, slotId, false), characterId);

            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => UpdateItemSlot(client, entityId), characterId);
            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (uint)SysEntity.ClientInventoryManagerId, new ClanInventoryReload(InventoryType.ClanInventory, clanInventory, 500));
        }

        public void RemoveItemBySlotForClan(uint clanId, uint slotId, uint skipThisCharacter)
        {
            ClanManager.Instance.CallMethodForOnlineMembers(clanId, (client) => RemoveItemBySlot(client, InventoryType.ClanInventory, slotId), skipThisCharacter);
        }

        #endregion
    }
}
