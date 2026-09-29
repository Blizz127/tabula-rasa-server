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
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// LogosGiveNegativeSwap and TarapediaBossLevels, the two fixes the DIT reference's discrepancy list led to
    /// (docs/retail-accuracy.md, 2026-09-27 DIT reference review): every updated value is the one its manifest change
    /// records, the loaded world carries the corrected shrines and levels, and the rollback gives the world back exactly.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DitDiscrepancyFixesTests
    {
        /// <summary>Written out independently of TarapediaBossLevelsRows: creature, client name id, upstream level, TaRapedia level.</summary>
        private static readonly (uint Id, uint NameId, uint Was, uint Level)[] Bosses =
        {
            (520024, 6904, 42, 28), (520025, 6905, 42, 30), (520026, 6906, 42, 26), (520027, 6907, 42, 26),
            (520048, 6991, 40, 28), (520049, 8786, 40, 25), (520031, 8917, 38, 42), (520036, 8916, 38, 42),
            (520056, 10017, 35, 37), (520058, 10016, 35, 37), (520059, 10018, 35, 37), (520061, 10015, 35, 37)
        };

        private const string SeedShrines =
            "INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES " +
            "(15, 7296, 1148, 184.17969, 122.953125, 137.64453, 'Give'), (33, 12691, 1148, 745.16797, 100.58594, -5.2539062, 'Negative')";

        [TestMethod]
        public void TheLevelsAreTheVerifiedTwelveAndOnlyTheLevelColumnChanges()
        {
            CollectionAssert.AreEqual(Bosses.Select(b => (b.Id, b.Was, b.Level)).ToArray(),
                TarapediaBossLevelsRows.Levels.Select(b => (b.Id, b.Was, b.Level)).ToArray());
            foreach (var migration in new[] { true, false }.Select(up => Build(up, TarapediaBossLevelsRows.InsertData, TarapediaBossLevelsRows.DeleteData)))
            {
                var updates = migration.Operations.Cast<UpdateDataOperation>().ToList();
                Assert.AreEqual(12, updates.Count);
                Assert.IsTrue(updates.All(u => u.Table == "creature" && u.Columns.SequenceEqual(new[] { "level" })));
            }
            // Palisades and Divide bosses are another batch's; none is touched here.
            Assert.IsFalse(TarapediaBossLevelsRows.Levels.Any(b => b.Map == 1244 || b.Map == 1148));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            foreach (var migration in new[]
                     {
                         Build(up, LogosGiveNegativeSwapRows.InsertData, LogosGiveNegativeSwapRows.DeleteData),
                         Build(up, TarapediaBossLevelsRows.InsertData, TarapediaBossLevelsRows.DeleteData)
                     })
            {
                Assert.IsTrue(migration.Operations.All(operation => operation is UpdateDataOperation));
                foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                {
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                }
            }
        }

        [TestMethod]
        public void EveryUpdatedValueMatchesItsManifestChange()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            foreach (var (name, insert, delete) in new (string, Action<MigrationBuilder>, Action<MigrationBuilder>)[]
                     {
                         (LogosGiveNegativeSwapRows.Migration, LogosGiveNegativeSwapRows.InsertData, LogosGiveNegativeSwapRows.DeleteData),
                         (TarapediaBossLevelsRows.Migration, TarapediaBossLevelsRows.InsertData, TarapediaBossLevelsRows.DeleteData)
                     })
            {
                var up = Values(Build(true, insert, delete));
                var down = Values(Build(false, insert, delete));
                var changes = root.GetProperty("changes").EnumerateArray().Where(c => c.GetProperty("migration").GetString() == name).ToList();
                Assert.AreEqual(up.Count, changes.Count, $"{name}: one manifest change per updated field");
                foreach (var change in changes)
                {
                    var key = (change.GetProperty("table").GetString(), change.GetProperty("key").GetProperty("id").GetUInt32(), change.GetProperty("field").GetString());
                    Assert.IsTrue(up.ContainsKey(key) && down.ContainsKey(key), $"{name}: {key} is not updated");
                    Assert.AreEqual(Convert.ToDecimal(up[key]), change.GetProperty("new").GetDecimal(), $"{name}: {key} new");
                    Assert.AreEqual(Convert.ToDecimal(down[key]), change.GetProperty("old").GetDecimal(), $"{name}: {key} old");
                    Assert.IsTrue(change.GetProperty("citations").GetArrayLength() > 0, $"{name}: {key} has no citation");
                    Assert.AreEqual("inferred", change.GetProperty("tier").GetString(), $"{name}: {key}");
                }
            }

            var decisions = root.GetProperty("owner_decisions").EnumerateArray().Select(d => d.GetProperty("id").GetString()).ToHashSet();
            foreach (var id in new[] { "OD-171", "OD-172" })
                Assert.IsTrue(decisions.Contains(id), id);
            var gaps = root.GetProperty("gaps").EnumerateArray().ToDictionary(g => g.GetProperty("id").GetString());
            foreach (var id in new[] { "GAP-DIVIDE-GIVE-NEGATIVE-FINAL", "GAP-UPSTREAM-BOSS-LEVELS", "GAP-UPSTREAM-BOSS-HP", "GAP-WILDERNESS-BOSS-LEVELS-PRE-D11" })
                Assert.AreEqual("open", gaps[id].GetProperty("status").GetString(), id);
        }

        [TestMethod]
        public void TheMigratedWorldLoadsTheCorrectedShrinesAndLevels()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            context.Database.ExecuteSqlRaw(SeedShrines);
            context.Database.Migrate();

            var logos = new LogosRepository(context).GetLogos().ToDictionary(l => l.Id);
            // Give, with its own Give stone, on the cave pedestal (745.24, 97.85, -5.21) of the client map; Negative on the
            // hilltop pedestal (184.09, 120.22, 137.75). Each stands 2.73 m above its pedestal and within 0.2 m of its axis, as the world seed did.
            Assert.AreEqual((7296u, 1148u, "Give"), (logos[15].ClassId, logos[15].MapContextId, logos[15].Name));
            Assert.AreEqual(745.24, logos[15].PosX, 0.2);
            Assert.AreEqual(97.85 + 2.73, logos[15].PosY, 0.2);
            Assert.AreEqual(-5.21, logos[15].PosZ, 0.2);
            Assert.AreEqual((12691u, 1148u, "Negative"), (logos[33].ClassId, logos[33].MapContextId, logos[33].Name));
            Assert.AreEqual(184.09, logos[33].PosX, 0.2);
            Assert.AreEqual(120.22 + 2.73, logos[33].PosY, 0.2);
            Assert.AreEqual(137.75, logos[33].PosZ, 0.2);

            var creatures = context.CreatureEntries.AsNoTracking().Where(c => c.Id >= 520000 && c.Id < 520100).ToDictionary(c => c.Id);
            foreach (var boss in Bosses)
            {
                Assert.AreEqual(boss.NameId, creatures[boss.Id].NameId, $"{boss.Id} is the boss its client name says");
                Assert.AreEqual(boss.Level, creatures[boss.Id].Level, $"{boss.Id} level");
                Assert.AreEqual(100 * boss.Was + 500, creatures[boss.Id].MaxHitPoints, $"{boss.Id} keeps upstream's health (OD-172)");
            }
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            context.Database.ExecuteSqlRaw(SeedShrines);
            var all = context.Database.GetMigrations().ToList();
            var first = all.Single(id => id.EndsWith("_" + LogosGiveNegativeSwapRows.Migration, StringComparison.Ordinal));
            var last = all.Single(id => id.EndsWith("_" + TarapediaBossLevelsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(first) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(12L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE " +
                string.Join(" OR ", Bosses.Select(b => $"(id = {b.Id} AND level = {b.Was})"))));

            migrator.Migrate(last);
            Assert.AreNotEqual(before, Snapshot(connection));
            migrator.Migrate(all[all.IndexOf(first) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only) still carries what the two migrations expect to find: both shrines on each other's
        /// pedestal with their own stone class, and each boss at upstream's level under its own client name.
        /// </summary>
        [TestMethod]
        public void TheDeployedWorldHoldsTheRowsTheFixesCorrect()
        {
            using var world = OpenWorld();
            // LogosGiveNegativeSwapRows.InsertData moves Give onto the cave pedestal and Negative onto the hilltop
            // pedestal (each takes the other's world-seed position); these two assertions had it backwards.
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM logos WHERE id = 15 AND name = 'Give' AND class_id = 7296 AND map_context_id = 1148 AND ABS(pos_x - 745.16797) < 0.001 AND ABS(pos_z + 5.2539062) < 0.001"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM logos WHERE id = 33 AND name = 'Negative' AND class_id = 12691 AND map_context_id = 1148 AND ABS(pos_x - 184.17969) < 0.001 AND ABS(pos_z - 137.64453) < 0.001"));
            foreach (var boss in Bosses)
                Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM creature WHERE id = {boss.Id} AND name_id = {boss.NameId} AND faction = 0 AND level IN ({boss.Was}, {boss.Level})"), $"{boss.Id}");
        }

        private static MigrationBuilder Build(bool up, Action<MigrationBuilder> insert, Action<MigrationBuilder> delete)
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            (up ? insert : delete)(migration);
            return migration;
        }

        private static Dictionary<(string, uint, string), object> Values(MigrationBuilder migration)
        {
            var values = new Dictionary<(string, uint, string), object>();
            foreach (var update in migration.Operations.OfType<UpdateDataOperation>())
            {
                var id = Convert.ToUInt32(update.KeyValues[0, 0]);
                for (var column = 0; column < update.Columns.Length; column++)
                    values.Add((update.Table, id, update.Columns[column]), update.Values[0, column]);
            }
            return values;
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "logos", "creature" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
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

        private static SqliteConnection OpenWorld()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            var path = directory == null ? null : Path.Combine(directory, "rasaworld.db");
            if (path == null || !File.Exists(path) || new FileInfo(path).Length == 0)
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
