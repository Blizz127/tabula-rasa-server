using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Migrations.WildernessData;
using Rasa.Services.DbContext;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// WildernessMunsonWithdrawal and WildernessXenobiologySamples against their manifest: every seeded and every changed
    /// value is the one its manifest row records with its tier, the drop chances carry the tiers the evidence allows
    /// (100 inferred where a walkthrough counts kills equal to the target, 50 analogue under OD-61 otherwise), the
    /// withdrawal is recorded as original evidence from the official live notes, and each rollback gives back the rows
    /// exactly.
    /// </summary>
    [TestClass]
    public class WildernessXenobiologySamplesTests
    {
        private static readonly string[] Tiers = { "original", "observed", "measured", "inferred", "analogue" };

        [TestMethod]
        public void HandWrittenRowsCarryStoreTypesForEveryDataOperation()
        {
            foreach (var (up, down) in new (Action<MigrationBuilder>, Action<MigrationBuilder>)[]
                     {
                         (WildernessMunsonWithdrawalRows.InsertData, WildernessMunsonWithdrawalRows.DeleteData),
                         (WildernessXenobiologySamplesRows.InsertData, WildernessXenobiologySamplesRows.DeleteData)
                     })
            {
                foreach (var apply in new[] { up, down })
                {
                    var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                    apply(migration);
                    foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                        Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                    foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                        Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                    foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                    {
                        Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                        Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                    }
                }
            }
        }

        [TestMethod]
        public void EverySeededAndChangedValueMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            WildernessXenobiologySamplesRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .Where(operation => !(operation.Table == "npc_mission_objective" && operation.Values[0, 3] == null))
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();
            var updated = migration.Operations.OfType<UpdateDataOperation>()
                .Select(operation => (operation.Table,
                    Key: operation.KeyColumns.Select((column, index) => (column, value: operation.KeyValues[0, index])).ToDictionary(p => p.column, p => p.value),
                    Values: operation.Columns.Select((column, index) => (column, value: operation.Values[0, index])).ToDictionary(p => p.column, p => p.value)))
                .ToList();
            Assert.AreEqual(14, inserted.Count);
            Assert.AreEqual(3, updated.Count);

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var rows = root.GetProperty("rows").EnumerateArray().ToList();
            var evidence = rows.Where(row => row.GetProperty("migration").GetString() == WildernessXenobiologySamplesRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            var storage = new HashSet<(string, string)>
            {
                ("npc_mission", "comment"), ("npc_mission_prerequisite", "comment"), ("npc_mission_objective_binding", "comment")
            };
            foreach (var row in evidence)
            {
                var table = row.GetProperty("table").GetString();
                var key = row.GetProperty("key");
                var matches = inserted.Where(item => item.Table == table && key.EnumerateObject().All(field =>
                    item.Values.TryGetValue(field.Name, out var actual) && Same(actual, field.Value))).ToList();
                Assert.AreEqual(1, matches.Count, $"{table} {key} must match exactly one seeded row");
                var fields = row.GetProperty("fields");
                foreach (var field in fields.EnumerateObject())
                {
                    Assert.IsTrue(matches[0].Values.TryGetValue(field.Name, out var actual), $"{table} {key}.{field.Name} is not seeded");
                    AssertField(field.Value, actual, $"{table} {key}.{field.Name}");
                }
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) || storage.Contains((table, column)),
                        $"{table} {key}.{column} is seeded without provenance");
            }

            // The rows the migration changes carry both migrations and a changes entry.
            const string both = "WildernessCollectionDrop; " + WildernessXenobiologySamplesRows.Migration;
            var changes = root.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() == WildernessXenobiologySamplesRows.Migration).ToList();
            foreach (var (table, key, values) in updated)
            {
                var row = rows.Single(candidate => candidate.GetProperty("table").GetString() == table &&
                    key.All(pair => candidate.GetProperty("key").TryGetProperty(pair.Key, out var part) && Same(pair.Value, part)));
                Assert.AreEqual(both, row.GetProperty("migration").GetString(), $"{table} {string.Join(",", key.Values)}");
                foreach (var (column, value) in values.Where(pair => !storage.Contains((table, pair.Key))))
                    AssertField(row.GetProperty("fields").GetProperty(column), value, $"{table} {string.Join(",", key.Values)}.{column}");
                Assert.IsTrue(changes.Any(change => change.GetProperty("table").GetString() == table &&
                    key.All(pair => change.GetProperty("key").TryGetProperty(pair.Key, out var part) && Same(pair.Value, part))),
                    $"{table} {string.Join(",", key.Values)} has no changes entry");
            }

            // Drop chances: 758 and 787 every kill (inferred from the walkthrough kill counts), 776 and 771 the 479 estimate
            // as an analogue under OD-61. Nothing is presented as original or observed.
            foreach (var (mission, objective, value, tier) in new (int, int, double, string)[]
                     { (758, 3, 100, "inferred"), (787, 3, 100, "inferred"), (776, 2, 50, "analogue"), (771, 2, 50, "analogue") })
            {
                var drop = rows.Single(row => row.GetProperty("table").GetString() == "npc_mission_objective_binding" &&
                        row.GetProperty("key").GetProperty("mission_id").GetInt32() == mission &&
                        row.GetProperty("key").GetProperty("objective_id").GetInt32() == objective)
                    .GetProperty("fields").GetProperty("drop_chance");
                Assert.AreEqual(value, drop.GetProperty("value").GetDouble(), $"{mission}");
                Assert.AreEqual(tier, drop.GetProperty("tier").GetString(), $"{mission}");
                if (tier == "analogue")
                {
                    Assert.AreEqual("OD-61", drop.GetProperty("decision").GetString(), $"{mission}");
                    StringAssert.Contains(drop.GetProperty("counterpart").GetString(), "479", $"{mission}");
                }
                else
                    StringAssert.Contains(drop.GetProperty("reasoning").GetString(), "Kill", $"{mission}");
            }

            var decisions = root.GetProperty("owner_decisions").EnumerateArray().ToDictionary(d => d.GetProperty("id").GetString());
            Assert.AreEqual("approved", decisions["OD-61"].GetProperty("status").GetString());
            Assert.AreEqual("approved", decisions["OD-66"].GetProperty("status").GetString());

            var gaps = root.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-COLLECTION-DROP-CHANCE", "GAP-COLLECTION-TEMPLATE-CHOICE", "GAP-758-FITHIK-GEOGRAPHY",
                         "GAP-776-BLOOD-SOURCES", "GAP-DOCTOR-CREDITS-CONFLICT", "GAP-DOCTOR-CHAIN-GATES", "GAP-D11-WITHDRAWN-IN-PROGRESS" })
                Assert.IsTrue(gaps.Contains(gap), gap);
        }

        [TestMethod]
        public void TheD11WithdrawalIsRecordedAsOriginalEvidenceAndNoWithdrawnMissionIsInTheManifest()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var withdrawn = new[] { 751, 767, 769, 780 };

            Assert.IsFalse(root.GetProperty("rows").EnumerateArray().Any(row =>
                    (row.GetProperty("key").TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number &&
                     row.GetProperty("table").GetString().StartsWith("npc_mission") && withdrawn.Contains(id.GetInt32())) ||
                    (row.GetProperty("key").TryGetProperty("mission_id", out var mission) && mission.ValueKind == JsonValueKind.Number &&
                     withdrawn.Contains(mission.GetInt32()))),
                "a withdrawn mission still has a manifest row");

            var change = root.GetProperty("changes").EnumerateArray()
                .Single(entry => entry.GetProperty("migration").GetString() == WildernessMunsonWithdrawalRows.Migration);
            Assert.AreEqual("original", change.GetProperty("tier").GetString());
            Assert.AreEqual(JsonValueKind.Null, change.GetProperty("new").ValueKind);
            var notes = change.GetProperty("citations").EnumerateArray()
                .Where(citation => citation.GetProperty("source").GetString() == "official_notes:d11-rgtr-20081220").ToList();
            Assert.IsTrue(notes.Any(citation => citation.GetProperty("locator").GetString().Contains("are no longer available")));
            Assert.IsTrue(notes.Any(citation => citation.GetProperty("locator").GetString().Contains("permanently disabled")));
            var source = root.GetProperty("sources").GetProperty("official_notes:d11-rgtr-20081220");
            Assert.AreEqual("official_notes", source.GetProperty("kind").GetString());
            Assert.AreEqual("3dfe1626d2e031c2eb88e38459caf0bfa9e6c49e34c698fbb179d557271dd7d8", source.GetProperty("sha256").GetString());

            var omitted = root.GetProperty("omitted").EnumerateArray()
                .Where(entry => entry.GetProperty("gap").GetString() == "GAP-D11-WITHDRAWN-IN-PROGRESS")
                .Select(entry => entry.GetProperty("what").GetString()).ToList();
            foreach (var mission in withdrawn)
                Assert.IsTrue(omitted.Any(what => what.StartsWith($"{mission} ")), $"{mission} is not recorded as intentionally never offered");
        }

        [TestMethod]
        public void BothRollbacksRestoreTheRowsExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            SeedCollectionDropState(context);
            var before = Snapshot(connection);

            Apply(context, Build(WildernessMunsonWithdrawalRows.InsertData));
            var withdrawn = Snapshot(connection);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 767"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 767 AND ordinal IS NULL"));

            Apply(context, Build(WildernessXenobiologySamplesRows.InsertData));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (758, 771, 776, 787)"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE kind = 9 AND drop_chance > 0"));
            // An unrelated mission's rows are never touched.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 479 AND credits = 400"));

            Apply(context, Build(WildernessXenobiologySamplesRows.DeleteData));
            Assert.AreEqual(withdrawn, Snapshot(connection));
            Apply(context, Build(WildernessMunsonWithdrawalRows.DeleteData));
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>The rows WildernessCollectionDrop leaves (drop_chance 0 since MissionItemDropChance), plus the client skeleton.</summary>
        private static void SeedCollectionDropState(SqliteWorldContext context)
        {
            context.Database.EnsureCreated();
            foreach (var sql in new[]
                     {
                         "INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES " +
                         "(767, 109, 109, 5, 1, 10000001, 0, 0, 'Mighty Miasma (Wilderness)'), (771, 110, 110, 5, 1, 10000001, 0, 0, 'Droning On (Wilderness)'), " +
                         "(787, 111, 111, 5, 1, 10000001, 0, 0, 'Xanx For the Help (Wilderness)'), (479, 43, 43, 5, 1, 10000001, 0, 0, 'Forming Alliances')",
                         "INSERT INTO npc_mission_objective (mission_id, objective_id, comment, is_required, ordinal, revealed_on_accept) VALUES " +
                         "(767, 2, 'Get 6 Miasma Goo Samples.', 1, 1, 1), (771, 2, 'Acquire 6 Shield Drone Parts.', 1, 1, 1), (787, 3, 'Acquire 4 Xanx Pincers', 1, 1, 1), " +
                         "(758, 3, 'Acquire 10 Fithik Spleens', NULL, NULL, NULL), (776, 2, 'Acquire 10 Samples of Thrax Blood', NULL, NULL, NULL)",
                         "INSERT INTO npc_mission_objective_binding (mission_id, objective_id, binding_id, kind, area_id, placement_id, creature_id, action_id, destroying_hit_only, equip_match, item_template_id, drop_chance, item_set_id, target_state, counter_id, shared_kill_credit, comment) VALUES " +
                         "(767, 2, 0, 6, 0, 0, 88, 0, 0, 0, 0, 0, 0, 0, 0, 0, '767/2 kill creature 88'), (771, 2, 0, 6, 0, 0, 85, 0, 0, 0, 0, 0, 0, 0, 0, 0, '771/2 kill creature 85'), " +
                         "(787, 3, 0, 6, 0, 0, 87, 0, 0, 0, 0, 0, 0, 0, 0, 0, '787/3 kill creature 87')",
                         "INSERT INTO npc_mission_objective_counter (mission_id, objective_id, counter_id, initial_value, target_value) VALUES (767, 2, 0, 0, 6), (771, 2, 0, 0, 6), (787, 3, 0, 0, 4)",
                         "INSERT INTO npc_mission_reward (id, type, credits, item_template_id, quantity) VALUES (767, 3, 4000, 0, 0), (767, 1, 600, 0, 0), (771, 1, 300, 0, 0), (787, 3, 4000, 0, 0), (787, 1, 600, 0, 0), (479, 1, 400, 0, 0)"
                     })
                context.Database.ExecuteSqlRaw(sql);
        }

        private static void AssertField(JsonElement field, object actual, string label)
        {
            Assert.IsTrue(Same(actual, field.GetProperty("value")), $"{label} differs from the manifest");
            var tier = field.GetProperty("tier").GetString();
            Assert.IsTrue(Tiers.Contains(tier), $"{label}: unknown tier {tier}");
            Assert.IsTrue(field.GetProperty("citations").GetArrayLength() > 0, $"{label} has no citation");
            Assert.AreNotEqual("observed", tier, $"{label}: no footage backs these values");
            if (tier == "inferred")
                Assert.IsFalse(string.IsNullOrWhiteSpace(field.GetProperty("reasoning").GetString()), $"{label}: inferred without reasoning");
        }

        private static IReadOnlyList<MigrationOperation> Build(Action<MigrationBuilder> rows)
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            rows(builder);
            return builder.Operations;
        }

        private static void Apply(SqliteWorldContext context, IReadOnlyList<MigrationOperation> operations)
        {
            var generator = context.GetService<IMigrationsSqlGenerator>();
            foreach (var command in generator.Generate(operations, context.Model))
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "npc_mission", "npc_mission_objective", "npc_mission_objective_binding", "npc_mission_objective_counter",
                         "npc_mission_reward", "npc_mission_prerequisite" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
                rows.Sort(StringComparer.Ordinal);
                parts.Add(table + ":" + string.Join(";", rows));
            }
            return string.Join("\n", parts);
        }

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }

        private static bool Same(object actual, JsonElement expected) => expected.ValueKind switch
        {
            JsonValueKind.Number => Convert.ToDecimal(actual) == expected.GetDecimal(),
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.String => Convert.ToString(actual) == expected.GetString(),
            _ => false
        };

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteWorldContext Context(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());
    }
}
