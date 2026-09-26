using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;

namespace Rasa.Test
{
    /// <summary>
    /// VendorPriceRules against the 1.16.5.0 client's own arithmetic: vendorwindow._GetRepairPrice (lines 1081-1094),
    /// item.GetCondition (line 254-260, Python 2 integer division) and gameconstants REPAIR_GLOBAL_MODIFIER 1.0.
    /// Each expected value is the client expression evaluated by hand.
    /// </summary>
    [TestClass]
    public class VendorPriceRulesTests
    {
        [DataTestMethod]
        // buyback, current, max, expected: max(int(int(buyback * (100.0 - cond) * 0.01) * 1.0), 1), cond = 100*cur//max
        [DataRow(101, 37, 160, 77)]     // cond 3700//160 = 23; 101 * 77 * 0.01 = 77.77 -> 77
        [DataRow(103, 82, 100, 18)]     // cond 82; 18.54 -> 18 (a rounding charge would take 19)
        [DataRow(1000, 0, 250, 1000)]   // cond 0: the full buyback price
        [DataRow(101, 159, 160, 1)]     // cond 99; 1.01 -> 1
        [DataRow(40, 159, 160, 1)]      // cond 99; 0.4 -> 0, floored to one credit
        [DataRow(0, 10, 100, 1)]        // no buyback price: one credit, which is what the client showed for every repair
        [DataRow(26, 1, 3, 17)]         // cond 100//3 = 33; 26 * 67 * 0.01 = 17.42 -> 17
        public void RepairPriceIsTheClientsGetRepairPrice(int buyback, int current, int max, int expected)
            => Assert.AreEqual(expected, VendorPriceRules.RepairPrice(buyback, current, max));

        [DataTestMethod]
        [DataRow(82, 100, 82)]
        [DataRow(1, 3, 33)]
        [DataRow(2, 3, 66)]
        [DataRow(159, 160, 99)]
        [DataRow(5, 0, 100)]            // no maximum: GetCondition returns 100
        public void ConditionUsesPythonTwoIntegerDivision(int current, int max, int expected)
            => Assert.AreEqual(expected, VendorPriceRules.Condition(current, max));

        [TestMethod]
        public void TheBuybackPriceIsTheSellPriceAndNeverNegative()
        {
            Assert.AreEqual(26, VendorPriceRules.BuybackPrice(26));
            Assert.AreEqual(0, VendorPriceRules.BuybackPrice(0));
            Assert.AreEqual(0, VendorPriceRules.BuybackPrice(-5));
            Assert.AreEqual(1.0, VendorPriceRules.RepairGlobalModifier);
        }
    }
}
