using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterInventory
{
    using Context.Char;
    using Structures.Char;

    public class CharacterInventoryRepository : ICharacterInventoryRepository
    {
        private readonly CharContext _charContext;

        public CharacterInventoryRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void AddInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            var entry = new CharacterInventoryEntry(accountId, characterId, inventoryType, slotId, itemId);

            try
            {
                _charContext.CharacterInventoryEntries.Add(entry);
                _charContext.SaveChanges();
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating item:");
                Logger.WriteLog(LogType.Error, e);
            }
        }

        public void DeleteInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var entry = query.Where(e => e.AccountId == accountId && e.CharacterId == characterId && e.InventoryType == inventoryType && e.SlotId == slotId).FirstOrDefault();

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public List<CharacterInventoryEntry> GetItems(uint accountId, uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            // Personal (1), equipped (8) and weapon drawer (9) belong to a
            // character. Home storage (2) belongs to the account, with owner 0.
            return query.Where(e => e.AccountId == accountId &&
                    ((characterId != 0 && e.CharacterId == characterId &&
                        (e.InventoryType == 1 || e.InventoryType == 8 || e.InventoryType == 9)) ||
                     (e.CharacterId == 0 && e.InventoryType == 2)))
                .OrderBy(e => e.InventoryType).ThenBy(e => e.SlotId).ThenBy(e => e.ItemId)
                .ToList();
        }

        public void MoveInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterInventoryEntries);
            var invItem = query.FirstOrDefault(e => e.ItemId == itemId);

            invItem.AccountId = accountId;
            invItem.CharacterId = characterId;
            invItem.SlotId = slotId;
            invItem.InventoryType = inventoryType;

            _charContext.CharacterInventoryEntries.Update(invItem);
            _charContext.SaveChanges();
        }

        public bool TrySwapItems(uint accountId, uint characterId, uint sourceType, uint sourceSlot, uint sourceItemId,
            uint destinationType, uint destinationSlot, uint destinationItemId)
        {
            if (accountId == 0 || characterId == 0 || !IsSupportedSlot(sourceType, sourceSlot) ||
                !IsSupportedSlot(destinationType, destinationSlot) ||
                sourceType == destinationType && sourceSlot == destinationSlot || sourceItemId == destinationItemId)
                return false;

            using var transaction = _charContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var sourceOwner = sourceType == 2 ? 0u : characterId;
            var destinationOwner = destinationType == 2 ? 0u : characterId;
            var locations = _charContext.CharacterInventoryEntries.AsNoTracking().Where(e =>
                e.AccountId == accountId &&
                (e.CharacterId == sourceOwner && e.InventoryType == sourceType && e.SlotId == sourceSlot ||
                 e.CharacterId == destinationOwner && e.InventoryType == destinationType && e.SlotId == destinationSlot)).ToList();
            var source = locations.Where(e => e.InventoryType == sourceType && e.SlotId == sourceSlot).ToList();
            var destination = locations.Where(e => e.InventoryType == destinationType && e.SlotId == destinationSlot).ToList();
            if (sourceItemId == 0 ? source.Count != 0 : source.Count != 1 || source[0].ItemId != sourceItemId)
                return false;
            if (destinationItemId == 0 ? destination.Count != 0 : destination.Count != 1 || destination[0].ItemId != destinationItemId)
                return false;
            var expectedItems = (sourceItemId == 0 ? 0 : 1) + (destinationItemId == 0 ? 0 : 1);
            if (_charContext.ItemEntries.Count(e => e.ItemId != 0 && (e.ItemId == sourceItemId || e.ItemId == destinationItemId) &&
                    e.StackSize > 0) != expectedItems)
                return false;

            if (sourceItemId != 0 && _charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character_inventory SET character_id = {destinationOwner}, invenotry_type = {destinationType}, slot_id = {destinationSlot}
                WHERE item_id = {sourceItemId} AND account_id = {accountId} AND character_id = {sourceOwner}
                AND invenotry_type = {sourceType} AND slot_id = {sourceSlot}") != 1)
                return false;
            if (destinationItemId != 0 && _charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character_inventory SET character_id = {sourceOwner}, invenotry_type = {sourceType}, slot_id = {sourceSlot}
                WHERE item_id = {destinationItemId} AND account_id = {accountId} AND character_id = {destinationOwner}
                AND invenotry_type = {destinationType} AND slot_id = {destinationSlot}") != 1)
                return false;
            transaction.Commit();
            return true;
        }

        // Existing personal/equipped/drawer capacities; equipped slot 13 is the
        // selected drawer reference, never an independent persisted location.
        private static bool IsSupportedSlot(uint type, uint slot)
            => type == 1 && slot < 250 || type == 2 && slot < 480 || type == 8 && slot < 22 && slot != 13 || type == 9 && slot < 5;
    }
}
