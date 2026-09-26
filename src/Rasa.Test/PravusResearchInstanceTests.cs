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
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Migrations.WildernessData;
using Rasa.Navigation;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// PravusResearchInstance: every seeded value is the one its manifest row records, the rollback gives back the
    /// migrated world exactly, the rebuilt navmesh joins the entrance to every room the content stands in, every squad
    /// copy of 1430 gets its own copy of the population, and the dossier's held content stays out. Loading and
    /// offerability are asserted on the migrated world in MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PravusResearchInstanceTests
    {
        private const string PravusNavMesh = "adv_foreas_concordia_wilderness_pravusresearch.nav";

        /// <summary>The instance's hospital marker (HospitalCatalog, client uimapmarker), where a squad arrives.</summary>
        private static readonly Vector3 Hospital = new Vector3(-237.95f, 6.82f, -125.18f);

        /// <summary>The original map's geometry: the tunnel jamb, the tunnel cross below the ramp, the control room floor,
        /// the chamber centre, the prison chunnel and the Control Rooms gangplank.</summary>
        private static readonly (string Name, Vector3 Point)[] Rooms =
        {
            ("entrance jamb", new Vector3(176f, 41.8f, -8f)),
            ("Interior Halls tunnel cross", new Vector3(224f, 9.9f, -24f)),
            ("Control Rooms", new Vector3(214f, 8.1f, 40f)),
            ("Production Chamber", new Vector3(224f, 8.6f, 168f)),
            ("prison chunnel", new Vector3(312f, 9.9f, 56f)),
            ("gangplank", new Vector3(190f, 17.9f, 120f))
        };

        [TestMethod]
        public void TheRebuiltNavMeshJoinsTheEntranceToEveryRoom()
        {
            var query = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", PravusNavMesh)));

            // Room to room, in the order the footage walks them, and each room from the camp.
            for (var i = 0; i + 1 < Rooms.Length; i++)
            {
                var path = query.FindPath(Rooms[i].Point, Rooms[i + 1].Point, out var complete);
                Assert.IsNotNull(path, $"{Rooms[i].Name} -> {Rooms[i + 1].Name}");
                Assert.IsTrue(complete, $"{Rooms[i].Name} -> {Rooms[i + 1].Name} is partial");
            }
            foreach (var (name, point) in Rooms)
            {
                query.FindPath(Hospital, point, out var complete);
                Assert.IsTrue(complete, $"hospital -> {name} is partial");
            }
        }

        [TestMethod]
        public void EveryCreatureTheSeedPlacesStandsOnGroundTheHospitalReaches()
        {
            var query = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", PravusNavMesh)));
            var points = PravusResearchInstanceRows.Pools.Select(pool => ($"pool {pool.Id}", pool.X, pool.Y, pool.Z))
                .Concat(PravusResearchInstanceRows.Placements
                    .Where(placement => placement.Kind == 1)
                    .Select(placement => ($"placement {placement.Id}", placement.X, placement.Y, placement.Z)))
                .Append(("Johnson", PravusResearchInstanceRows.JohnsonNow.X, PravusResearchInstanceRows.JohnsonNow.Y, PravusResearchInstanceRows.JohnsonNow.Z));
            foreach (var (name, x, y, z) in points)
            {
                var spot = new Vector3((float)x, (float)y, (float)z);
                var ground = query.GroundHeight(spot);
                Assert.IsNotNull(ground, $"{name} has no navmesh ground");
                // Seeded heights are the floor minus the 0.276 m the original spawns sit at.
                Assert.AreEqual(ground.Value - 0.276, y, 0.3, $"{name} is not on its floor");
                query.FindPath(Hospital, spot, out var complete);
                Assert.IsTrue(complete, $"{name} cannot be reached from the hospital");
            }
        }

        [TestMethod]
        public void EverySquadCopyGetsItsOwnPopulation()
        {
            const uint Pravus = PravusResearchInstanceRows.Pravus;
            var maps = new MapChannelManager(null, () => 0) { IsSquadContext = contextId => contextId == Pravus };
            maps.MapChannelArray.Add(Pravus, new MapChannel { MapInfo = new MapInfo(Pravus, "adv_foreas_concordia_wilderness_pravusresearch", 555, 0), ClientList = new List<Client>() });

            // The pools as SpawnPoolManager.LoadSpawnPools builds them from the seeded rows.
            var pools = PravusResearchInstanceRows.Pools.Select(row => new SpawnPool
            {
                DbId = row.Id, MapContextId = Pravus, AnimType = row.Anim, Position = new Vector3((float)row.X, (float)row.Y, (float)row.Z),
                RespawnTime = PravusResearchInstanceRows.PoolRespawn * 100, UpdateTimer = PravusResearchInstanceRows.PoolRespawn * 100,
                SpawnSlot = row.Slots.Select(slot => new SpawnPoolSlot(slot.Creature, slot.Count, slot.Count)).ToList()
            }).ToList();
            foreach (var pool in pools)
                SpawnPoolManager.Instance.LoadedSpawnPools.Add(pool.DbId, pool);

            try
            {
                var first = maps.ChannelForEntry(101, Pravus, 7);
                var second = maps.ChannelForEntry(102, Pravus, 8);
                Assert.AreNotSame(first, second);
                foreach (var copy in new[] { first, second })
                {
                    CollectionAssert.AreEquivalent(pools.Select(pool => pool.DbId).ToArray(), copy.SpawnPools.Select(pool => pool.DbId).ToArray());
                    foreach (var own in copy.SpawnPools)
                    {
                        var template = pools.Single(pool => pool.DbId == own.DbId);
                        Assert.AreNotSame(template, own);
                        Assert.AreSame(copy, own.MapChannel);
                        Assert.AreEqual(0, own.AliveCreatures, "a copy starts with its own counters");
                        CollectionAssert.AreEqual(template.SpawnSlot.Select(slot => (slot.CreatureId, slot.CountMin)).ToArray(),
                            own.SpawnSlot.Select(slot => (slot.CreatureId, slot.CountMin)).ToArray());
                    }
                }
                // Twelve Frontlines attackers, arriving by Bane dropship, and 38 creatures in all per copy.
                Assert.AreEqual(1, first.SpawnPools.Single(pool => pool.DbId == PravusResearchInstanceRows.FrontlinesPool).AnimType);
                Assert.AreEqual(12, first.SpawnPools.Single(pool => pool.DbId == PravusResearchInstanceRows.FrontlinesPool).SpawnSlot.Sum(slot => slot.CountMin));
                Assert.AreEqual(38, first.SpawnPools.Sum(pool => pool.SpawnSlot.Sum(slot => slot.CountMin)));
            }
            finally
            {
                foreach (var pool in pools)
                    SpawnPoolManager.Instance.LoadedSpawnPools.Remove(pool.DbId);
            }
        }

        [TestMethod]
        public void TheDossiersHeldContentStaysOut()
        {
            // 574 has no giver and no Wilderness source for its remains; 593 waits on nothing until it does (OD-139).
            Assert.IsFalse(PravusResearchInstanceRows.Missions.Any(mission => mission.Id == 574));
            Assert.IsFalse(PravusResearchInstanceRows.Prerequisites.Any(prerequisite => prerequisite.Mission == PravusResearchInstanceRows.TheEscapist));
            // 575's bonus (Maulis) and ambush objectives are neither required nor revealed, and nothing binds them.
            foreach (var objective in new uint[] { 4, 5 })
            {
                var row = PravusResearchInstanceRows.Objectives.Single(o => o.Mission == 575 && o.Objective == objective);
                Assert.IsFalse(row.Required || row.Revealed, $"575/{objective}");
                Assert.IsFalse(PravusResearchInstanceRows.Bindings.Any(binding => binding.Mission == 575 && binding.Objective == objective));
            }
            // D11 removed the Juggernaut and the Predators; no other boss than Tarmok is placed.
            var names = PravusResearchInstanceRows.Creatures.Select(creature => creature.Comment).ToList();
            Assert.IsFalse(names.Any(name => name.Contains("Juggernaut") || name.Contains("Predator") || name.Contains("Prion")));
            Assert.AreEqual(1, PravusResearchInstanceRows.Creatures.Count(creature => creature.Class == 10504));
            // Nothing waits on a presence condition: a squad copy has no owner to evaluate one.
            Assert.IsTrue(PravusResearchInstanceRows.Placements.All(placement => placement.Kind == 2 || placement.Creature != 0));
            // Nylla's and Johnson's client rows for the content-bound objectives are withheld from the definitions.
            foreach (var (mission, objective, package) in new (uint, uint, uint)[] { (575, 1, 420), (575, 2, 420), (323, 309, 106), (323, 310, 106), (323, 311, 106) })
                Assert.IsTrue(MissionRedirectConversations.IsWithheld(mission, objective, package), $"{mission}/{objective}");
            Assert.IsFalse(MissionRedirectConversations.IsWithheld(593, 1, 420), "593 still completes on Nylla");
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) PravusResearchInstanceRows.InsertData(migration);
                else PravusResearchInstanceRows.DeleteData(migration);
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
            PravusResearchInstanceRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == PravusResearchInstanceRows.Migration).ToList();
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

                // The creature statistics are analogues under OD-135; every seeded amount carries its era.
                if (table == "creature")
                    foreach (var column in new[] { "max_hp", "action1", "run_speed", "walk_speed" })
                        Assert.AreEqual(("analogue", "OD-135"), (fields.GetProperty(column).GetProperty("tier").GetString(), fields.GetProperty(column).GetProperty("decision").GetString()), $"{key}.{column}");
                if (table == "npc_mission_reward")
                    Assert.AreEqual("pre-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
            }

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-PRAVUS-CREATURE-STATS", "GAP-PRAVUS-PRODUCTION-RATE", "GAP-PRAVUS-PRODUCTION-STOP", "GAP-PRAVUS-CAPSULE-HP",
                         "GAP-PRAVUS-INFESTATION-HP", "GAP-PRAVUS-AMBUSH", "GAP-PRAVUS-MAULIS", "GAP-PRAVUS-574-HELD", "GAP-PRAVUS-INTERIOR-LAYOUT",
                         "GAP-PRAVUS-NYLLA-APPEARANCE", "GAP-PARSONS-POSITION", "GAP-PRAVUS-ENTRANCE-FORCEFIELD", "GAP-NAVMESH-TERRAIN-CUTS" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            var decisions = document.RootElement.GetProperty("owner_decisions").EnumerateArray().Select(decision => decision.GetProperty("id").GetString()).ToHashSet();
            foreach (var decision in new[] { "OD-135", "OD-136", "OD-137", "OD-138", "OD-139" })
                Assert.IsTrue(decisions.Contains(decision), decision);
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + PravusResearchInstanceRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199016 AND pos_x = -83.0"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (593, 575, 323, 924)"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "creature", "spawnpool", "content_area", "content_placement", "npc_package", "npc_mission", "npc_mission_objective",
                         "npc_mission_objective_binding", "npc_mission_objective_counter", "npc_mission_reward", "npc_mission_prerequisite" })
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
