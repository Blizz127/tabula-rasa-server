using System;
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
    /// OfficialNotesCorrections against its manifest: each value it writes is a changes entry at original tier citing
    /// the official live notes, the manifest rows carry the new values, Down restores exactly what was there, and
    /// the notes audit's unresolved findings are open gaps rather than guessed data.
    /// </summary>
    [TestClass]
    public class OfficialNotesCorrectionsProvenanceTests
    {
        private static JsonElement Manifest()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            return document.RootElement.Clone();
        }

        private static UpdateDataOperation[] Updates(Action<MigrationBuilder> rows)
        {
            var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            rows(builder);
            return builder.Operations.Cast<UpdateDataOperation>().ToArray();
        }

        [TestMethod]
        public void EveryCorrectionIsAnOriginalTierChangeCitingTheLiveNotes()
        {
            var root = Manifest();
            var sources = root.GetProperty("sources");
            var changes = root.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString().Contains(OfficialNotesCorrectionsRows.Migration)).ToList();
            var updates = Updates(OfficialNotesCorrectionsRows.InsertData);
            Assert.AreEqual(3, updates.Length);
            Assert.AreEqual(updates.Length, changes.Count, "one changes entry per written value");

            foreach (var update in updates)
            {
                Assert.AreEqual(1, update.Columns.Length);
                var column = update.Columns[0];
                var value = update.Values[0, 0];
                var change = changes.Single(entry => entry.GetProperty("table").GetString() == update.Table && entry.GetProperty("field").GetString() == column);
                var label = $"{update.Table}.{column}";

                var key = change.GetProperty("key");
                for (var i = 0; i < update.KeyColumns.Length; i++)
                    Assert.AreEqual(Convert.ToUInt32(update.KeyValues[0, i]), key.GetProperty(update.KeyColumns[i]).GetUInt32(), $"{label} key {update.KeyColumns[i]}");

                var recorded = change.GetProperty("new");
                if (value is bool flag) Assert.AreEqual(flag, recorded.GetBoolean(), label);
                else if (value is string text) Assert.AreEqual(text, recorded.GetString(), label);
                else Assert.AreEqual(Convert.ToInt64(value), recorded.GetInt64(), label);

                Assert.AreEqual("original", change.GetProperty("tier").GetString(), label);
                var citations = change.GetProperty("citations").EnumerateArray().ToList();
                Assert.IsTrue(citations.Count > 0, label);
                foreach (var citation in citations)
                {
                    var source = citation.GetProperty("source").GetString();
                    Assert.AreEqual("official_notes", sources.GetProperty(source).GetProperty("kind").GetString(), $"{label}: {source}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(citation.GetProperty("locator").GetString()), $"{label}: {source}");
                }
            }

            // The rows the manifest keeps for the corrected keys carry the new values at original tier.
            var rows = root.GetProperty("rows").EnumerateArray().ToList();
            var mystery = rows.Single(row => row.GetProperty("table").GetString() == "npc_mission" && row.GetProperty("key").GetProperty("id").GetUInt32() == 2016);
            Assert.AreEqual(50, mystery.GetProperty("fields").GetProperty("level").GetProperty("value").GetInt32());
            Assert.AreEqual("original", mystery.GetProperty("fields").GetProperty("level").GetProperty("tier").GetString());
            var xanx = rows.Single(row => row.GetProperty("table").GetString() == "npc_mission_objective_binding" &&
                                          row.GetProperty("key").GetProperty("mission_id").GetUInt32() == 682 &&
                                          row.GetProperty("key").GetProperty("objective_id").GetUInt32() == 3);
            var shared = xanx.GetProperty("fields").GetProperty("shared_kill_credit");
            Assert.IsTrue(shared.GetProperty("value").GetBoolean());
            Assert.AreEqual("original", shared.GetProperty("tier").GetString());
            CollectionAssert.AreEquivalent(new[] { "official_notes:1.6", "official_notes:d8" },
                shared.GetProperty("citations").EnumerateArray().Select(citation => citation.GetProperty("source").GetString()).ToArray());
        }

        [TestMethod]
        public void RollbackRestoresEveryPreviousValue()
        {
            var up = Updates(OfficialNotesCorrectionsRows.InsertData);
            var down = Updates(OfficialNotesCorrectionsRows.DeleteData);
            Assert.AreEqual(up.Length, down.Length);
            var changes = Manifest().GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString().Contains(OfficialNotesCorrectionsRows.Migration)).ToList();

            foreach (var update in down)
            {
                var forward = up.Single(entry => entry.Table == update.Table && entry.Columns[0] == update.Columns[0]);
                CollectionAssert.AreEqual(forward.KeyColumns, update.KeyColumns);
                CollectionAssert.AreEqual(forward.KeyValues.Cast<object>().ToArray(), update.KeyValues.Cast<object>().ToArray());
                var old = changes.Single(entry => entry.GetProperty("table").GetString() == update.Table &&
                                                  entry.GetProperty("field").GetString() == update.Columns[0]).GetProperty("old");
                var value = update.Values[0, 0];
                if (value is bool flag) Assert.AreEqual(flag, old.GetBoolean());
                else if (value is string text) Assert.AreEqual(text, old.GetString());
                else Assert.AreEqual(Convert.ToInt64(value), old.GetInt64());
            }
        }

        [TestMethod]
        public void TheAuditsUnresolvedFindingsAreOpenGaps()
        {
            var gaps = Manifest().GetProperty("gaps").EnumerateArray()
                .ToDictionary(gap => gap.GetProperty("id").GetString(), gap => gap.GetProperty("summary").GetString());

            // Nothing is moved, granted or excluded on evidence that predates the change the notes describe.
            StringAssert.Contains(gaps["GAP-NOTES-CRF-RELOCATION"], "199085");
            StringAssert.Contains(gaps["GAP-NOTES-CRF-RELOCATION"], "199089");
            StringAssert.Contains(gaps["GAP-NOTES-1125-KEYCARD"], "50310");
            StringAssert.Contains(gaps["GAP-NOTES-1125-KEYCARD"], "Never grant two");
            StringAssert.Contains(gaps["GAP-NOTES-321-JOHNSON-PERKINS"], "308 m");
            StringAssert.Contains(gaps["GAP-NOTES-1390-QUILLAS-POSITION"], "2007-09-26");
            StringAssert.Contains(gaps["GAP-NOTES-976-STALKER-DISTRIBUTION"], "199812");
            StringAssert.Contains(gaps["GAP-NOTES-983-1041-ALTERNATIVE-COURSES"], "982");
            StringAssert.Contains(gaps["GAP-NOTES-682-SHARED-CREDIT-REACH"], "map channel");
            StringAssert.Contains(gaps["GAP-NOTES-1998-TIME-CAPSULE"], "never seed");
        }
    }
}
