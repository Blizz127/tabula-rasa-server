using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.WildernessData;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class FormingAlliancesMigrationTests
    {
        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            WildernessHubFormingAlliancesRows.InsertData(migration);
            foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
            foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
        }

        [TestMethod]
        public void EverySeededFieldMatchesItsProvenanceManifest()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            WildernessHubFormingAlliancesRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();
            using var document = JsonDocument.Parse(File.ReadAllText(
                EvidenceLocator.EvidenceFile("wilderness-forming-alliances-reconstruction.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray().ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "Every inserted row needs one manifest row");

            foreach (var row in evidence)
            {
                var table = row.GetProperty("table").GetString();
                var key = row.GetProperty("key");
                var matches = inserted.Where(item => item.Table == table && key.EnumerateObject().All(field =>
                    item.Values.TryGetValue(field.Name, out var actual) && Same(actual, field.Value))).ToList();
                Assert.AreEqual(1, matches.Count, $"{table}/{key} must match exactly one seed row");
                var values = row.GetProperty("values");
                Assert.AreEqual(matches[0].Values.Count, values.EnumerateObject().Count(), $"{table}/{key} field count");
                foreach (var field in values.EnumerateObject())
                {
                    Assert.IsTrue(matches[0].Values.TryGetValue(field.Name, out var actual), $"{table}.{field.Name}");
                    Assert.IsTrue(Same(actual, field.Value), $"{table}.{field.Name} differs from manifest");
                }

                var covered = new HashSet<string>();
                foreach (var group in row.GetProperty("provenance").EnumerateArray())
                {
                    Assert.IsTrue(new[] { "original", "observed", "measured", "inferred", "analogue" }
                        .Contains(group.GetProperty("tier").GetString()), $"{table}/{key}: unknown tier");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(group.GetProperty("source").GetString()), $"{table}/{key}: missing source");
                    foreach (var field in group.GetProperty("fields").EnumerateArray())
                        Assert.IsTrue(covered.Add(field.GetString()), $"{table}/{key}: duplicate provenance for {field}");
                }
                CollectionAssert.AreEquivalent(values.EnumerateObject().Select(field => field.Name).ToArray(),
                    covered.ToArray(), $"{table}/{key}: missing field provenance");
            }
        }

        [TestMethod]
        public void ConscientiousGateMatchesItsFieldProvenance()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            WildernessHubConscientiousGateRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>().Single();
            Assert.AreEqual(inserted.Columns.Length, inserted.ColumnTypes?.Length ?? 0);
            Assert.AreEqual("npc_mission_prerequisite", inserted.Table);
            Assert.AreEqual(1, inserted.Values.GetLength(0));

            using var document = JsonDocument.Parse(File.ReadAllText(
                EvidenceLocator.EvidenceFile("wilderness-conscientious-gate.json")));
            var row = document.RootElement.GetProperty("row");
            Assert.AreEqual(inserted.Table, row.GetProperty("table").GetString());
            var values = row.GetProperty("values");
            var fields = row.GetProperty("fields");
            Assert.AreEqual(inserted.Columns.Length, values.EnumerateObject().Count());
            Assert.AreEqual(inserted.Columns.Length, fields.EnumerateObject().Count());
            for (var index = 0; index < inserted.Columns.Length; index++)
            {
                var column = inserted.Columns[index];
                Assert.IsTrue(Same(inserted.Values[0, index], values.GetProperty(column)), column);
                var provenance = fields.GetProperty(column);
                Assert.IsTrue(new[] { "original", "observed", "measured", "inferred", "analogue" }
                    .Contains(provenance.GetProperty("tier").GetString()), column);
                Assert.IsFalse(string.IsNullOrWhiteSpace(provenance.GetProperty("source").GetString()), column);
                Assert.IsFalse(string.IsNullOrWhiteSpace(provenance.GetProperty("exact_location").GetString()), column);
            }
        }

        private static bool Same(object actual, JsonElement expected) => expected.ValueKind switch
        {
            JsonValueKind.Number => Convert.ToDecimal(actual) == expected.GetDecimal(),
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.String => Convert.ToString(actual) == expected.GetString(),
            _ => false
        };
    }
}
