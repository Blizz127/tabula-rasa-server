using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.BootcampData;

namespace Rasa.Test.Reconstruction
{
    [TestClass]
    public class BootcampS2ProvenanceTests
    {
        internal static Dictionary<(string Table, uint Id), Dictionary<string, object>> CurrentSeededRows()
        {
            var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            BootcampS2GearingUpRows.InsertData(builder);
            BootcampPlacementGroundSnapRows.InsertData(builder);
            BootcampPlatformTopCorrectionRows.InsertData(builder);
            BootcampTargetDummyLaneRows.InsertData(builder);
            BootcampTargetDummyFrontRows.InsertData(builder);
            BootcampPracticeDummyHealthRows.InsertData(builder);
            BootcampDeSimoneCampPlacementRows.InsertData(builder);
            var seeded = new Dictionary<(string Table, uint Id), Dictionary<string, object>>();
            foreach (var operation in builder.Operations)
            {
                if (operation is InsertDataOperation insert && (insert.Table == "content_placement" || insert.Table == "creature"))
                    for (var row = 0; row < insert.Values.GetLength(0); row++)
                    {
                        var values = insert.Columns.Select((column, index) => (column, value: insert.Values[row, index]))
                            .ToDictionary(pair => pair.column, pair => pair.value);
                        seeded[(insert.Table, Convert.ToUInt32(values["id"]))] = values;
                    }
                else if (operation is UpdateDataOperation update && update.Table == "content_placement")
                    for (var row = 0; row < update.KeyValues.GetLength(0); row++)
                    {
                        var values = seeded[(update.Table, Convert.ToUInt32(update.KeyValues[row, 0]))];
                        for (var column = 0; column < update.Columns.Length; column++)
                            values[update.Columns[column]] = update.Values[row, column];
                    }
            }

            return seeded;
        }

        [TestMethod]
        public void EveryS2PlacementAndNpcHasCurrentPerFieldEvidence()
        {
            var seeded = CurrentSeededRows();
            Assert.AreEqual(9, seeded.Count, "six placements and three NPC creature identities");
            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = manifest.RootElement.GetProperty("rows").EnumerateArray().ToList();
            foreach (var (key, values) in seeded)
            {
                var matches = evidence.Where(row => row.GetProperty("table").GetString() == key.Table &&
                    row.GetProperty("key").GetProperty("id").GetUInt32() == key.Id).ToList();
                Assert.AreEqual(1, matches.Count, $"missing or duplicate evidence for {key}");
                var row = matches.Single();
                var fields = row.GetProperty("fields");
                Assert.IsTrue(ProvenanceRegistry.Default.TryGet(key.Table, out var registry));
                foreach (var required in registry.RequiredColumns)
                    Assert.IsTrue(fields.TryGetProperty(required, out _), $"{key}: required field {required} lacks provenance");
                foreach (var field in fields.EnumerateObject().Concat(row.GetProperty("storage_fields").EnumerateObject()))
                {
                    var actual = values.TryGetValue(field.Name, out var value) ? value : 0u; // later neutral schema columns
                    var expected = field.Value.GetProperty("value");
                    if (expected.ValueKind == JsonValueKind.Number)
                        Assert.AreEqual(expected.GetDecimal(), Convert.ToDecimal(actual), $"{key}.{field.Name}");
                    else
                        Assert.AreEqual(expected.ToString(), Convert.ToString(actual), $"{key}.{field.Name}");
                }
            }
        }

        [TestMethod]
        public void PracticeHealthCorrectionTouchesOnlyFirstDummyAndRestoresPriorEstimate()
        {
            foreach (var rollback in new[] { false, true })
            {
                var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
                if (rollback) BootcampPracticeDummyHealthRows.DeleteData(builder);
                else BootcampPracticeDummyHealthRows.InsertData(builder);
                Assert.AreEqual(1, builder.Operations.Count);
                var update = (UpdateDataOperation)builder.Operations.Single();
                Assert.AreEqual("content_placement", update.Table);
                CollectionAssert.AreEqual(new[] { "id" }, update.KeyColumns);
                Assert.AreEqual(198652u, update.KeyValues[0, 0]);
                CollectionAssert.AreEqual(new[] { "hit_points" }, update.Columns);
                Assert.AreEqual(rollback ? 100u : 1u, update.Values[0, 0]);
            }
        }

        [TestMethod]
        public void LoadedRifleCorrectionChangesOnlyItsMagazineAndRestoresZero()
        {
            foreach (var rollback in new[] { false, true })
            {
                var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
                if (rollback) BootcampCrateLoadedRifleRows.DeleteData(builder);
                else BootcampCrateLoadedRifleRows.InsertData(builder);
                Assert.AreEqual(1, builder.Operations.Count);
                var update = (UpdateDataOperation)builder.Operations.Single();
                Assert.AreEqual("content_item_set", update.Table);
                CollectionAssert.AreEqual(new[] { "item_set_id", "item_template_id" }, update.KeyColumns);
                CollectionAssert.AreEqual(new object[] { 19858u, 13713u }, update.KeyValues.Cast<object>().ToArray());
                CollectionAssert.AreEqual(new[] { "initial_ammo" }, update.Columns);
                Assert.AreEqual(rollback ? 0u : 20u, update.Values[0, 0]);
            }
        }
    }
}
