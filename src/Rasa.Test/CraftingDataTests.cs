using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;

    /// <summary>
    /// The Kraftwerks' recipes, which are the client's own (shared/crafting.pyo recipeItemTemplateTable). The station
    /// makes what these say, so the tests are about the data being complete and being read the way the client wrote it -
    /// including the one recipe whose numbers can be checked against the client's own item table.
    /// </summary>
    [TestClass]
    public class CraftingDataTests
    {
        [TestMethod]
        public void TheClientsRecipesAreAllPresentAndSelfConsistent()
        {
            Assert.AreEqual(160, CraftingData.Recipes.Length);
            Assert.IsTrue(CraftingData.IsSelfConsistent());

            // Every schematic is reachable by its own template id.
            foreach (var recipe in CraftingData.Recipes)
                Assert.AreSame(recipe, CraftingData.ForTemplate(recipe.TemplateId), $"template {recipe.TemplateId}");

            Assert.IsNull(CraftingData.ForTemplate(0));
            Assert.IsNull(CraftingData.ForTemplate(uint.MaxValue));
        }

        [TestMethod]
        public void TheSimplestRecipeIsTheOneTheClientSpellsOut()
        {
            // Schematic 641 is the cartridges: 500 of item 1765 in, 500 of item 28 out (Standard Grade Cartridges),
            // five seconds at the station and no energy cost, for a level 1 character. Its numbers are a cross-check of
            // the whole chain - the client table, the decoder and this data class.
            var recipe = CraftingData.ForTemplate(641u);

            Assert.IsNotNull(recipe);
            Assert.AreEqual(0u, recipe.EnergyUnitCost);
            Assert.AreEqual(5u, recipe.KraftwerksTimeSeconds);
            Assert.AreEqual(28u, recipe.ResultItemTemplateId);
            Assert.AreEqual(500u, recipe.ResultItemAmount);
            Assert.AreEqual(1u, recipe.MinimumPlayerLevel);
            Assert.AreEqual(1, recipe.Inputs.Length);
            Assert.AreEqual(1765u, recipe.Inputs[0].ItemTemplateId);
            Assert.AreEqual(500u, recipe.Inputs[0].Quantity);
        }

        [TestMethod]
        public void TheRecipesFollowTheLevelBandsAndTakeRealTime()
        {
            // The minimum levels are the game's own band starts (1, 6, 11, ... every five levels), and no recipe is
            // instant: the station's time is what the client counts down.
            CollectionAssert.AreEqual(new uint[] { 1u, 6u, 11u, 16u, 21u, 26u, 31u, 36u, 41u, 46u },
                CraftingData.Recipes.Select(recipe => recipe.MinimumPlayerLevel).Distinct().OrderBy(level => level).ToArray());

            Assert.IsTrue(CraftingData.Recipes.All(recipe => recipe.KraftwerksTimeSeconds >= 2 && recipe.KraftwerksTimeSeconds <= 14));
        }
    }
}
