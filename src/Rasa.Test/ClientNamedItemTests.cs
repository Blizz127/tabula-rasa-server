using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Migrations.WildernessData;
    using Rasa.Structures;

    /// <summary>
    /// Whether a thing reaches the player under the name the client has for it.
    ///
    /// Only the client has display names. <c>generated/client/language/english/
    /// physicalentityclassnamelanguage.pyo</c> is the table
    /// <c>clientlanguagemanager.GetEntityClassName</c> reads, and there is no copy of it on this server: the
    /// world database holds <c>entityclass.class_name</c>, which is the internal name out of
    /// <c>entityclass.pyo</c> ("Ammo_Nucleotides_1_Pyrimidines"), never the name a player sees ("Pyramidine
    /// Nucleotides"). So a server-built sentence can never name an item, and every message that needs to
    /// name one has to be the client's own - the lesson of "You received 30 3147", which
    /// <see cref="KillRewardTests"/> covers at the packet.
    ///
    /// These tests hold the two places the same mistake was still live on 2026-09-21: the crafting station's
    /// refusals, and the fifteen entity classes this server had invented a name for.
    /// </summary>
    [TestClass]
    public class ClientNamedItemTests
    {
        /// <summary>
        /// The four crafting messages the client ships, by id, out of <c>generated/client/playermessage.pyo</c>
        /// with the English text from <c>language/english/playermessagelanguage.pyo</c>. Three of them are
        /// posted by no client module in the whole 1.16.5.0 script tree, which is what marks them as the ones
        /// the retail server sent; the fourth, PM_CRAFTING_SUCCESS, the old crafting window also posts.
        /// </summary>
        [TestMethod]
        public void TheCraftingMessagesAreTheClientsOwnIds()
        {
            Assert.AreEqual(236, (int)PlayerMessage.PmNoRecipesToCraft);   // "You do not have any recipes that can currently be used to craft items"
            Assert.AreEqual(237, (int)PlayerMessage.PmNoRoomInOven);       // "This crafting facility cannot accept any more items to craft until you claim previous items."
            Assert.AreEqual(239, (int)PlayerMessage.PmCraftingSuccess);    // "Crafting success!"
            Assert.AreEqual(240, (int)PlayerMessage.PmNoRecipesInOven);    // "You do not have any items being crafted by this facility"
        }

        /// <summary>
        /// The station may not write an item's name, because it has none to write. The shortfall branch used
        /// to send "You need 50 Ammo_Nucleotides_1_Pyrimidines and have 2."; it now sends
        /// PM_NO_RECIPES_TO_CRAFT, which is the state the client's own shared/crafting.py
        /// GetValidRecipeIdList puts the recipe in when CanManifestationUseRecipeNow refuses it.
        ///
        /// Read off the source, because the check is that no route from a class to a word exists here at all,
        /// not that one particular call was changed.
        /// </summary>
        [TestMethod]
        public void TheCraftingStationNeverNamesAnItemItself()
        {
            var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Rasa.Game", "Managers", "KraftwerksManager.cs"));

            StringAssert.Contains(source, "PlayerMessage.PmNoRecipesToCraft", "the shortfall is told in the client's words");
            StringAssert.Contains(source, "PlayerMessage.PmNoRoomInOven", "a full station is told in the client's words");

            Assert.IsFalse(source.Contains(".ClassName"),
                "the crafting path reads an entity class's internal name, which is not a name the client would show");
            Assert.IsFalse(source.Contains("NameOfClass"),
                "the helper that turned a class id into a word is gone; only the client can do that");

            // The sentences the station may still write are about this server, not about the game, and name
            // nothing: the Crafting v2 pages it has not built.
            var written = System.Text.RegularExpressions.Regex.Matches(source, @"Fail\(client, station, \$?""([^""]*)""");
            CollectionAssert.AreEqual(
                new[] { "Only fabrication is available on this server yet." },
                written.Select(match => match.Groups[1].Value).ToArray());
        }

        /// <summary>
        /// The fifteen classes the client's entityclass table has no row for, which the seed had labelled
        /// Missing_ItemClassId_N/15. Every replacement is the client's own display name; the seed and the
        /// migration agree, so a database built today and one migrated from yesterday read the same.
        /// </summary>
        [TestMethod]
        public void TheFifteenUnnamedClassesTakeTheClientsNames()
        {
            Assert.AreEqual(15, ClientNamesForItemsRows.Rows.Length);

            var expected = new Dictionary<uint, string>
            {
                { 3180, "Rifle Ammo" },
                { 3600, "Combat Suit Paint Tier 3 Soldier" },
                { 3734, "Melanotan for Caucasians" },
                { 3741, "Combat Suit Paint Tier 3 Specialist" },
                { 3743, "Combat Suit Paint Tier 2 Soldier" },
                { 3744, "Combat Suit Paint Tier 2 Specialist" },
                { 3745, "Combat Suit Paint Tier 4 Soldier" },
                { 3746, "Combat Suit Paint Tier 4 Specialist" },
                { 4086, "Melanotan for Africans" },
                { 4327, "Botany Kit" },
                { 6251, "Your favorite text sucks." },
                { 6331, "RCH_TestRecord has no display text" },
                { 20759, "New record for rholtrop" },
                { 20781, "New record for rholtrop" },
                { 20987, "RCH_ThisBeTestDataHere has no display text." }
            };

            CollectionAssert.AreEqual(expected.Keys.OrderBy(id => id).ToArray(),
                ClientNamesForItemsRows.Rows.Select(row => row.Id).OrderBy(id => id).ToArray());

            var seed = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Rasa.DBL", "Services", "Preloader", "EntityClassPreloader.cs"));

            Assert.IsFalse(seed.Contains("Missing_ItemClassId"),
                "the seed no longer invents a name for a class the client's entityclass table lacks");

            foreach (var row in ClientNamesForItemsRows.Rows)
            {
                Assert.AreEqual(expected[row.Id], row.Client, $"class {row.Id}");
                StringAssert.StartsWith(row.Was, "Missing_ItemClassId_", $"class {row.Id} rolls back to the label it had");
                StringAssert.Contains(seed, $"{{ {row.Id}, \"{row.Client}\"", $"the seed carries class {row.Id} under the client's name");
            }
        }

        /// <summary>
        /// The accident the labels caused. DefaultInventoryCategory sorts a template with no item_template row
        /// by its class name, and its mission rule was StartsWith("Mis"), so "Missing_ItemClassId_1/15" made
        /// Rifle Ammo a mission item. The rule now asks for one of the three shapes the client's own 857
        /// "Mis" names take - "Mis_" and a zone, "Mis" and a capital, or the word "Mission" - which leaves
        /// every one of them a mission item and refuses a label like that even if one comes back.
        /// </summary>
        [TestMethod]
        public void TheMissionRuleReadsOnlyTheShapesTheClientUses()
        {
            // The client's own names, all three shapes, including the one all-capitals outlier.
            foreach (var name in new[] { "Mis_Palisades_ToolHealingDisc", "MisPalisadesItemDatabladedeployment",
                         "MissionThraxHead", "MISBaneArmoryBlueprints", "MisCavesofDonn_DyingForean" })
                Assert.AreEqual(InventoryCategory.Mission, ItemManager.DefaultInventoryCategory(Plain(name)), name);

            // The labels this server invented, and the names it has now.
            foreach (var name in new[] { "Missing_ItemClassId_1/15", "Missing_ItemClassId_15/15" })
                Assert.AreNotEqual(InventoryCategory.Mission, ItemManager.DefaultInventoryCategory(Plain(name)), name);

            foreach (var row in ClientNamesForItemsRows.Rows)
                Assert.AreNotEqual(InventoryCategory.Mission, ItemManager.DefaultInventoryCategory(Plain(row.Client)),
                    $"class {row.Id}, {row.Client}");

            // The rest of the rule is untouched.
            Assert.AreEqual(InventoryCategory.Consumable, ItemManager.DefaultInventoryCategory(Plain("Ammo_Nucleotides_1_Pyrimidines")));
            Assert.AreEqual(InventoryCategory.Crafting, ItemManager.DefaultInventoryCategory(Plain("Component_ArmorDye_Pigment_Magenta")));
            Assert.AreEqual(InventoryCategory.Misc, ItemManager.DefaultInventoryCategory(Plain("UsableTwoStateHumWormhole")));
        }

        /// <summary>An item class carrying nothing but a name, which is all the rule reads.</summary>
        private static EntityClass Plain(string className)
            => new EntityClass(0, className, 776, 1, new List<AugmentationType> { AugmentationType.Item }, false);

        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }
    }
}
