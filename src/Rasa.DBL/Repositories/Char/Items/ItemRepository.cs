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

            _charContext.Remove(entry);
            _charContext.SaveChanges();
        }

        public ItemEntry GetItem(uint itemId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var item = query.FirstOrDefault(e => e.ItemId == itemId);

            return item;

        }

        public void UpdateAmmo(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.AmmoCount = item.CurrentAmmo;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCurrentHitPoints(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.CurrentHitPoints = item.CurrentHitPoints;

            _charContext.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateItemStackSize(IItemChange item)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.ItemEntries);
            var entry = query.Where(e => e.ItemId == item.Id).FirstOrDefault();

            entry.StackSize = item.StackSize;

            _charContext.Update(entry);
            _charContext.SaveChanges();
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
