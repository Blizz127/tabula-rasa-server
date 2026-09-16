using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;

    /// <summary>
    /// The auction house's own data, which is the client's (client/auctionhouse.pyo): the four duration rows with the
    /// deposit each costs, and the 83 category rows the browse window filters by. Both are used as they are, so the
    /// tests are about the data being intact and being read correctly - the deposits the create window charges and the
    /// category an item's class name belongs to.
    /// </summary>
    [TestClass]
    public class AuctionHouseDataTests
    {
        [TestMethod]
        public void TheFourDurationsCarryTheClientsOwnDepositPercentages()
        {
            CollectionAssert.AreEqual(new uint[] { 5u, 10u, 20u, 25u },
                AuctionHouseData.Durations.Select(duration => duration.DepositPercent).ToArray());

            // The deposit is a percentage of the price, which is what the create window shows the seller.
            Assert.AreEqual(50u, AuctionHouseManagerDeposit(1000, 0));
            Assert.AreEqual(100u, AuctionHouseManagerDeposit(1000, 1));
            Assert.AreEqual(200u, AuctionHouseManagerDeposit(1000, 2));
            Assert.AreEqual(250u, AuctionHouseManagerDeposit(1000, 3));

            // Rounding is down, and a price the percentage cannot reach costs nothing.
            Assert.AreEqual(49u, AuctionHouseManagerDeposit(995, 0));
            Assert.AreEqual(0u, AuctionHouseManagerDeposit(5, 0));
        }

        private static uint AuctionHouseManagerDeposit(uint price, uint duration)
            => Rasa.Managers.AuctionHouseManager.DepositFor(price, duration);

        [TestMethod]
        public void AnItemsClassNamesPicksItsOwnCategory()
        {
            // The category names are the item class prefixes, and the longest match wins: a pistol is Weapon_Pistol,
            // not Weapon.
            var pistol = AuctionHouseData.Categories.Single(category => category.Name == "Weapon_Pistol");
            var weapon = AuctionHouseData.Categories.Single(category => category.Name == "Weapon");

            Assert.AreEqual(pistol.Bit, AuctionHouseData.CategoryOf("Weapon_Avatar_Pistol_Physical_UNC_01_to_04"));
            Assert.AreEqual(pistol.Bit, AuctionHouseData.CategoryOf("Weapon_Pistol"));

            // A name that is only the parent's stays in the parent rather than in a subcategory.
            Assert.AreEqual(weapon.Bit, AuctionHouseData.CategoryOf("Weapon_Any_Thing"));

            // Armour is categorised by family, and the family word wins over the slot word the class name also
            // carries: an item template's own name is "Armor_T1_MotorAssist_V01_CMN_Vest_01_to_02".
            var motorAssist = AuctionHouseData.Categories.Single(category => category.Name == "Armor_MotorAssist");
            Assert.AreEqual(motorAssist.Bit, AuctionHouseData.CategoryOf("Armor_T1_MotorAssist_V01_CMN_Vest_01_to_02"));

            // A name no category covers is "any category", which is the 0 the client sends.
            Assert.AreEqual(0u, AuctionHouseData.CategoryOf("MisWildernessItemThraxHeart"));
            Assert.AreEqual(0u, AuctionHouseData.CategoryOf(string.Empty));
            Assert.AreEqual(0u, AuctionHouseData.CategoryOf(null));
        }

        [TestMethod]
        public void TheCategoryTreeIsIntact()
        {
            // 83 rows, each with its own bit, and every parent that is not the root exists - a broken tree would show
            // the seller an item in a category the window cannot open.
            Assert.AreEqual(83, AuctionHouseData.Categories.Length);
            Assert.AreEqual(83, AuctionHouseData.Categories.Select(category => category.Bit).Distinct().Count());
            Assert.AreEqual(83, AuctionHouseData.Categories.Select(category => category.Id).Distinct().Count());

            // The tree is parent/leaf pairs: every two-word category's first word is a category of its own.
            foreach (var category in AuctionHouseData.Categories.Where(category => category.Name.Contains('_')))
                Assert.IsTrue(AuctionHouseData.Categories.Any(parent => parent.Name == category.Name.Split('_')[0]),
                    $"{category.Name}: no parent category");

            var byId = AuctionHouseData.Categories.ToDictionary(category => category.Id);
            var roots = 0;
            foreach (var category in AuctionHouseData.Categories)
            {
                if (category.ParentId == category.Id)
                    roots++;
                else
                    Assert.IsTrue(byId.ContainsKey(category.ParentId), $"{category.Name}: parent {category.ParentId} is missing");
            }

            // The client's tree has more than one top-level category; each root is its own parent.
            Assert.IsTrue(roots >= 1, "at least one root category");
        }
    }
}
