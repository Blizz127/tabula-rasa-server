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

        public void StageInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId)
        {
            _charContext.CharacterInventoryEntries.Add(new CharacterInventoryEntry(accountId, characterId, inventoryType, slotId, itemId));
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
            uint destinationType, uint destinationSlot, uint destinationItemId,
            uint? expectedSourceStack = null, uint? expectedDestinationStack = null, int? expectedHomeTabs = null)
        {
            if (accountId == 0 || characterId == 0 || !IsSupportedSlot(sourceType, sourceSlot) ||
                !IsSupportedSlot(destinationType, destinationSlot) ||
                sourceType == destinationType && sourceSlot == destinationSlot || sourceItemId == destinationItemId)
                return false;

            using var transaction = _charContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            if (!MatchesHomeTabs(accountId, sourceType, sourceSlot, destinationType, destinationSlot, expectedHomeTabs))
                return false;
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
            if (sourceItemId != 0 && expectedSourceStack.HasValue && !_charContext.ItemEntries.Any(e =>
                    e.ItemId == sourceItemId && e.StackSize == expectedSourceStack.Value) ||
                destinationItemId != 0 && expectedDestinationStack.HasValue && !_charContext.ItemEntries.Any(e =>
                    e.ItemId == destinationItemId && e.StackSize == expectedDestinationStack.Value))
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

        public ItemEntry TrySplitItemStack(uint accountId, uint characterId, uint sourceType, uint sourceSlot, uint sourceItemId,
            uint expectedStack, uint expectedTemplateId, uint destinationType, uint destinationSlot, uint quantity, int? expectedHomeTabs)
        {
            if (accountId == 0 || characterId == 0 || sourceItemId == 0 || quantity == 0 || quantity >= expectedStack ||
                sourceType != 1 && sourceType != 2 || destinationType != 1 && destinationType != 2 ||
                !IsSupportedSlot(sourceType, sourceSlot) || !IsSupportedSlot(destinationType, destinationSlot) ||
                sourceType == destinationType && sourceSlot == destinationSlot)
                return null;
            using var transaction = _charContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            if (!MatchesHomeTabs(accountId, sourceType, sourceSlot, destinationType, destinationSlot, expectedHomeTabs))
                return null;
            var sourceOwner = sourceType == 2 ? 0u : characterId;
            var destinationOwner = destinationType == 2 ? 0u : characterId;
            var sourceRows = _charContext.CharacterInventoryEntries.AsNoTracking().Where(e => e.AccountId == accountId &&
                e.CharacterId == sourceOwner && e.InventoryType == sourceType && e.SlotId == sourceSlot).ToList();
            if (sourceRows.Count != 1 || sourceRows[0].ItemId != sourceItemId ||
                _charContext.CharacterInventoryEntries.Any(e => e.AccountId == accountId && e.CharacterId == destinationOwner &&
                    e.InventoryType == destinationType && e.SlotId == destinationSlot))
                return null;
            var source = _charContext.ItemEntries.AsNoTracking().SingleOrDefault(e => e.ItemId == sourceItemId && e.StackSize == expectedStack && e.ItemTemplateId == expectedTemplateId);
            if (source == null)
                return null;
            if (_charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE items SET stack_size = {expectedStack - quantity}
                WHERE item_id = {sourceItemId} AND stack_size = {expectedStack}") != 1)
                return null;
            // Copy persisted instance attributes; a split is not a new template reward.
            var split = new ItemEntry
            {
                ItemTemplateId = source.ItemTemplateId, StackSize = quantity,
                CurrentHitPoints = source.CurrentHitPoints, Color = source.Color,
                AmmoCount = source.AmmoCount, CrafterName = source.CrafterName, CreatedAt = source.CreatedAt
            };
            _charContext.ItemEntries.Add(split);
            _charContext.SaveChanges();
            _charContext.CharacterInventoryEntries.Add(new CharacterInventoryEntry(accountId, destinationOwner,
                destinationType, destinationSlot, split.ItemId));
            _charContext.SaveChanges();
            transaction.Commit();
            return split;
        }

        private bool MatchesHomeTabs(uint accountId, uint sourceType, uint sourceSlot, uint destinationType,
            uint destinationSlot, int? expectedTabs)
        {
            if (!expectedTabs.HasValue || sourceType != 2 && destinationType != 2)
                return true;
            var tabs = expectedTabs.Value;
            return tabs >= 1 && tabs <= 5 && (sourceType != 2 || sourceSlot < tabs * 96) &&
                (destinationType != 2 || destinationSlot < tabs * 96) &&
                _charContext.CharacterLockboxEntries.Any(e => e.AccountId == accountId && e.PurashedTabs == tabs);
        }

        // Existing personal/equipped/drawer capacities; equipped slot 13 is the
        // selected drawer reference, never an independent persisted location.
        private static bool IsSupportedSlot(uint type, uint slot)
            => type == 1 && slot < 250 || type == 2 && slot < 480 || type == 8 && slot < 22 && slot != 13 || type == 9 && slot < 5;
    }
}
