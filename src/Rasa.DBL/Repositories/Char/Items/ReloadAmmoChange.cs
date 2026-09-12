namespace Rasa.Repositories.Char.Items
{
    // Expected personal-inventory state for one atomic magazine transfer.
    public sealed class ReloadAmmoChange
    {
        public uint ItemId { get; }
        public uint SlotId { get; }
        public uint StackBefore { get; }
        public uint Consumed { get; }

        public ReloadAmmoChange(uint itemId, uint slotId, uint stackBefore, uint consumed)
        {
            ItemId = itemId;
            SlotId = slotId;
            StackBefore = stackBefore;
            Consumed = consumed;
        }
    }
}
