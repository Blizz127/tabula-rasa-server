using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
using Rasa.Navigation;
using Rasa.Services.DbContext;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// DivideOperationsInstances: the rebuilt Minos Caverns and Timora Mines navmeshes join what the dossier found apart,
    /// every seeded position stands on navmesh floor its instance's arrival reaches, Tyler's 392 route and the Timora
    /// Warden are pathable, every seeded value is the one its manifest row records, the held content stays out, and the
    /// rollback gives the migrated world back exactly. Mission loading and each squad copy's placements are asserted on the
    /// migrated world in MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class DivideOperationsInstancesTests
    {
        private const string MinosNav = "adv_foreas_concordia_divide_minoscaverns.nav";
        private const string TimoraNav = "adv_foreas_concordia_divide_timoramines.nav";
        private const string TorcastraNav = "adv_foreas_concordia_divide_torcastraprison.nav";

        /// <summary>Each operation's arrival: where the instance door (MapLinkPreloader) puts a squad.</summary>
        private static readonly Vector3 MinosArrival = new(-15.822f, 29.5111f, 122.6093f);
        private static readonly Vector3 TimoraArrival = new(-120.049f, 205.6074f, 546.935f);
        private static readonly Vector3 TorcastraArrival = new(-304.0f, 13.75f, 286.75f);

        /// <summary>The places the dossier found on separate islands, and where the rebuilt meshes must reach.</summary>
        private static readonly (string What, Vector3 Point)[] MinosReached =
        {
            ("Scout Horlo's branch", new Vector3(-47f, 29.7f, 19f)),
            ("the Bane area start", new Vector3(148.7f, 4.3f, 50.1f)),
            ("Karik and Zola's hall", new Vector3(296.9f, 3.6f, 113.9f)),
            ("the Galactic Map chest point", new Vector3(159f, 3.5f, 70f)),
            ("Tyler's canyon", new Vector3(58f, -30.75f, -49f))
        };

        private static readonly (string What, Vector3 Point)[] TimoraReached =
        {
            ("the Fuel Egress ramp", new Vector3(282.9f, 202.7f, -245.6f)),
            ("the master pump", new Vector3(312.2f, 204.9f, -267.5f)),
            ("the warden building", new Vector3(414.8f, 231.6f, -206.1f)),
            ("the Command Center", new Vector3(470.5f, 231.75f, -200.06f)),
            ("Field Ranger Sanchez", new Vector3(56.6f, 145.6f, 50f))
        };

        [TestMethod]
        public void TheRebuiltNavMeshesJoinWhatTheDossierFoundApart()
        {
            var minos = Query(MinosNav);
            foreach (var (what, point) in MinosReached)
                Assert.IsTrue(Complete(minos, MinosArrival, point), $"Minos: {what} is reached from the arrival");
            // 392: Tyler walks from his canyon to the Field Medic at the way in.
            var medic = new Vector3((float)DivideOperationsInstancesRows.CaveEntrance.X, (float)DivideOperationsInstancesRows.CaveEntrance.Y,
                (float)DivideOperationsInstancesRows.CaveEntrance.Z);
            Assert.IsTrue(Complete(minos, new Vector3(58f, -30.5f, -49f), medic), "Tyler reaches the Field Medic");
            // Kept as original geometry: the Logos Those pit is a drop, the capped Bane control room has no door.
            Assert.IsFalse(Complete(minos, MinosArrival, new Vector3(26.13f, -32.5f, -51.12f)), "the Logos Those pit is a drop");
            Assert.IsFalse(Complete(minos, MinosArrival, new Vector3(57f, 12.9f, 166.75f)), "the sealed control room is sealed");

            var timora = Query(TimoraNav);
            foreach (var (what, point) in TimoraReached)
                Assert.IsTrue(Complete(timora, TimoraArrival, point), $"Timora: {what} is reached from the arrival");
            Assert.IsTrue(Complete(timora, new Vector3(-136.9f, 205.74f, 518.2f), new Vector3(56.6f, 145.74f, 50f)), "Kearney to Sanchez (1905)");

            // Torcastra was connected and is not rebuilt; the tower floor is on it.
            Assert.IsTrue(Complete(Query(TorcastraNav), TorcastraArrival, new Vector3(-80f, 122.06f, -156f)), "the command tower floor");
        }

        [TestMethod]
        public void EverySeededPositionStandsOnFloorItsArrivalReaches()
        {
            var meshes = new Dictionary<uint, (NavMeshQuery Query, Vector3 Arrival)>
            {
                [DivideOperationsInstancesRows.Minos] = (Query(MinosNav), MinosArrival),
                [DivideOperationsInstancesRows.Timora] = (Query(TimoraNav), TimoraArrival),
                [DivideOperationsInstancesRows.Torcastra] = (Query(TorcastraNav), TorcastraArrival)
            };
            foreach (var placement in DivideOperationsInstancesRows.Placements)
            {
                var (mesh, arrival) = meshes[placement.Map];
                var spot = new Vector3((float)placement.X, (float)placement.Y + 0.276f, (float)placement.Z);
                var surface = mesh.GroundHeight(spot);
                Assert.IsNotNull(surface, $"placement {placement.Id} has navmesh ground");
                Assert.AreEqual(surface.Value - 0.276, placement.Y, 0.01, $"placement {placement.Id} is not on its floor");
                Assert.IsTrue(Complete(mesh, arrival, spot), $"placement {placement.Id} is reached from the arrival");
            }
        }

        [TestMethod]
        public void TheNavMeshBuildListsBothFixes()
        {
            var root = RepositoryRoot();
            var cuts = File.ReadAllLines(Path.Combine(root, "src", "Rasa.NavMesh", "data", "terrain_cuts.csv"));
            Assert.AreEqual(1, cuts.Count(line => line.StartsWith("adv_foreas_concordia_divide_timoramines,arch_bane_industrial_chunnel_entrance_v01@242:-181,", StringComparison.Ordinal)));
            var grids = File.ReadAllLines(Path.Combine(root, "src", "Rasa.NavMesh", "data", "map_build_settings.csv"));
            Assert.AreEqual("map,cell_size,cell_height,reason", grids[0]);
            Assert.AreEqual(1, grids.Count(line => line.StartsWith("adv_foreas_concordia_divide_minoscaverns,0.2,0.1,", StringComparison.Ordinal)));
            Assert.AreEqual(2, grids.Length, "no other map changes grid");
        }

        [TestMethod]
        public void TheDossiersHeldContentStaysOut()
        {
            var seeded = DivideOperationsInstancesRows.Missions.Select(mission => mission.Id).ToList();
            CollectionAssert.AreEquivalent(new uint[] { 340, 1905, 792, 392 }, seeded);
            foreach (var held in new uint[] { 403, 404, 1276, 384, 391, 397, 594, 356, 1860, 1861, 383 })
                Assert.IsFalse(seeded.Contains(held), $"mission {held} is held");
            // 1905's escort objective is neither required nor revealed and nothing binds it (OD-146).
            var escort = DivideOperationsInstancesRows.Objectives.Single(o => o.Mission == 1905 && o.Objective == 1);
            Assert.IsFalse(escort.Required || escort.Revealed);
            Assert.IsFalse(DivideOperationsInstancesRows.Bindings.Any(binding => binding.Mission == 1905));
            // 392 has no prerequisite while 383 is not seeded (OD-147); 1905 follows 340.
            Assert.IsFalse(DivideOperationsInstancesRows.Prerequisites.Any(prerequisite => prerequisite.Mission == 392));
            Assert.AreEqual(340u, DivideOperationsInstancesRows.Prerequisites.Single(prerequisite => prerequisite.Mission == 1905).Required);
            // Only 392 pays, in currency; no item reward is seeded for any Divide mission.
            Assert.IsTrue(DivideOperationsInstancesRows.Rewards.All(reward => reward.Mission == 392));
            // No container, drill, pump, ambush or nest placement; the only usable is Hamilton's tube.
            Assert.AreEqual(DivideOperationsInstancesRows.StasisTube, DivideOperationsInstancesRows.Placements.Single(placement => placement.Kind == 2).Id);
            // Every new swapset NPC has a body (a bare one renders headless); Horlo's Forean class and the Bane bosses need none.
            foreach (var creature in DivideOperationsInstancesRows.Creatures.Where(creature => creature.Class is 3846 or 3848))
                Assert.IsTrue(DivideOperationsInstancesRows.Appearance.Count(piece => piece.Creature == creature.Id) >= 5, creature.Comment);
            // Q'uoa keeps his package: his own is not evidenced (GAP-QUOA-PACKAGE).
            Assert.IsFalse(DivideOperationsInstancesRows.Packages.Any(package => package.Id == 510104));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) DivideOperationsInstancesRows.InsertData(migration);
                else DivideOperationsInstancesRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                Assert.IsTrue(migration.Operations.OfType<InsertDataOperation>().Where(row => row.Columns.Contains("comment") && row.Table != "npc_mission_objective")
                    .All(row => Enumerable.Range(0, row.Values.GetLength(0)).All(index =>
                        Convert.ToString(row.Values[index, Array.IndexOf(row.Columns, "comment")])?.Length <= 50 || !up)), "comments fit varchar(50)");
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            DivideOperationsInstancesRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .Where(row => row.Table != "npc_mission_objective" || row.Values["is_required"] != null)
                // The swapset bodies are analogue sets outside the manifest's registry, as ContentNpcAppearance's (GAP-DIVIDE-NPC-APPEARANCE).
                .Where(row => row.Table != "creature_appearance")
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == DivideOperationsInstancesRows.Migration).ToList();
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
                    if (field.Value.GetProperty("tier").GetString() == "analogue")
                        CollectionAssert.Contains(new[] { "OD-145", "OD-150" }, field.Value.GetProperty("decision").GetString(), $"{table} {key}.{field.Name}");
                }
                row.TryGetProperty("storage_fields", out var storage);
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) ||
                                  (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty(column, out _)),
                        $"{table} {key}.{column} is seeded without provenance");
                if (storage.ValueKind == JsonValueKind.Object)
                    foreach (var column in storage.EnumerateObject())
                        Assert.IsTrue(Same(matches[0].Values[column.Name], column.Value.GetProperty("value")), $"{table} {key}.{column.Name} storage differs");

                // Health and attacks nobody records are analogues; every seeded amount carries its era.
                if (table == "creature")
                    Assert.AreEqual("analogue", fields.GetProperty("max_hp").GetProperty("tier").GetString(), $"{key}.max_hp");
                if (table == "npc_mission_reward")
                    Assert.AreEqual("pre-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
            }

            // The pools, Hamilton's package, Tyler's escort, the flags and both navmeshes are recorded changes.
            var changes = root.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() is { } name &&
                                 (name == DivideOperationsInstancesRows.Migration || name.Contains("divide_minoscaverns") || name.Contains("divide_timoramines")))
                .Select(change => change.GetProperty("table").GetString()).ToList();
            Assert.AreEqual(3, changes.Count(table => table == "spawnpool"));
            Assert.AreEqual(1, changes.Count(table => table == "npc_package"));
            Assert.AreEqual(1, changes.Count(table => table == "content_placement"));
            Assert.AreEqual(7, changes.Count(table => table == "npc_mission_objective"));
            Assert.AreEqual(2, changes.Count(table => table == "(none: navmesh)"));

            var gaps = root.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-MINOS-NAVMESH", "GAP-TIMORA-NAVMESH", "GAP-TORCASTRA-NAVMESH-TOWER", "GAP-DIVIDE-CREATURE-STATS", "GAP-TIMORA-ESCORT-SOLDIERS",
                         "GAP-QUOA-PACKAGE", "GAP-MINOS-CHEST-CLASS", "GAP-DIVIDE-392-PREREQUISITE", "GAP-DIVIDE-1860-1861-HELD", "GAP-DIVIDE-356-594-HELD",
                         "GAP-DIVIDE-NPC-APPEARANCE", "GAP-DIVIDE-403-DRILLS", "GAP-NAVMESH-MAP-GRID", "GAP-TORCASTRA-HAMILTON-RELEASE" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            var decisions = root.GetProperty("owner_decisions").EnumerateArray()
                .ToDictionary(decision => decision.GetProperty("id").GetString(), decision => decision.GetProperty("status").GetString());
            for (var number = 145; number <= 152; number++)
                Assert.AreEqual("approved", decisions[$"OD-{number}"], $"OD-{number}");
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + DivideOperationsInstancesRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id IN (510118, 510119, 510120) AND creature_1_min_count = 1 AND creature_1_max_count = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 510120 AND package_id = 1527"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (340, 1905, 792, 392)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 510120 AND package_id = 1526"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only), which predates this migration: the three pools draw their NPC once, Kearney
        /// carries 158 and Hamilton the computer bank's 1527, Tyler stands still, the client rows 340/2 and 1905/2 name
        /// Kearney's package, and the ids this migration allocates are free.
        /// </summary>
        [TestMethod]
        public void TheDeployedWorldHasWhatTheSeedStandsOn()
        {
            using var world = OpenWorld(RepositoryRoot());
            Assert.AreEqual(3L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE id IN (510118, 510119, 510120) AND creature_1_Id = id AND creature_1_min_count = 1 AND creature_1_max_count = 1"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM npc_package WHERE (id = 510118 AND package_id = 158) OR (id = 510120 AND package_id = 1527)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_package WHERE id = 510119 OR package_id IN (1526, 2380, 1156, 193)"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id = 199006 AND map_context_id = 1347 AND behavior = 1 AND escort_mission_id = 0"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id IN (340, 1905) AND objective_id = 2 AND npc_package_id = 158"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_mission WHERE id IN (340, 1905, 792, 392)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 1347000 AND 1349999"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 1347000 AND 1349999"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_area WHERE id BETWEEN 1347000 AND 1349999"));
        }

        private static NavMeshQuery Query(string file) => new(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", file)));

        private static bool Complete(NavMeshQuery mesh, Vector3 from, Vector3 to)
        {
            var path = mesh.FindPath(from, to, out var complete);
            return path != null && complete;
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var (table, filter) in new[]
                     {
                         ("npc_mission", ""), ("npc_mission_objective", ""), ("npc_mission_objective_binding", ""), ("npc_mission_objective_counter", ""),
                         ("npc_mission_reward", ""), ("npc_mission_prerequisite", ""), ("npc_package", ""), ("content_placement", ""), ("content_area", ""),
                         ("creature", " WHERE id >= 1347000"), ("creature_appearance", " WHERE id >= 1347000"), ("spawnpool", " WHERE id IN (510118, 510119, 510120)")
                     })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}{filter}";
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
