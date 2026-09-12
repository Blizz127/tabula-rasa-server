using System;
using System.Collections.Generic;

namespace Rasa.Structures
{
    public sealed class WeaponReloadPlan
    {
        public sealed class StackTransfer
        {
            public Item Item { get; }
            public uint SlotId { get; }
            public uint StackBefore { get; }
            public uint Consumed { get; }

            public StackTransfer(Item item, uint slotId, uint consumed)
            {
                Item = item;
                SlotId = slotId;
                StackBefore = item.StackSize;
                Consumed = consumed;
            }
        }

        public uint AmmoBefore { get; }
        public uint AmmoAfter { get; }
        public IReadOnlyList<StackTransfer> Transfers { get; }

        public WeaponReloadPlan(Item weapon, uint clipSize, IEnumerable<(uint SlotId, Item Item)> ammunition)
        {
            AmmoBefore = weapon.CurrentAmmo;
            var loaded = AmmoBefore;
            var transfers = new List<StackTransfer>();
            var seen = new HashSet<ulong>();
            foreach (var (slot, item) in ammunition)
            {
                if (loaded >= clipSize)
                    break;
                if (item == null || item.StackSize == 0 || !seen.Add(item.EntityId))
                    continue;
                var consumed = Math.Min(clipSize - loaded, item.StackSize);
                transfers.Add(new StackTransfer(item, slot, consumed));
                loaded += consumed;
            }
            AmmoAfter = loaded;
            Transfers = transfers.AsReadOnly();
        }
    }
}
