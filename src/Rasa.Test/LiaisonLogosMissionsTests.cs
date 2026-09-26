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
    /// LiaisonLogosMissions: every seeded value is the one its manifest row records, the rollback gives back the client
    /// skeleton exactly, and every shrine the eleven missions wait on is the world's own row for the client's logosstone
    /// constant, on its giver's map. Loading and offerability are asserted on the migrated world in
    /// MissionContentLoadingTests, where the content layer attaches the shrine bindings.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class LiaisonLogosMissionsTests
    {
        /// <summary>
        /// Written out independently of LiaisonLogosMissionsRows: mission, giver, client objective, the client's logosstone
        /// constant for the word (logosstone.pyo), the word, and the map context the giver stands on.
        /// </summary>
        private static readonly (uint Mission, uint Giver, uint Objective, uint Stone, string Word, uint Map)[] Expected =
        {
            (1633, 133, 3, 2, "Attack", 1220), (1638, 133, 2, 10, "Enhance", 1220), (1639, 133, 6, 23, "Power", 1220),
            (1640, 133, 4, 1, "Area", 1220), (1634, 134, 4, 49, "Target", 1220), (1635, 134, 2, 53, "Here", 1220),
            (1643, 199004, 7, 3, "Backward", 1148), (1644, 199004, 8, 7, "Defend", 1148), (1646, 199004, 10, 15, "Give", 1148),
            (1647, 199004, 11, 18, "Increase", 1148), (1652, 199085, 16, 46, "Ground", 1244)
        };

        [TestMethod]
        public void TheSeedIsTheVerifiedListOfElevenOnTheClientsOwnShrineIds()
        {
            CollectionAssert.AreEqual(Expected.Select(e => (e.Mission, e.Giver)).OrderBy(e => e.Mission).ToArray(),
                LiaisonLogosMissionsRows.Missions.Select(m => (m.Id, m.Liaison)).OrderBy(m => m.Id).ToArray());
            CollectionAssert.AreEqual(Expected.Select(e => (e.Mission, e.Objective, e.Stone, $"Acquire Logos Information: {e.Word}")).OrderBy(e => e.Mission).ToArray(),
                LiaisonLogosMissionsRows.Objectives.Select(o => (o.Mission, o.Objective, o.Logos, o.Text)).OrderBy(o => o.Mission).ToArray());
            // No item reward: the shrine grants the Logos. Only currency rows, at most one of each type per mission.
            Assert.IsTrue(LiaisonLogosMissionsRows.Rewards.All(r => r.Type == 1 || r.Type == 3));
            Assert.AreEqual(LiaisonLogosMissionsRows.Rewards.Length, LiaisonLogosMissionsRows.Rewards.Select(r => (r.Mission, r.Type)).Distinct().Count());
            // Langerman's four take his observed level; the others their creature rows' (OD-100).
            Assert.IsTrue(LiaisonLogosMissionsRows.Missions.All(m => m.Level == (m.Liaison switch { 133u => 15u, 199085u => 25u, _ => 10u })));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) LiaisonLogosMissionsRows.InsertData(migration);
                else LiaisonLogosMissionsRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                // Nothing here touches a creature: Langerman's class belongs to MissingMissionGivers.
                Assert.IsFalse(migration.Operations.OfType<UpdateDataOperation>().Any());
                Assert.IsFalse(migration.Operations.OfType<InsertDataOperation>().Any(row => row.Table.StartsWith("creature", StringComparison.Ordinal)));
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            LiaisonLogosMissionsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == LiaisonLogosMissionsRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

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
                    Assert.IsTrue(Same(actual, field.Value.GetProperty("value")), $"{table} {key}.{field.Name} differs from the manifest");
                    Assert.IsTrue(field.Value.GetProperty("citations").GetArrayLength() > 0, $"{table} {key}.{field.Name} has no citation");
                }
                row.TryGetProperty("storage_fields", out var storage);
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) ||
                                  (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty(column, out _)),
                        $"{table} {key}.{column} is seeded without provenance");
                if (storage.ValueKind == JsonValueKind.Object)
                    foreach (var column in storage.EnumerateObject())
                        Assert.IsTrue(Same(matches[0].Values[column.Name], column.Value.GetProperty("value")), $"{table} {key}.{column.Name} storage differs");

                // Every level is an analogue under OD-100; every seeded amount carries its era.
                if (table == "npc_mission")
                    Assert.AreEqual(("analogue", "OD-100"), (fields.GetProperty("level").GetProperty("tier").GetString(), fields.GetProperty("level").GetProperty("decision").GetString()));
                if (table == "npc_mission_reward")
                    Assert.AreEqual("pre-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
            }

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-LIAISON-LOGOS-REWARD-ERA", "GAP-LIAISON-LOGOS-REWARDS-MISSING", "GAP-LIAISON-LOGOS-SPEAKER",
                         "GAP-LOGOS-SKIP-POWER", "GAP-LIAISON-LOGOS-POWER-D11", "GAP-LIAISON-LOGOS-UNSEEDED" })
                Assert.IsTrue(gaps.Contains(gap), gap);
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            // The real migration chain up to the migration before this one (whatever that is once other batches merge),
            // then this one up and down again: the mission tables must come back byte for byte.
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + LiaisonLogosMissionsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1633, 1634, 1635, 1638, 1639, 1640, 1643, 1644, 1646, 1647, 1652) AND ordinal IS NULL"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1633, 1634, 1635, 1638, 1639, 1640, 1643, 1644, 1646, 1647, 1652)"));
            // The 23 SeedLogosMissions rows and 1069 are untouched.
            Assert.AreEqual(24L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1069, 1645, 1648, 1649, 1650, 1651, 1654, 1655, 1657, 1658, 1659, 1661, 1663, 1664, 1665, 1666, 1667, 1668, 1669, 1670, 1671, 1749, 1750, 1814)"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only): each shrine is the `logos` row whose id is the client's constant for the word the
        /// objective names, and it stands on the map its giver stands on. The world has no row yet for any of the eleven.
        /// </summary>
        [TestMethod]
        public void EveryShrineIsTheWorldsRowForItsWordOnItsGiversMap()
        {
            using var world = OpenWorld(RepositoryRoot());
            foreach (var (mission, giver, _, stone, word, map) in Expected)
            {
                Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM logos WHERE id = {stone} AND name = '{word}' AND map_context_id = {map}"), $"{mission}: shrine {stone} {word}");
                var giverMap = Scalar(world,
                    $"SELECT COALESCE((SELECT map_context_id FROM content_placement WHERE creature_id = {giver} AND kind = 1 LIMIT 1), " +
                    $"(SELECT map_context_id FROM spawnpool WHERE creature_1_Id = {giver} AND creature_1_max_count > 0 LIMIT 1), 0)");
                Assert.AreEqual((long)map, giverMap, $"{mission}: giver {giver} stands on map {giverMap}");
                Assert.AreEqual(0L, Scalar(world, $"SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id = {mission}"), $"{mission}: no redirect row");
            }
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM npc_mission WHERE id = 1069"));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "npc_mission", "npc_mission_objective", "npc_mission_objective_binding", "npc_mission_objective_transition",
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
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static bool Same(object actual, JsonElement expected) => expected.ValueKind switch
        {
            JsonValueKind.Number => Convert.ToDecimal(actual) == expected.GetDecimal(),
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.String => Convert.ToString(actual) == expected.GetString(),
            _ => false
        };

        /// <summary>The repository root, found by walking up from the test binaries to the folder with the navmeshes.</summary>
        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }

        private static SqliteConnection OpenWorld(string root)
        {
            var path = Path.Combine(root, "rasaworld.db");
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                Assert.Inconclusive("rasaworld.db is not in the repository root; this check reads the world database");
            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }

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
