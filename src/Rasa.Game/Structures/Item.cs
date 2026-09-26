namespace Rasa.Structures
{
    using Managers;
    using Repositories.Char.Items;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Item : IItemChange
    {
        public Item()
        {
            EntityId = EntityManager.Instance.GetEntityId;
        }

        public Item(uint itemTemplateId, uint stackSize, int currentHitPoints, uint color)
        {
            ItemTemplateId = itemTemplateId;
            StackSize = stackSize;
            CurrentHitPoints = currentHitPoints;
            Crafter = "";
            Color = color;
        }

        public ulong EntityId { get; }
        public ItemTemplate ItemTemplate { get; set; }
        public uint ItemTemplateId { get; set; }
        // uniqe id stored in db
        public uint Id { get; set; }
        // location info
        public uint OwnerId { get; set; }
        public uint OwnerSlotId { get; set; }
        // item instance specific
        public uint Color { get; set; }
        public string Crafter { get; set; }
        public int CurrentHitPoints { get; set; }
        public uint StackSize { get; set; }
        private IReadOnlyList<ItemLootModule> _lootModules = Array.Empty<ItemLootModule>();
        public IReadOnlyList<ItemLootModule> LootModules
        {
            get => _lootModules;
            set => _lootModules = ItemLootModules.Copy(value);
        }
        public bool? TradableOverride { get; set; }
        public bool? SellableOverride { get; set; }
        public bool IsTradable => TradableOverride ?? ItemTemplate?.ItemInfo?.Tradable ?? false;
        public bool IsSellable => SellableOverride ?? ItemTemplate?.HasSellableFlag ?? false;

        // Distinct instance restrictions/modules must never disappear through stacking.
        public bool HasSameInstanceMetadata(Item other) => other != null &&
            TradableOverride == other.TradableOverride && SellableOverride == other.SellableOverride &&
            LootModules.SequenceEqual(other.LootModules);

        public void RestoreInstanceMetadata(Char.ItemEntry entry)
        {
            LootModules = ItemLootModules.Deserialize(entry.LootModulesJson);
            TradableOverride = entry.TradableOverride;
            SellableOverride = entry.SellableOverride;
        }
        // weapon specific
        public uint CurrentAmmo { get; set; }
        public bool IsJammed { get; set; }
        public int CammeraProfile { get; set; }
    }
}
