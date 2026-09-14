using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.Items
{
    using Context.Char;
    using Structures.Char;
    using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

    public class ItemRepository : IItemRepository
    {
        private readonly CharContext _charContext;

        public ItemRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public uint CreateItem(IItemChange item)
        {
            var entry = new ItemEntry(item);

            try
            {
                _charContext.ItemEntries.Add(entry);
                _charContext.SaveChanges();
                return entry.ItemId;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating item:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }
        }

        public void DeleteItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == itemId).FirstOrDefault();

            if (entry == null)
                return;

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        /// <summary>
        /// The tracked row for one item, or null: only changed columns are written, and a
        /// missing row is logged rather than dereferenced.
        /// </summary>
        private ItemEntry GetWritable(uint itemId)
        {
            var entry = _charContext.CreateTrackingQuery(_charContext.ItemEntries).FirstOrDefault(e => e.ItemId == itemId);

            if (entry == null)
                Logger.WriteLog(LogType.Error, $"Item {itemId} does not exist; update skipped.");

            return entry;
        }

        public ItemEntry GetItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var item = query.FirstOrDefault(e => e.ItemId == itemId);

            return item;

        }

        public void UpdateAmmo(IItemChange item)
        {
            var entry = GetWritable(item.Id);

            if (entry == null)
                return;

            entry.AmmoCount = item.CurrentAmmo;
            _charContext.SaveChanges();
        }

        public void UpdateCurrentHitPoints(IItemChange item)
        {
            var entry = GetWritable(item.Id);

            if (entry == null)
                return;

            entry.CurrentHitPoints = item.CurrentHitPoints;
            _charContext.SaveChanges();
        }

        public bool TrySpendWeaponAmmo(uint accountId, uint characterId, uint weaponId, uint ammoBefore, uint amount)
        {
            if (amount == 0 || amount > ammoBefore)
                return false;
            var remaining = ammoBefore - amount;
            return _charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE items SET ammo_count = {remaining}
                WHERE item_id = {weaponId} AND ammo_count = {ammoBefore}
                AND EXISTS (SELECT 1 FROM character_inventory
                    WHERE item_id = {weaponId} AND account_id = {accountId}
                    AND character_id = {characterId} AND invenotry_type = 9)") == 1;
        }

        public void UpdateItemStackSize(IItemChange item)
        {
            var entry = GetWritable(item.Id);

            if (entry == null)
                return;

            entry.StackSize = item.StackSize;
            _charContext.SaveChanges();
        }

        public bool TryConsumeItemStack(uint accountId, uint characterId, uint inventoryType, uint slotId,
            uint itemId, uint stackBefore, uint amount)
        {
            // Personal inventory belongs to a character; home storage uses owner 0.
            if (itemId == 0 || amount == 0 || amount > stackBefore ||
                !((inventoryType == 1 && characterId != 0 && slotId < 250) ||
                  (inventoryType == 2 && characterId == 0 && slotId < 480)))
                return false;

            var remaining = stackBefore - amount;
            using var transaction = _charContext.Database.BeginTransaction();
            var changed = _charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE items SET stack_size = {remaining}
                WHERE item_id = {itemId} AND stack_size = {stackBefore}
                AND EXISTS (SELECT 1 FROM character_inventory
                    WHERE item_id = {itemId} AND account_id = {accountId}
                    AND character_id = {characterId} AND invenotry_type = {inventoryType}
                    AND slot_id = {slotId})");
            if (changed != 1)
                return false;

            if (remaining == 0)
            {
                var removed = _charContext.Database.ExecuteSqlInterpolated($@"
                    DELETE FROM character_inventory WHERE item_id = {itemId}
                    AND account_id = {accountId} AND character_id = {characterId}
                    AND invenotry_type = {inventoryType} AND slot_id = {slotId}");
                if (removed != 1)
                    return false;
            }
            transaction.Commit();
            return true;
        }

        public bool TryReloadWeapon(uint accountId, uint characterId, uint weaponId, uint ammoBefore,
            uint ammoAfter, IReadOnlyList<ReloadAmmoChange> ammunition)
        {
            if (ammunition == null || ammunition.Count == 0 || ammoAfter <= ammoBefore ||
                ammunition.Any(a => a == null || a.ItemId == weaponId || a.Consumed == 0 ||
                    a.Consumed > a.StackBefore || a.SlotId < 50 || a.SlotId >= 100) ||
                ammunition.Select(a => a.ItemId).Distinct().Count() != ammunition.Count ||
                ammunition.Select(a => a.SlotId).Distinct().Count() != ammunition.Count ||
                ammunition.Sum(a => (long)a.Consumed) != (long)ammoAfter - ammoBefore)
                return false;

            // Conditional writes keep stale in-memory counts from creating ammunition.
            // The magazine, every stack and emptied inventory links commit together.
            using var transaction = _charContext.Database.BeginTransaction();
            var weaponChanged = _charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE items SET ammo_count = {ammoAfter}
                WHERE item_id = {weaponId} AND ammo_count = {ammoBefore}
                AND EXISTS (SELECT 1 FROM character_inventory
                    WHERE item_id = {weaponId} AND account_id = {accountId}
                    AND character_id = {characterId} AND invenotry_type = 9)");
            if (weaponChanged != 1)
                return false;

            foreach (var ammo in ammunition)
            {
                var remaining = ammo.StackBefore - ammo.Consumed;
                var changed = _charContext.Database.ExecuteSqlInterpolated($@"
                    UPDATE items SET stack_size = {remaining}
                    WHERE item_id = {ammo.ItemId} AND stack_size = {ammo.StackBefore}
                    AND EXISTS (SELECT 1 FROM character_inventory
                        WHERE item_id = {ammo.ItemId} AND account_id = {accountId}
                        AND character_id = {characterId} AND invenotry_type = 1
                        AND slot_id = {ammo.SlotId})");
                if (changed != 1)
                    return false;

                if (remaining == 0)
                {
                    var removed = _charContext.Database.ExecuteSqlInterpolated($@"
                        DELETE FROM character_inventory WHERE item_id = {ammo.ItemId}
                        AND account_id = {accountId} AND character_id = {characterId}
                        AND invenotry_type = 1 AND slot_id = {ammo.SlotId}");
                    if (removed != 1)
                        return false;
                }
            }

            transaction.Commit();
            return true;
        }
    }
}
