using System;

namespace Rasa.Data
{
    /// <summary>
    /// What a vendor pays for an item and charges to repair it, as the 1.16.5.0 client computes and shows them
    /// (docs/retail-accuracy.md, "2026-09-26 - Client-contract defects"). The per-unit price itself is the server's
    /// itemtemplate sell_price; its provenance is GAP-W1-ITEM-PRICES / the Regenerate_item_template record,
    /// not this class.
    /// </summary>
    public static class VendorPriceRules
    {
        /// <summary>gameconstants REPAIR_GLOBAL_MODIFIER (shared/gameconstants.pyo store 1541-1544: 1.0).</summary>
        public const double RepairGlobalModifier = 1.0;

        /// <summary>
        /// The item-info buyback price (kItemIdx_BuybackPrice) the client is told for a template. The client
        /// uses that one number for two things: the sale price on an inventory item's tooltip while a vendor is
        /// open (inventorywindow.OnSlotEntered sets eTooltipAdd_Price; tooltipwindow line 908 shows
        /// stack count x gameuiutil.GetItemBuybackPrice) and the base of the repair price
        /// (vendorwindow._GetRepairPrice). Both are per unit what RequestVendorSale pays, so it is the sell price.
        /// A negative sell price is paid as nothing (RequestVendorSale clamps it), so it is shown as nothing.
        /// </summary>
        public static int BuybackPrice(int sellPrice) => Math.Max(sellPrice, 0);

        /// <summary>
        /// Equipable.GetCondition / item.GetCondition: 100 * currentHp / maxHp with Python 2 integer division
        /// (both are ints), and 100 when there is no maximum.
        /// </summary>
        public static int Condition(int currentHitPoints, int maxHitPoints)
            => maxHitPoints > 0 ? (int)Math.Floor((double)(100L * currentHitPoints) / maxHitPoints) : 100;

        /// <summary>
        /// vendorwindow._GetRepairPrice (lines 1081-1094):
        /// repairPrice = int(buyback * (100.0 - float(condition)) * 0.01); repairPrice *= REPAIR_GLOBAL_MODIFIER;
        /// return max(int(repairPrice), 1). An item at condition 100 or more is not listed for repair at all
        /// (_LoadVendorItems line 816), so the one-credit floor applies only to an item that needs repairing.
        /// </summary>
        public static int RepairPrice(int buybackPrice, int currentHitPoints, int maxHitPoints)
        {
            var condition = Condition(currentHitPoints, maxHitPoints);
            var price = (double)(int)(buybackPrice * (100.0 - condition) * 0.01);
            price *= RepairGlobalModifier;
            return Math.Max((int)price, 1);
        }
    }
}
