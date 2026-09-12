using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterInventory
{
    using Structures.Char;
    public interface ICharacterInventoryRepository
    {
        void AddInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
        void DeleteInvItem(uint accountId, uint characteId, uint inventoryType, uint slotIndex);
        List<CharacterInventoryEntry> GetItems(uint accountId, uint characterId);
        void MoveInvItem(uint accountId, uint characteId, uint inventoryType, uint slotId, uint itemId);
        bool TrySwapItems(uint accountId, uint characterId, uint sourceType, uint sourceSlot, uint sourceItemId,
            uint destinationType, uint destinationSlot, uint destinationItemId);
    }
}
