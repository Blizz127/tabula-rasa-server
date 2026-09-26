using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.WildernessData;

namespace Rasa.Test.Reconstruction
{
    /// <summary>
    /// MissionRewardItems against its manifest rows: every seeded field has exactly one inferred-tier provenance with
    /// a dated TaRapedia revision, every template names its client class, and the caveats the review required are
    /// open gaps. Estimates are never presented as original or observed.
    /// </summary>
    [TestClass]
    public class MissionRewardItemsProvenanceTests
    {
        private static readonly string[] Columns = { "type", "credits", "item_template_id", "quantity" };

        [TestMethod]
        public void EverySeededRewardItemMatchesItsManifestRowAtInferredTier()
        {
            var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            MissionRewardItemsRows.InsertData(builder);
            var insert = (InsertDataOperation)builder.Operations.Single();
            Assert.AreEqual("npc_mission_reward", insert.Table);
            var seeded = Enumerable.Range(0, insert.Values.GetLength(0))
                .Select(row => insert.Columns.Select((column, index) => (column, value: insert.Values[row, index])).ToDictionary(pair => pair.column, pair => pair.value))
                .ToList();

            // The seed is the approved list, in its order.
            CollectionAssert.AreEqual(
                ContentSchemaMigrationTests.ApprovedMissionRewardItems.SelectMany(entry => entry.Items.Select(item => (entry.Mission, item.Template, item.Quantity))).ToArray(),
                seeded.Select(row => (Convert.ToUInt32(row["id"]), Convert.ToUInt32(row["item_template_id"]), Convert.ToUInt32(row["quantity"]))).ToArray());

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = manifest.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == MissionRewardItemsRows.Migration).ToList();
            Assert.AreEqual(seeded.Count, evidence.Count, "one manifest row per seeded row");

            foreach (var values in seeded)
            {
                var label = $"{values["id"]}/{values["item_template_id"]}";
                var row = evidence.Single(candidate =>
                {
                    var key = candidate.GetProperty("key");
                    return key.GetProperty("id").GetUInt32() == Convert.ToUInt32(values["id"]) &&
                           key.GetProperty("type").GetUInt32() == Convert.ToUInt32(values["type"]) &&
                           key.GetProperty("item_template_id").GetUInt32() == Convert.ToUInt32(values["item_template_id"]);
                });
                Assert.AreEqual("npc_mission_reward", row.GetProperty("table").GetString(), label);
                var fields = row.GetProperty("fields");
                CollectionAssert.AreEquivalent(Columns, fields.EnumerateObject().Select(field => field.Name).ToArray(), label);

                foreach (var column in Columns)
                {
                    var field = fields.GetProperty(column);
                    Assert.AreEqual(Convert.ToDecimal(values[column]), field.GetProperty("value").GetDecimal(), $"{label}.{column}");
                    Assert.AreEqual("inferred", field.GetProperty("tier").GetString(), $"{label}.{column}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(field.GetProperty("reasoning").GetString()), $"{label}.{column}");
                    var citations = field.GetProperty("citations").EnumerateArray().ToList();
                    Assert.IsTrue(citations.Any(citation => citation.GetProperty("source").GetString() == "community_db:tarapedia-reward-history" &&
                                                            citation.GetProperty("locator").GetString().Contains("?oldid=")), $"{label}.{column}: no dated wiki revision");
                }

                // The template names its client class, and its reasoning carries the list shape's evidence strength.
                var template = fields.GetProperty("item_template_id");
                Assert.IsTrue(template.GetProperty("citations").EnumerateArray().Any(citation =>
                    citation.GetProperty("source").GetString() == "client_table:game.zip" &&
                    citation.GetProperty("locator").GetString().Contains($"itemTemplateItemClass {values["item_template_id"]} -> ")), $"{label}: no client class");
                var reasoning = template.GetProperty("reasoning").GetString();
                StringAssert.Contains(reasoning, "GAP-MISSION-REWARD-TEMPLATE-ID", label);
                StringAssert.Contains(reasoning, "post-1.4, no documented later change", label);
                if (Convert.ToUInt32(values["item_template_id"]) >= 120000)
                {
                    StringAssert.Contains(reasoning, "consecutive run", label);
                    StringAssert.Contains(reasoning, "GAP-MISSION-REWARD-MODULES", label);
                }
            }

            var mission983 = evidence.Where(row => row.GetProperty("key").GetProperty("id").GetUInt32() == 983)
                .Select(row => row.GetProperty("fields").GetProperty("item_template_id").GetProperty("reasoning").GetString());
            Assert.IsTrue(mission983.All(text => text.Contains("confidence MEDIUM") && text.Contains("3-5%")), "983 is medium confidence");
            var stealthLegs = evidence.Single(row => row.GetProperty("key").GetProperty("item_template_id").GetUInt32() == 120421);
            StringAssert.Contains(stealthLegs.GetProperty("fields").GetProperty("item_template_id").GetProperty("reasoning").GetString(), "120109-120112");

            var gaps = root.GetProperty("gaps").EnumerateArray().ToDictionary(gap => gap.GetProperty("id").GetString(), gap => gap.GetProperty("summary").GetString());
            foreach (var id in new[] { "GAP-MISSION-REWARD-TEMPLATE-ID", "GAP-MISSION-REWARD-CHOICE", "GAP-MISSION-REWARD-MODULES", "GAP-MISSION-REWARD-FINAL-STATE" })
                Assert.IsTrue(gaps.ContainsKey(id), id);
            StringAssert.Contains(gaps["GAP-MISSION-REWARD-FINAL-STATE"], "2008-04-28");
            foreach (var mission in new[] { "1541", "1673", "1040", "970", "1068", "1863" })
                StringAssert.Contains(gaps["GAP-MISSION-LEVEL"], mission);
            StringAssert.Contains(gaps["GAP-MISSION-REWARD-ITEMS"], "479");
            StringAssert.Contains(gaps["GAP-MISSION-REWARD-ITEMS"], "1407");
            StringAssert.Contains(gaps["GAP-MISSION-REWARD-ITEMS"], "Logos");
        }

        [TestMethod]
        public void RollbackDeletesExactlyTheSeededRows()
        {
            var up = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            var down = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            MissionRewardItemsRows.InsertData(up);
            MissionRewardItemsRows.DeleteData(down);
            var insert = (InsertDataOperation)up.Operations.Single();
            var delete = (DeleteDataOperation)down.Operations.Single();
            Assert.AreEqual(insert.Table, delete.Table);
            CollectionAssert.AreEqual(new[] { "id", "type", "item_template_id" }, delete.KeyColumns);
            var inserted = new List<(object, object, object)>();
            for (var row = 0; row < insert.Values.GetLength(0); row++)
                inserted.Add((insert.Values[row, 0], insert.Values[row, 1], insert.Values[row, 3]));
            var deleted = new List<(object, object, object)>();
            for (var row = 0; row < delete.KeyValues.GetLength(0); row++)
                deleted.Add((delete.KeyValues[row, 0], delete.KeyValues[row, 1], delete.KeyValues[row, 2]));
            CollectionAssert.AreEqual(inserted, deleted);
            Assert.AreEqual(inserted.Count, inserted.Distinct().Count(), "every key is unique, so the delete removes one row each");
        }
    }
}
