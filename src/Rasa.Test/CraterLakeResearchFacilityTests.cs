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
    /// WildernessCraterLakeResearchFacility: the seed is the dossier's seedable set minus what the navmesh holds, every
    /// seeded value is the one its manifest row records, every position stands on navmesh floor joined to the instance
    /// entrance, a squad copy of 1721 carries the shrines 960 waits on, and the rollback gives the world back exactly.
    /// Mission loading and the placements a copy materializes are asserted on the migrated world in
    /// MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CraterLakeResearchFacilityTests
    {
        private const uint CraterLake = 1721;

        /// <summary>map_link 15 (Wilderness -> Crater Lake) arrives here: the instance entrance.</summary>
        private static readonly Vector3 Entrance = new(8.5096f, 139.9557f, 158.2927f);

        /// <summary>Written out independently of the Rows class: (what, x, y, z) of every seeded position.</summary>
        private static readonly (string What, float X, float Y, float Z)[] SeededPositions =
        {
            ("Lt. Casper 1721100", -272.0f, 168.763f, -54.0f),
            ("Overseer Tyryd 1721101", -160.0f, 93.18f, -10.0f),
            ("Captain Velns 199061", 30.0f, 139.163f, 200.0f),
            ("Daniel Corman 199062", 20.0f, 72.038f, -10.0f)
        };

        /// <summary>The three shrines of 960: logos row, word, the world's position.</summary>
        private static readonly (uint Id, string Word, float X, float Y, float Z)[] Shrines =
        {
            (50, "Movement", 0.93359375f, 90.92969f, 115.21094f),
            (45, "Around", 24.070312f, 74.84766f, -9.0f),
            (4, "Chaos", 23.980469f, 74.828125f, -20.121094f)
        };

        [TestMethod]
        public void TheSeedIsTheDossiersReachableSet()
        {
            CollectionAssert.AreEqual(new[] { (450u, 107u, 199061u, 10u), (960u, 134u, 134u, 10u) },
                WildernessCraterLakeResearchFacilityRows.Missions.Select(m => (m.Id, m.Giver, m.Receiver, m.Level)).ToArray());
            CollectionAssert.AreEqual(new[] { (450u, 4u, 1u), (960u, 4u, 1u), (960u, 5u, 2u), (960u, 6u, 3u) },
                WildernessCraterLakeResearchFacilityRows.Objectives.Select(o => (o.Mission, o.Objective, o.Ordinal)).ToArray());
            CollectionAssert.AreEqual(Shrines.Select((s, i) => (960u, 4u + (uint)i, s.Id, s.Word)).ToArray(),
                WildernessCraterLakeResearchFacilityRows.Shrines.Select(s => (s.Mission, s.Objective, s.Logos, s.Word)).ToArray());
            // 900 credits and no experience for 450 (none recorded); 18,000 and 1,500 for 960. No item.
            CollectionAssert.AreEqual(new[] { (450u, (byte)1, 900), (960u, (byte)3, 18000), (960u, (byte)1, 1500) },
                WildernessCraterLakeResearchFacilityRows.Rewards.Select(r => (r.Mission, r.Type, r.Amount)).ToArray());
            CollectionAssert.AreEqual(new[] { (1721100u, 520012u), (1721101u, 1721001u) },
                WildernessCraterLakeResearchFacilityRows.Placements.Select(p => (p.Id, p.Creature)).ToArray());
            // Held: 1055, 1065, 489, 1054 and the dish, terminals, crates and exit area of the dossier.
            var seededMissions = WildernessCraterLakeResearchFacilityRows.Missions.Select(m => m.Id).ToHashSet();
            foreach (var held in new uint[] { 1055, 1065, 489, 1054, 1056 })
                Assert.IsFalse(seededMissions.Contains(held), $"mission {held} is held");
            var placements = WildernessCraterLakeResearchFacilityRows.Placements.Select(p => p.Id).ToHashSet();
            foreach (var held in new uint[] { 1721110, 1721111, 1721112, 1721113, 1721120, 1721130, 1721131, 1721132 })
                Assert.IsFalse(placements.Contains(held), $"placement {held} is held");
            // Tyryd: the client's name and class, Ellatha's level; the rest analogues (OD-141).
            var tyryd = WildernessCraterLakeResearchFacilityRows.TyrydCreature;
            Assert.AreEqual((1721001u, 10502u, 0u, 9u, 7002u), ((uint)tyryd[0], (uint)tyryd[2], (uint)tyryd[3], (uint)tyryd[4], (uint)tyryd[6]));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) WildernessCraterLakeResearchFacilityRows.InsertData(migration);
                else WildernessCraterLakeResearchFacilityRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                // The only update is spawnpool 520012's first slot: 0/0 up, 1/1 down.
                var update = migration.Operations.OfType<UpdateDataOperation>().Single();
                Assert.AreEqual(("spawnpool", 520012u), (update.Table, (uint)update.KeyValues[0, 0]));
                CollectionAssert.AreEqual(new[] { "creature_1_min_count", "creature_1_max_count" }, update.Columns);
                Assert.AreEqual(up ? (byte)0 : (byte)1, update.Values[0, 0]);
                Assert.AreEqual(up ? (byte)0 : (byte)1, update.Values[0, 1]);
                Assert.AreEqual(2, update.ColumnTypes?.Length ?? 0);
                Assert.IsFalse(migration.Operations.OfType<SqlOperation>().Any());
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            WildernessCraterLakeResearchFacilityRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == WildernessCraterLakeResearchFacilityRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            var decisions = root.GetProperty("owner_decisions").EnumerateArray()
                .ToDictionary(decision => decision.GetProperty("id").GetString(), decision => decision.GetProperty("status").GetString());

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
                    // Every analogue rests on one of this batch's approved decisions.
                    if (field.Value.GetProperty("tier").GetString() == "analogue")
                    {
                        var decision = field.Value.GetProperty("decision").GetString();
                        Assert.IsTrue(decision is "OD-140" or "OD-141" or "OD-142", $"{table} {key}.{field.Name}: {decision}");
                        Assert.AreEqual("approved", decisions[decision]);
                    }
                }
                row.TryGetProperty("storage_fields", out var storage);
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) ||
                                  (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty(column, out _)),
                        $"{table} {key}.{column} is seeded without provenance");
                if (storage.ValueKind == JsonValueKind.Object)
                    foreach (var column in storage.EnumerateObject())
                        Assert.IsTrue(Same(matches[0].Values[column.Name], column.Value.GetProperty("value")), $"{table} {key}.{column.Name} storage differs");

                if (table == "npc_mission")
                    Assert.AreEqual(("analogue", "OD-140"), (fields.GetProperty("level").GetProperty("tier").GetString(), fields.GetProperty("level").GetProperty("decision").GetString()));
                if (table == "npc_mission_reward")
                    Assert.AreEqual("pre-1.4", fields.GetProperty("credits").GetProperty("era").GetString());
                // Every seeded height is the navmesh floor, measured.
                if (table == "content_placement")
                    Assert.AreEqual("measured", fields.GetProperty("pos_y").GetProperty("tier").GetString());
            }

            // Casper's pool retirement is a recorded change; the held missions are recorded omissions.
            Assert.IsTrue(root.GetProperty("changes").EnumerateArray().Any(change =>
                change.GetProperty("migration").GetString() == WildernessCraterLakeResearchFacilityRows.Migration &&
                change.GetProperty("table").GetString() == "spawnpool" && change.GetProperty("key").GetProperty("id").GetInt32() == 520012));
            var gaps = root.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-CLRF-INTERIOR-NAVMESH", "GAP-CLRF-DT3-POSITION", "GAP-CLRF-TERMINAL-BODY", "GAP-CLRF-RADAR-DISH-TWOSTATE",
                         "GAP-CLRF-1056-CHAIN", "GAP-CLRF-PEN-FORCEFIELD", "GAP-CLRF-450-XP", "GAP-REWARD-ERA-CLRF", "GAP-CLRF-TYRYD",
                         "GAP-CLRF-CASPER-ENCAMPMENT", "GAP-CLRF-D11-BOSSES", "GAP-CLRF-D11-TREASURE-CRATES", "GAP-CLRF-POPULATION",
                         "GAP-CLRF-JAMMER", "GAP-CLRF-CRYSTAL-CONTAINER-CLASS", "GAP-MISSION-LEVEL", "GAP-INSTANCE-RESET-TIMER" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            foreach (var decision in new[] { "OD-140", "OD-141", "OD-142", "OD-143", "OD-144" })
                Assert.AreEqual("approved", decisions[decision], decision);
            var omitted = root.GetProperty("omitted").EnumerateArray().Select(entry => entry.GetProperty("what").GetString()).ToList();
            Assert.IsTrue(omitted.Any(what => what.StartsWith("1055 Destroying the Evidence", StringComparison.Ordinal)));
            Assert.IsTrue(omitted.Any(what => what.StartsWith("1065 Bending the Rules", StringComparison.Ordinal)));
        }

        /// <summary>
        /// Every seeded position and every shrine 960 waits on stands on navmesh floor (the placement 0.276 m under the
        /// surface) joined to the instance entrance by a complete path; the held terminal readings are not.
        /// </summary>
        [TestMethod]
        public void EveryPositionIsOnFloorJoinedToTheEntrance()
        {
            var mesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", "adv_foreas_concordia_wilderness_clrf.nav")));
            Assert.IsNotNull(mesh.GroundHeight(Entrance), "the entrance has ground");

            foreach (var (what, x, y, z) in SeededPositions)
            {
                var surface = mesh.GroundHeight(new Vector3(x, y + 0.276f, z));
                Assert.IsNotNull(surface, what);
                Assert.AreEqual(surface.Value - 0.276f, y, 0.01f, what);
                Assert.IsNotNull(mesh.FindPath(Entrance, new Vector3(x, y + 0.276f, z), out var complete), what);
                Assert.IsTrue(complete, $"{what} is reached from the entrance");
            }

            foreach (var (id, word, x, y, z) in Shrines)
            {
                Assert.IsNotNull(mesh.FindPath(Entrance, new Vector3(x, y, z), out var complete), word);
                Assert.IsTrue(complete, $"shrine {id} {word} is reached from the entrance");
            }

            // Held: the Processing Center terminal's floors (HQ second floor, roof) and the Biological Lab greenhouse are islands.
            foreach (var (what, x, y, z) in new[] { ("HQ second floor", 124.3f, 105f, -27.3f), ("HQ roof", 124.3f, 118f, -27.3f), ("greenhouse", 102.3f, 118.9f, -89.6f) })
            {
                mesh.FindPath(Entrance, new Vector3(x, y, z), out var complete);
                Assert.IsFalse(complete, $"{what} is not joined (GAP-CLRF-INTERIOR-NAVMESH)");
            }
        }

        /// <summary>
        /// A squad copy of Crater Lake carries its own copy of each shrine 960 waits on, under the id the binding names
        /// (the dossier's GAP-LOGOS-IN-SQUAD-INSTANCE), and two squads' copies do not share them.
        /// </summary>
        [TestMethod]
        public void EachSquadCopyCarriesTheShrinesTheLogosMissionWaitsOn()
        {
            var maps = new MapChannelManager(null, () => 0) { IsSquadContext = contextId => contextId == CraterLake };
            var primary = new MapChannel { MapInfo = new MapInfo(CraterLake, "adv_foreas_concordia_wilderness_clrf", 290, 0), ClientList = new List<Client>() };
            maps.MapChannelArray.Add(CraterLake, primary);
            foreach (var (id, word, _, _, _) in Shrines)
                primary.DynamicObjects.Add(new Logos(new Structures.World.LogosEntry { Id = id, ClassId = 12690, MapContextId = CraterLake, Name = word }));

            var first = maps.ChannelForEntry(101, CraterLake, 7);
            var second = maps.ChannelForEntry(103, CraterLake, 8);
            try
            {
                Assert.AreNotSame(first, second);
                Assert.IsTrue(first.IsSquadInstance && second.IsSquadInstance);
                var bound = WildernessCraterLakeResearchFacilityRows.Shrines.Select(shrine => shrine.Logos).OrderBy(id => id).ToArray();
                foreach (var copy in new[] { first, second })
                    CollectionAssert.AreEqual(bound, copy.DynamicObjects.OfType<Logos>().Select(logos => logos.Id).OrderBy(id => id).ToArray());
                Assert.IsFalse(first.DynamicObjects.OfType<Logos>().Intersect(second.DynamicObjects.OfType<Logos>()).Any(), "each copy has its own shrine objects");
                Assert.IsFalse(first.DynamicObjects.Intersect(primary.DynamicObjects).Any());
            }
            finally
            {
                foreach (var (copy, party) in new[] { (first, 7u), (second, 8u) })
                {
                    maps.ReleaseInstanceIfEmpty(copy);
                    maps.SquadRetired(party);
                }
                maps.DestroyQueuedInstances();
            }
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + WildernessCraterLakeResearchFacilityRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (450, 960) AND ordinal IS NULL"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 520012 AND creature_1_min_count = 1 AND creature_1_max_count = 1 AND map_context_id = 1721"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 520012 AND creature_1_min_count = 0 AND creature_1_max_count = 0"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (1721100, 1721101) AND map_context_id = 1721 AND kind = 1 AND behavior = 2 AND respawn_ms = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 199061 AND package_id = 23"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (450, 960)"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only), which predates this migration: the shrines are the client's logosstone rows on
        /// 1721, Casper's pool and creature are the preloader's, Velns and Daniel stand on 1721, nothing carries package
        /// 23, the client's own completion row for 450/4 is there, and the ids this migration allocates are free.
        /// </summary>
        [TestMethod]
        public void TheDeployedWorldHasWhatTheSeedStandsOn()
        {
            using var world = OpenWorld(RepositoryRoot());
            foreach (var (id, word, _, _, _) in Shrines)
                Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM logos WHERE id = {id} AND name = '{word}' AND map_context_id = 1721"), word);
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE id = 520012 AND map_context_id = 1721 AND creature_1_Id = 520012 AND creature_1_min_count = 1 AND creature_1_max_count = 1 AND pos_x = -272.0 AND pos_z = -54.0 AND ABS(pos_y - 168.763) < 0.001"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 520012 AND name_id = 6739 AND class_id = 21474 AND level = 9 AND faction = 0"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id IN (199061, 199062) AND creature_id = id AND map_context_id = 1721 AND npc_package_id = 0"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_package WHERE package_id = 23 OR id = 199061"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id = 450 AND objective_id = 4 AND npc_package_id = 23"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id = 960"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_mission WHERE id IN (450, 960)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 1721001 AND 1721099"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 1721100 AND 1721499"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM map_link WHERE id = 15 AND map_context_id = 1220 AND dest_map_context_id = 1721"));
            // The givers: Dr. Franja Corman on the Wilderness, Standley at Twin Pillars.
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE creature_1_Id IN (107, 134) AND map_context_id = 1220 AND creature_1_max_count > 0"));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var (table, filter) in new[]
                     {
                         ("npc_mission", ""), ("npc_mission_objective", ""), ("npc_mission_objective_binding", ""), ("npc_mission_reward", ""),
                         ("npc_mission_prerequisite", ""), ("npc_package", ""), ("content_placement", ""), ("creature", " WHERE id >= 1721000"),
                         ("spawnpool", " WHERE id = 520012")
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
