using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rasa.Structures.Char
{
    using Repositories.Char.Items;

    [Table(TableName)]
    public class ItemEntry
    {
        public const string TableName = "items";

        public ItemEntry()
        {
        }

        public ItemEntry(IItemChange item)
        {
            Color = item.Color;
            CrafterName = item.Crafter;
            CurrentHitPoints = item.CurrentHitPoints;
            ItemTemplateId = item.ItemTemplateId;
            StackSize = item.StackSize;
            AmmoCount = item.CurrentAmmo;
            LootModulesJson = ItemLootModules.Serialize(item.LootModules);
            TradableOverride = item.TradableOverride;
            SellableOverride = item.SellableOverride;
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        [Column("item_id")]
        [Required]
        public uint ItemId { get; set; }

        [Column("item_template_id")]
        [Required]
        public uint ItemTemplateId { get; set; }

        [Column("stack_size")]
        [Required]
        public uint StackSize { get; set; }

        [Column("current_hp")]
        [Required]
        public int CurrentHitPoints { get; set; }

        [Column("color")]
        [Required]
        public uint Color { get; set; }

        [Column("ammo_count")]
        [Required]
        public uint AmmoCount { get; set; }

        [Column("crafter_name", TypeName = "varchar(64)")]
        [Required]
        public string CrafterName { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; }

        [Column("loot_modules", TypeName = "TEXT")]
        public string LootModulesJson { get; set; }

        [Column("tradable_override")]
        public bool? TradableOverride { get; set; }

        [Column("sellable_override")]
        public bool? SellableOverride { get; set; }
    }
}
