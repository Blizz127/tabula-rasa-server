using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterInventory
{
    using Structures.Char;
    public interface ICharacterInventoryRepository
    {
        void AddInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
        // Staged: committed by the unit of work; failures propagate.
        void StageInvItem(uint accountId, uint characterId, uint inventoryType, uint slotId, uint itemId);
        void DeleteInvItem(uint accountId, uint characteId, uint inventoryType, uint slotIndex);
        void DeleteInvItemByItemId(uint itemId);
        List<CharacterInventoryEntry> GetItems(uint accountId, uint characterId);
        List<CharacterInventoryEntry> GetItems(uint accountId);
        void MoveInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
        bool TrySwapItems(uint accountId, uint characterId, uint sourceType, uint sourceSlot, uint sourceItemId,
            uint destinationType, uint destinationSlot, uint destinationItemId,
            uint? expectedSourceStack = null, uint? expectedDestinationStack = null, int? expectedHomeTabs = null);
        ItemEntry TrySplitItemStack(uint accountId, uint characterId, uint sourceType, uint sourceSlot, uint sourceItemId,
            uint expectedStack, uint expectedTemplateId, uint destinationType, uint destinationSlot, uint quantity, int? expectedHomeTabs);
    }
}
