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
using Rasa.Migrations.WildernessData;
using Rasa.Navigation;
using Rasa.Services.DbContext;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// ConcordiaAmbientPopulations: every pool stands on navmesh floor its zone's main waypoint reaches, each zone gets the
    /// species its evidence names, every seeded value is the one its manifest row records, the rollback gives back the
    /// migrated world exactly, and what the evidence does not support stays out. Loading and offerability of the missions are
    /// asserted on the migrated world in MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ConcordiaAmbientPopulationsTests
    {
        /// <summary>The zones' main waypoints (client uimapmarker): Foreas Base and Hightower Outpost.</summary>
        private static readonly Dictionary<uint, (string NavMesh, Vector3 Waypoint)> Zones = new()
        {
            [ConcordiaAmbientPopulationsRows.Divide] = ("adv_foreas_concordia_divide.nav", new Vector3(-33.55f, 116.18f, 564.44f)),
            [ConcordiaAmbientPopulationsRows.Palisades] = ("adv_foreas_concordia_palisades.nav", new Vector3(-72.45f, 147.59f, 70.52f))
        };

        [TestMethod]
        public void EveryPoolAndAreaStandsOnFloorItsZonesWaypointReaches()
        {
            var queries = Zones.ToDictionary(zone => zone.Key, zone => new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", zone.Value.NavMesh))));
            var points = ConcordiaAmbientPopulationsRows.Pools.Select(pool => ($"pool {pool.Id}", pool.Map, pool.X, pool.Y, pool.Z))
                .Concat(ConcordiaAmbientPopulationsRows.Areas.Select(area => ($"area {area.Id}", area.Map, area.X, area.Y, area.Z)));
            foreach (var (name, map, x, y, z) in points)
            {
                var query = queries[map];
                var spot = new Vector3((float)x, (float)y + 0.276f, (float)z);
                var ground = query.GroundHeight(spot);
                Assert.IsNotNull(ground, $"{name} has no navmesh ground");
                // Seeded heights are the floor minus the 0.276 m the original spawns sit at.
                Assert.AreEqual(ground.Value - 0.276, y, 0.3, $"{name} is not on its floor");
                query.FindPath(Zones[map].Waypoint, new Vector3((float)x, ground.Value, (float)z), out var complete);
                Assert.IsTrue(complete, $"{name} cannot be reached from its zone's waypoint");
            }
        }

        [TestMethod]
        public void EachZoneGetsTheSpeciesItsEvidenceNames()
        {
            var classes = ConcordiaAmbientPopulationsRows.Creatures.ToDictionary(creature => creature.Id, creature => creature.Class);
            IEnumerable<uint> ClassesOn(uint map) => ConcordiaAmbientPopulationsRows.Pools.Where(pool => pool.Map == map)
                .SelectMany(pool => pool.Slots).Select(slot => classes[slot.Creature]).Distinct();

            // The Divide: Xanx, Thrax Infantry (the Targets of Opportunity's own), Caretakers, Filchers, Warnets, Class IV Stalkers.
            CollectionAssert.AreEquivalent(new uint[] { 7510, 29769, 9244, 6421, 6262, 3781 }, ClassesOn(ConcordiaAmbientPopulationsRows.Divide).ToArray());
            // Palisades after D12: Fithik and Hunters (1809), 342's Boargar, 368's Warnets, 406's Thrax Technicians - and no Stalker,
            // which 1809 dropped from 1630's list.
            CollectionAssert.AreEquivalent(new uint[] { 6031, 6262, 4313, 10166, 7043 }, ClassesOn(ConcordiaAmbientPopulationsRows.Palisades).ToArray());

            Assert.AreEqual(14, ConcordiaAmbientPopulationsRows.Pools.Count(pool => pool.Map == ConcordiaAmbientPopulationsRows.Divide));
            Assert.AreEqual(8, ConcordiaAmbientPopulationsRows.Pools.Count(pool => pool.Map == ConcordiaAmbientPopulationsRows.Palisades));
            Assert.AreEqual(60, ConcordiaAmbientPopulationsRows.Pools.Sum(pool => pool.Slots.Sum(slot => slot.Count)));
            // Only the Front Lines squad arrives by Bane dropship (the footage's drop); only the Stalkers wait 15 minutes.
            CollectionAssert.AreEqual(new[] { ConcordiaAmbientPopulationsRows.FrontLines },
                ConcordiaAmbientPopulationsRows.Pools.Where(pool => pool.Anim == 1).Select(pool => pool.Id).ToArray());
            CollectionAssert.AreEquivalent(new[] { ConcordiaAmbientPopulationsRows.StalkerFoxtrotBridge, ConcordiaAmbientPopulationsRows.StalkerForwardBase },
                ConcordiaAmbientPopulationsRows.Pools.Where(pool => pool.Respawn != ConcordiaAmbientPopulationsRows.PoolRespawn).Select(pool => pool.Id).ToArray());
            Assert.AreEqual(9000u, ConcordiaAmbientPopulationsRows.StalkerRespawn);

            // Each kill or drop binding names a creature some pool of the objective's zone draws, or a boss pool of the world seed.
            var drawn = ConcordiaAmbientPopulationsRows.Pools.SelectMany(pool => pool.Slots).Select(slot => slot.Creature).ToHashSet();
            foreach (var binding in ConcordiaAmbientPopulationsRows.Bindings.Where(binding => binding.Creature != 0))
                Assert.IsTrue(drawn.Contains(binding.Creature) || binding.Creature == ConcordiaAmbientPopulationsRows.RottingSal || binding.Creature == ConcordiaAmbientPopulationsRows.Shahrbaraz,
                    $"{binding.Mission}/{binding.Objective}");
            // Every counted objective can be finished before its pool empties for good: pools respawn.
            Assert.IsTrue(ConcordiaAmbientPopulationsRows.Pools.All(pool => pool.Respawn > 0));
        }

        [TestMethod]
        public void WhatTheEvidenceDoesNotSupportStaysOut()
        {
            var names = ConcordiaAmbientPopulationsRows.Creatures.Select(creature => creature.Comment).ToList();
            // No Predator (no final-era evidence), no Hominis Machina or Amoeboid group (no place recorded), no Juggernaut.
            Assert.IsFalse(names.Any(name => name.Contains("Predator") || name.Contains("Hominis") || name.Contains("Amoeboid") || name.Contains("Juggernaut")));
            // Missions whose giver is not in the world, and the Targets of Opportunity, are not defined.
            foreach (var held in new uint[] { 370, 346, 348, 326, 688, 373, 374, 1582, 1809, 344, 351, 353, 354, 355, 363, 405, 406 })
                Assert.IsFalse(ConcordiaAmbientPopulationsRows.Missions.Any(mission => mission.Id == held), $"mission {held}");
            // 371 is offered without 370 (OD-165); 372 waits on 371 and 755 on 774.
            Assert.IsFalse(ConcordiaAmbientPopulationsRows.Prerequisites.Any(prerequisite => prerequisite.Mission == ConcordiaAmbientPopulationsRows.DissectionsTwo));
            CollectionAssert.AreEquivalent(new[] { (372u, 371u), (755u, 774u) },
                ConcordiaAmbientPopulationsRows.Prerequisites.Select(prerequisite => (prerequisite.Mission, prerequisite.Required)).ToArray());
            // 1808's Start/Middle/End are optional and unrevealed, and nothing binds them (OD-169).
            foreach (var objective in new uint[] { 3, 4, 5 })
            {
                var row = ConcordiaAmbientPopulationsRows.Objectives.Single(o => o.Mission == 1808 && o.Objective == objective);
                Assert.IsFalse(row.Required || row.Revealed, $"1808/{objective}");
                Assert.IsFalse(ConcordiaAmbientPopulationsRows.Bindings.Any(binding => binding.Mission == 1808 && binding.Objective == objective));
            }
            // 368 gets its kill, not a timer; its first-conversation row on Kogari is withheld.
            Assert.IsFalse(ConcordiaAmbientPopulationsRows.Objectives.Any(objective => objective.Mission == ConcordiaAmbientPopulationsRows.SearchingForAcceptance));
            Assert.IsTrue(MissionRedirectConversations.IsWithheld(368, 1, 44));
            Assert.IsFalse(MissionRedirectConversations.IsWithheld(368, 2, 44), "368/2 still completes on Kogari");
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            ConcordiaAmbientPopulationsRows.InsertData(migration);
            Assert.IsFalse(migration.Operations.OfType<InsertDataOperation>().Any(operation => operation.Table == "npc_mission_objective_timer"));
            // No item reward: every list was first recorded before Update 1.4.
            Assert.IsTrue(ConcordiaAmbientPopulationsRows.Rewards.All(reward => reward.Type == 1 || reward.Type == 3));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) ConcordiaAmbientPopulationsRows.InsertData(migration);
                else ConcordiaAmbientPopulationsRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                Assert.IsTrue(migration.Operations.OfType<InsertDataOperation>().Where(row => row.Columns.Contains("comment") && row.Table != "npc_mission_objective")
                    .All(row => Enumerable.Range(0, row.Values.GetLength(0)).All(index =>
                        Convert.ToString(row.Values[index, Array.IndexOf(row.Columns, "comment")])?.Length <= 50)), "comments fit varchar(50)");
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            ConcordiaAmbientPopulationsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == ConcordiaAmbientPopulationsRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            var tiers = new Dictionary<string, int>();
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
                    var tier = field.Value.GetProperty("tier").GetString();
                    tiers[tier] = tiers.GetValueOrDefault(tier) + 1;
                }
                row.TryGetProperty("storage_fields", out var storage);
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) ||
                                  (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty(column, out _)),
                        $"{table} {key}.{column} is seeded without provenance");
                if (storage.ValueKind == JsonValueKind.Object)
                    foreach (var column in storage.EnumerateObject())
                        Assert.IsTrue(Same(matches[0].Values[column.Name], column.Value.GetProperty("value")), $"{table} {key}.{column.Name} storage differs");

                // Levels, statistics and movement are analogues under their decisions; every amount carries its era.
                if (table == "creature")
                {
                    Assert.AreEqual(("analogue", "OD-162"), (fields.GetProperty("level").GetProperty("tier").GetString(), fields.GetProperty("level").GetProperty("decision").GetString()), $"{key}.level");
                    foreach (var column in new[] { "max_hp", "action1", "run_speed", "walk_speed" })
                        Assert.AreEqual(("analogue", "OD-163"), (fields.GetProperty(column).GetProperty("tier").GetString(), fields.GetProperty(column).GetProperty("decision").GetString()), $"{key}.{column}");
                }
                if (table == "spawnpool")
                    Assert.AreEqual("measured", fields.GetProperty("pos_y").GetProperty("tier").GetString(), $"{key}.pos_y is the probed floor");
                if (table == "npc_mission_reward")
                    Assert.AreEqual("pre-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
            }
            // Footage, measurement and text each carry part of the reconstruction; nothing claims to be original data it is not.
            foreach (var tier in new[] { "original", "observed", "measured", "inferred", "analogue" })
                Assert.IsTrue(tiers.GetValueOrDefault(tier) > 0, tier);

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-CONCORDIA-AMBIENT-LEVELS", "GAP-CONCORDIA-CREATURE-STATS", "GAP-CONCORDIA-POOL-COUNTS", "GAP-CONCORDIA-POOL-RESPAWN",
                         "GAP-CONCORDIA-POOL-PLACES", "GAP-DIVIDE-HOMINIS-AMOEBOID", "GAP-DIVIDE-PREDATORS", "GAP-DIVIDE-371-PREREQUISITE", "GAP-DIVIDE-372-MICROMECH",
                         "GAP-DIVIDE-ROTTING-SAL-POSITION", "GAP-1808-TRAVERSE", "GAP-UHERUM-NAVMESH", "GAP-PALISADES-342-OBELISK-ERA", "GAP-CONCORDIA-FOOTAGE-DOWNLOADS",
                         "GAP-PALISADES-368-KILL-TIMER", "GAP-PALISADES-AMBIENT-POPULATION", "GAP-DIVIDE-AMBIENT-POPULATION", "GAP-REWARD-ERA", "GAP-COLLECTION-DROP-CHANCE" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            var decisions = document.RootElement.GetProperty("owner_decisions").EnumerateArray().Select(decision => decision.GetProperty("id").GetString()).ToHashSet();
            for (var id = 161; id <= 170; id++)
                Assert.IsTrue(decisions.Contains($"OD-{id}"), $"OD-{id}");
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + ConcordiaAmbientPopulationsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 1148200 AND 1148299 OR id BETWEEN 1244200 AND 1244299"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(22L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 1148200 AND 1148299 OR id BETWEEN 1244200 AND 1244299"));
            Assert.AreEqual(7L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (358, 371, 372, 774, 755, 342, 1808)"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "creature", "spawnpool", "content_area", "npc_package", "npc_mission", "npc_mission_objective", "npc_mission_objective_transition",
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
