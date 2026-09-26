using Rasa.Structures.Char;

namespace Rasa.Repositories.Char.Items
{
    public interface IItemRepository
    {
        uint CreateItem(IItemChange item);
        void DeleteItem(uint itemId);
        ItemEntry GetItem(uint itemId);
        void UpdateAmmo(IItemChange item);
        bool TrySpendWeaponAmmo(uint accountId, uint characterId, uint weaponId, uint ammoBefore, uint amount);
        void UpdateCurrentHitPoints(IItemChange item);
        void UpdateItemStackSize(IItemChange item);
        void UpdateInstanceMetadata(IItemChange item);
        bool TryConsumeItemStack(uint accountId, uint characterId, uint inventoryType, uint slotId,
            uint itemId, uint stackBefore, uint amount);
        // Caller owns the transaction; used when a mission turn-in consumes items and pays rewards atomically.
        bool StageConsumeItemStack(uint accountId, uint characterId, uint inventoryType, uint slotId,
            uint itemId, uint stackBefore, uint amount);
        bool TryReloadWeapon(uint accountId, uint characterId, uint weaponId, uint ammoBefore,
            uint ammoAfter, System.Collections.Generic.IReadOnlyList<ReloadAmmoChange> ammunition);
    }
}
