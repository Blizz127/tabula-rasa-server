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
    /// FootageAmbientPopulations: every pool (and the moved Warnet pool) stands on navmesh floor its zone's main waypoint
    /// reaches, each zone gets the species its footage shows, every seeded value is the one its manifest row records and every
    /// replaced analogue has its change entry, the rollback gives back the migrated world exactly, and what the footage does
    /// not place stays out. 1067's loading and offerability are asserted on the migrated world in MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class FootageAmbientPopulationsTests
    {
        /// <summary>The zones' main waypoints (client uimapmarker): Hightower Outpost, Irendas Penal Colony, Baylor Base.</summary>
        private static readonly Dictionary<uint, (string NavMesh, Vector3 Waypoint)> Zones = new()
        {
            [FootageAmbientPopulationsRows.Palisades] = ("adv_foreas_concordia_palisades.nav", new Vector3(-72.45f, 147.59f, 70.52f)),
            [FootageAmbientPopulationsRows.Plains] = ("adv_arieki_torden_plains.nav", new Vector3(306.16f, 430.59f, -166.14f)),
            [FootageAmbientPopulationsRows.Mires] = ("adv_arieki_torden_mires.nav", new Vector3(-647.09f, 219.99f, -504.9f))
        };

        [TestMethod]
        public void EveryPoolStandsOnFloorItsZonesWaypointReaches()
        {
            var queries = Zones.ToDictionary(zone => zone.Key, zone => new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", zone.Value.NavMesh))));
            double Moved(string column) => FootageAmbientPopulationsRows.Updates.Single(u => u.Table == "spawnpool" && u.Id == 1244201 && u.Column == column).New;
            var points = FootageAmbientPopulationsRows.Pools.Select(pool => ($"pool {pool.Id}", pool.Map, pool.X, pool.Y, pool.Z))
                .Append(("pool 1244201 (moved)", FootageAmbientPopulationsRows.Palisades, Moved("pos_x"), Moved("pos_y"), Moved("pos_z")));
            foreach (var (name, map, x, y, z) in points)
            {
                var query = queries[map];
                var ground = query.GroundHeight(new Vector3((float)x, (float)y + 0.276f, (float)z));
                Assert.IsNotNull(ground, $"{name} has no navmesh ground");
                Assert.AreEqual(ground.Value - 0.276, y, 0.3, $"{name} is not on its floor");
                query.FindPath(Zones[map].Waypoint, new Vector3((float)x, ground.Value, (float)z), out var complete);
                Assert.IsTrue(complete, $"{name} cannot be reached from its zone's waypoint");
            }
        }

        [TestMethod]
        public void EachZoneGetsTheSpeciesItsFootageShows()
        {
            var own = FootageAmbientPopulationsRows.Creatures.ToDictionary(creature => creature.Id, creature => creature.Class);
            // The pools also draw three of the previous batch's Palisades rows (Boargar, Hunter, Technician).
            var previous = new Dictionary<uint, uint> { [1244001] = 6031, [1244004] = 10166, [1244005] = 7043 };
            uint ClassOf(uint creature) => own.TryGetValue(creature, out var klass) ? klass : previous[creature];
            uint[] ClassesOn(uint map) => FootageAmbientPopulationsRows.Pools.Where(pool => pool.Map == map)
                .SelectMany(pool => pool.Slots).Select(slot => ClassOf(slot.Creature)).Distinct().ToArray();

            // Palisades after D12: Treemites, Boargar, Thrax grunts and Technicians, Caretakers, Hunters, Howlers.
            CollectionAssert.AreEquivalent(new uint[] { 6040, 6031, 29769, 7043, 9244, 10166, 7336 }, ClassesOn(FootageAmbientPopulationsRows.Palisades));
            // Torden Plains: Flaregashers, Striders, Hunters, Thrax, Howlers, Beam Mantas, Atta Harvesters and Soldiers, Technicians.
            CollectionAssert.AreEquivalent(new uint[] { 7338, 9804, 10166, 29769, 7336, 6441, 7078, 7081, 7043 }, ClassesOn(FootageAmbientPopulationsRows.Plains));
            // Torden Mires: Technicians, Scavengers, Kael, Caretakers, Lightbenders.
            CollectionAssert.AreEquivalent(new uint[] { 7043, 20768, 4046, 9244, 7120 }, ClassesOn(FootageAmbientPopulationsRows.Mires));

            Assert.AreEqual(6, FootageAmbientPopulationsRows.Pools.Count(pool => pool.Map == FootageAmbientPopulationsRows.Palisades));
            Assert.AreEqual(11, FootageAmbientPopulationsRows.Pools.Count(pool => pool.Map == FootageAmbientPopulationsRows.Plains));
            Assert.AreEqual(8, FootageAmbientPopulationsRows.Pools.Count(pool => pool.Map == FootageAmbientPopulationsRows.Mires));
            Assert.AreEqual(47, FootageAmbientPopulationsRows.Pools.Sum(pool => pool.Slots.Sum(slot => slot.Count)));
            // Every new creature row is drawn by some pool; creature ids stay in their map's reserved block.
            var drawn = FootageAmbientPopulationsRows.Pools.SelectMany(pool => pool.Slots).Select(slot => slot.Creature).ToHashSet();
            foreach (var creature in FootageAmbientPopulationsRows.Creatures)
                Assert.IsTrue(drawn.Contains(creature.Id), $"creature {creature.Id} is drawn by no pool");
            foreach (var pool in FootageAmbientPopulationsRows.Pools)
            {
                Assert.AreEqual(pool.Map, pool.Id / 1000, $"pool {pool.Id} lies in its map's block");
                Assert.IsTrue(pool.Slots.Length is > 0 and <= 6 && pool.Slots.All(slot => slot.Count > 0), $"pool {pool.Id}");
                Assert.IsTrue(pool.Slots.All(slot => slot.Creature / 1000 == pool.Map), $"pool {pool.Id} draws its own zone's creatures");
            }
            // 1067's two drops come from creatures the Mires pools draw.
            foreach (var binding in FootageAmbientPopulationsRows.Bindings)
                Assert.IsTrue(FootageAmbientPopulationsRows.Pools.Any(pool => pool.Map == FootageAmbientPopulationsRows.Mires && pool.Slots.Any(slot => slot.Creature == binding.Creature)), $"1067/{binding.Objective}");
            // Levels are the ones the footage reads: Palisades 15-18, Plains 20-25, Mires 28-30.
            foreach (var creature in FootageAmbientPopulationsRows.Creatures)
            {
                var (low, high) = (creature.Id / 1000) switch { 1244 => (15u, 18u), 1764 => (20u, 25u), _ => (28u, 30u) };
                Assert.IsTrue(creature.Level >= low && creature.Level <= high, $"creature {creature.Id} level {creature.Level}");
            }
        }

        [TestMethod]
        public void ThePreviousAnaloguesAreReplacedByWhatTheFootageShows()
        {
            var expected = new (string Table, uint Id, string Column, double Old, double New)[]
            {
                ("creature", 1244001, "level", 15, 18), ("creature", 1244001, "name_id", 0, 7805),
                ("creature", 1244002, "level", 15, 16), ("creature", 1244002, "name_id", 460, 7957),
                ("creature", 1244004, "level", 15, 16), ("creature", 1244004, "name_id", 0, 8249),
                ("creature", 1244005, "level", 15, 17), ("creature", 1244005, "name_id", 9106, 8057),
                ("spawnpool", 1244201, "pos_x", 60, 419.2), ("spawnpool", 1244201, "pos_y", 113.235, 109.767), ("spawnpool", 1244201, "pos_z", 50, 368.8),
                ("spawnpool", 1244201, "creature_1_min_count", 3, 1), ("spawnpool", 1244201, "creature_1_max_count", 3, 1)
            };
            CollectionAssert.AreEquivalent(expected, FootageAmbientPopulationsRows.Updates);
            // The old values are the previous batch's own rows.
            var previousCreatures = ConcordiaAmbientPopulationsRows.Creatures.ToDictionary(creature => creature.Id);
            foreach (var update in FootageAmbientPopulationsRows.Updates.Where(u => u.Table == "creature"))
                Assert.AreEqual(update.Old, (double)(update.Column == "level" ? previousCreatures[update.Id].Level : previousCreatures[update.Id].NameId), $"{update.Id}.{update.Column}");
            var warnets = ConcordiaAmbientPopulationsRows.Pools.Single(pool => pool.Id == 1244201);
            Assert.AreEqual((60.0, 113.235, 50.0, (byte)3), (warnets.X, warnets.Y, warnets.Z, warnets.Slots.Single().Count));
            // Executor Gantic already stands at level 18, as filmed: no update.
            Assert.IsFalse(FootageAmbientPopulationsRows.Updates.Any(update => update.Id == 199054));
        }

        [TestMethod]
        public void WhatTheFootageDoesNotPlaceStaysOut()
        {
            var names = FootageAmbientPopulationsRows.Creatures.Select(creature => creature.Comment).ToList();
            foreach (var held in new[] { "Predator", "Shield Drone", "Crab Mine", "Ordnance", "Medico", "Corporal", "Warrant", "Pharmacologist", "Qraal", "Harmox", "Barb Tick", "Stalker" })
                Assert.IsFalse(names.Any(name => name.Contains(held)), held);
            // Only Palisades, the Plains and the Mires: Abyss (2028), Plateau (1497) and Howling Maw (2051) get no pool.
            CollectionAssert.AreEquivalent(new uint[] { 1244, 1764, 1759 }, FootageAmbientPopulationsRows.Pools.Select(pool => pool.Map).Distinct().ToArray());
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            FootageAmbientPopulationsRows.InsertData(migration);
            // 1067 alone: no prerequisite on the unseeded 975 (OD-180), no item reward, no other mission.
            Assert.IsFalse(migration.Operations.OfType<InsertDataOperation>().Any(operation => operation.Table == "npc_mission_prerequisite"));
            Assert.IsTrue(FootageAmbientPopulationsRows.Rewards.All(reward => reward.Type == 1 || reward.Type == 3));
            var missions = migration.Operations.OfType<InsertDataOperation>().Where(operation => operation.Table == "npc_mission").ToList();
            Assert.AreEqual(1067u, missions.Single().Values[0, 0]);
            Assert.AreEqual(1, missions.Single().Values.GetLength(0));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) FootageAmbientPopulationsRows.InsertData(migration);
                else FootageAmbientPopulationsRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                    Assert.AreEqual((row.KeyColumns.Length, row.Columns.Length), (row.KeyColumnTypes?.Length ?? 0, row.ColumnTypes?.Length ?? 0), row.Table);
                Assert.AreEqual(FootageAmbientPopulationsRows.Updates.Length, migration.Operations.OfType<UpdateDataOperation>().Count());
                Assert.IsTrue(migration.Operations.OfType<InsertDataOperation>().Where(row => row.Columns.Contains("comment") && row.Table != "npc_mission_objective")
                    .All(row => Enumerable.Range(0, row.Values.GetLength(0)).All(index =>
                        Convert.ToString(row.Values[index, Array.IndexOf(row.Columns, "comment")])?.Length <= 50)), "comments fit varchar(50)");
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRowAndEveryReplacementItsChange()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            FootageAmbientPopulationsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == FootageAmbientPopulationsRows.Migration).ToList();
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

                // Levels are read from the footage; statistics and movement are analogues under OD-178.
                if (table == "creature")
                {
                    Assert.AreEqual("observed", fields.GetProperty("level").GetProperty("tier").GetString(), $"{key}.level");
                    foreach (var column in new[] { "max_hp", "action1", "run_speed", "walk_speed" })
                        Assert.AreEqual(("analogue", "OD-178"), (fields.GetProperty(column).GetProperty("tier").GetString(), fields.GetProperty(column).GetProperty("decision").GetString()), $"{key}.{column}");
                }
                // Places are measured from the radar (+/-50 m) and the navmesh floor.
                if (table == "spawnpool")
                    foreach (var column in new[] { "pos_x", "pos_y", "pos_z" })
                        Assert.AreEqual("measured", fields.GetProperty(column).GetProperty("tier").GetString(), $"{key}.{column}");
                if (table == "spawnpool")
                    Assert.AreEqual(50, fields.GetProperty("pos_x").GetProperty("uncertainty").GetProperty("horizontal_m").GetInt32(), $"{key}.pos_x");
                if (table == "npc_mission_reward")
                    Assert.AreEqual("post-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
            }
            foreach (var tier in new[] { "original", "observed", "measured", "inferred", "analogue" })
                Assert.IsTrue(tiers.GetValueOrDefault(tier) > 0, tier);

            // Every replaced analogue has a change entry with the same old and new value.
            var changes = document.RootElement.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() == FootageAmbientPopulationsRows.Migration && change.GetProperty("key").TryGetProperty("id", out _)).ToList();
            foreach (var update in FootageAmbientPopulationsRows.Updates)
            {
                var change = changes.Single(c => c.GetProperty("table").GetString() == update.Table && c.GetProperty("key").GetProperty("id").GetUInt32() == update.Id
                                                  && c.GetProperty("field").GetString() == update.Column);
                Assert.AreEqual((decimal)update.Old, change.GetProperty("old").GetDecimal(), $"{update.Id}.{update.Column}");
                Assert.AreEqual((decimal)update.New, change.GetProperty("new").GetDecimal(), $"{update.Id}.{update.Column}");
                Assert.IsTrue(change.GetProperty("citations").GetArrayLength() > 0);
            }
            Assert.AreEqual(FootageAmbientPopulationsRows.Updates.Length, changes.Count);

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-FOOTAGE-POOL-COUNTS", "GAP-FOOTAGE-CREATURE-STATS", "GAP-FOOTAGE-POOL-RESPAWN", "GAP-FOOTAGE-UNPLACED-SPECIES",
                         "GAP-1067-PREREQUISITE", "GAP-1067-REWARD-ITEMS", "GAP-PALISADES-368-WARNET-DISTANCE", "GAP-COLLECTION-DROP-CHANCE", "GAP-MISSION-LEVEL" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            var decisions = document.RootElement.GetProperty("owner_decisions").EnumerateArray().Select(decision => decision.GetProperty("id").GetString()).ToHashSet();
            for (var id = 176; id <= 181; id++)
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
            var self = all.Single(id => id.EndsWith("_" + FootageAmbientPopulationsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE map_context_id IN (1764, 1759) AND id BETWEEN 1759200 AND 1764299"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(25L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id BETWEEN 1244208 AND 1244213 OR id BETWEEN 1764200 AND 1764210 OR id BETWEEN 1759200 AND 1759207"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 1244002 AND level = 16 AND name_id = 7957"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "creature", "spawnpool", "npc_mission", "npc_mission_objective", "npc_mission_objective_binding",
                         "npc_mission_objective_counter", "npc_mission_reward", "npc_mission_prerequisite" })
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
