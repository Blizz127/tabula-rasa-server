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
using Rasa.Managers;
using Rasa.Migrations.WildernessData;
using Rasa.Navigation;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// AmbientPoolRespawn and DivideOpenGroundPopulations (2026-09-28): the hostile ambient pools of the 2026-09-27 batches and of
    /// the Wilderness leave their 2-5 s for the owner's 60-300 s, spread so neighbours are out of step; the Divide's 21 new open-ground
    /// pools stand on floor the Foreas Base waypoint reaches, load as the server loads them, match their manifest rows, and
    /// both migrations roll back exactly. What the evidence does not place (Predators, Amoeboids) stays out.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AmbientRespawnDividePopulationsTests
    {
        private static readonly Vector3 ForeasBase = new Vector3(-33.55f, 116.18f, 564.44f);

        /// <summary>The Wilderness (1220) hostile ambient pools OD-186 covers (owner, 2026-09-28).</summary>
        private static readonly uint[] WildernessIncluded =
        {
            37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 52, 54, 55, 56, 57, 58, 60, 61, 69, 70, 73, 74, 75, 76, 77, 79, 80, 83, 85, 88,
            94, 96, 97, 98, 99, 102, 103, 104, 105, 108, 109, 110, 111, 112, 118, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 133,
            137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 151, 152, 153, 154, 160, 161, 162, 163, 164, 165, 166, 167
        };

        /// <summary>Wilderness pools of hostile creatures OD-186 leaves alone: bosses and named bosses, and pools that draw nothing.</summary>
        private static readonly uint[] WildernessHostileExcluded = { 1, 2, 3, 81, 82, 91, 132, 156, 157, 158, 159, 168, 169, 520065 };

        [TestMethod]
        public void TheHostileAmbientPoolsTakeTheOwnersRangeAndDoNotRefillInStep()
        {
            var batches = Enumerable.Range(1148200, 12).Concat(Enumerable.Range(1244200, 14)).Concat(Enumerable.Range(1764200, 11))
                .Concat(Enumerable.Range(1759200, 8)).Select(id => (uint)id).ToArray();
            CollectionAssert.AreEquivalent(batches.Concat(WildernessIncluded).ToArray(), AmbientPoolRespawnRows.Pools.Select(pool => pool.Id).ToArray());
            Assert.IsFalse(AmbientPoolRespawnRows.Pools.Any(pool => WildernessHostileExcluded.Contains(pool.Id)), "bosses keep their respawn");
            // The two Class IV Stalkers keep TaRapedia's 15 minutes; the Divide bosses (520015-520019) are not touched.
            Assert.IsFalse(AmbientPoolRespawnRows.Pools.Any(pool => pool.Id is 1148212 or 1148213 || pool.Id / 1000 == 520));
            Assert.IsTrue(ConcordiaAmbientPopulationsRows.Pools.Where(pool => pool.Id is 1148212 or 1148213).All(pool => pool.Respawn == 9000));

            // The old value is each batch's own 20, or the Wilderness world seed's 2-5 s; the new one lies in 60-300 s, in 10 s steps.
            var previous = ConcordiaAmbientPopulationsRows.Pools.Select(pool => (pool.Id, pool.Respawn))
                .Concat(FootageAmbientPopulationsRows.Pools.Select(pool => (pool.Id, FootageAmbientPopulationsRows.PoolRespawn))).ToDictionary(p => p.Id, p => p.Item2);
            foreach (var pool in AmbientPoolRespawnRows.Pools)
            {
                if (previous.TryGetValue(pool.Id, out var old))
                    Assert.AreEqual(old, pool.Old, $"pool {pool.Id} old");
                else
                    Assert.IsTrue(pool.Old is 20 or 25 or 30 or 50, $"Wilderness pool {pool.Id} old {pool.Old}");
                Assert.AreEqual(600u + 100u * ((pool.Id * 7u) % 25u), pool.New, $"pool {pool.Id} new");
                Assert.IsTrue(pool.New >= 600 && pool.New <= 3000, $"pool {pool.Id} new {pool.New}");
            }
            foreach (var pool in DivideOpenGroundPopulationsRows.Pools)
                Assert.AreEqual(600u + 100u * ((pool.Id * 7u) % 25u), pool.Respawn, $"pool {pool.Id} respawn");

            // Spread, not one value: consecutive ids never share a delay, and every 10 s step of 60-300 s is used.
            var all = AmbientPoolRespawnRows.Pools.Select(pool => (pool.Id, Respawn: pool.New))
                .Concat(DivideOpenGroundPopulationsRows.Pools.Select(pool => (pool.Id, pool.Respawn))).OrderBy(pool => pool.Id).ToList();
            for (var i = 1; i < all.Count; i++)
                if (all[i].Id == all[i - 1].Id + 1)
                    Assert.AreNotEqual(all[i - 1].Respawn, all[i].Respawn, $"pools {all[i - 1].Id} and {all[i].Id} refill in step");
            Assert.AreEqual(25, all.Select(pool => pool.Respawn).Distinct().Count());
        }

        [TestMethod]
        public void TheWildernessChangeIsExactlyItsHostileAmbientPools()
        {
            var root = RepositoryRoot();
            var path = Path.Combine(root, "rasaworld.db");
            if (!File.Exists(path))
                Assert.Inconclusive("rasaworld.db is not in the repository root; this audit reads the world database");
            using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            var faction = new Dictionary<long, long>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, faction FROM creature";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    faction[reader.GetInt64(0)] = reader.GetInt64(1);
            }
            var hostile = new Dictionary<uint, uint>();
            var drawn = new Dictionary<uint, long>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, respown_time, creature_1_Id, creature_2_Id, creature_3_Id, creature_4_Id, creature_5_Id, creature_6_Id, " +
                                      "creature_1_max_count + creature_2_max_count + creature_3_max_count + creature_4_max_count + creature_5_max_count + creature_6_max_count FROM spawnpool WHERE map_context_id = 1220";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var slots = Enumerable.Range(2, 6).Select(reader.GetInt64).Where(id => id != 0).ToList();
                    if (slots.Count > 0 && slots.All(id => faction.GetValueOrDefault(id, 1) == 0))
                    {
                        hostile[(uint)reader.GetInt64(0)] = (uint)reader.GetInt64(1);
                        drawn[(uint)reader.GetInt64(0)] = reader.GetInt64(8);
                    }
                }
            }
            // Every hostile Wilderness pool is either changed or deliberately left out; no friendly or service pool is changed.
            CollectionAssert.AreEquivalent(hostile.Keys.ToArray(), WildernessIncluded.Concat(WildernessHostileExcluded).ToArray());
            var changed = AmbientPoolRespawnRows.Pools.Where(pool => pool.Id < 1_000_000).ToDictionary(pool => pool.Id);
            CollectionAssert.AreEquivalent(WildernessIncluded, changed.Keys.ToArray());
            foreach (var id in WildernessIncluded)
            {
                Assert.AreEqual(hostile[id], changed[id].Old, $"pool {id}: Down restores the world seed's value");
                Assert.IsTrue(hostile[id] < 500 && drawn[id] > 0, $"pool {id} is a drawing, non-boss pool");
            }
            // The excluded ones are the 50 s boss pools, the named bosses, and the pools that draw nothing.
            foreach (var id in WildernessHostileExcluded)
                Assert.IsTrue(hostile[id] == 500 || drawn[id] == 0 || id is 81 or 82 or 91 or 168 or 169, $"pool {id}");
        }

        [TestMethod]
        public void AKilledOutPoolWaitsItsRowsDelayAsTheServerLoadsIt()
        {
            foreach (var row in AmbientPoolRespawnRows.Pools)
            {
                // SpawnPoolManager.SpawnPoolInit: RespawnTime = respown_time x 100 ms.
                var pool = new SpawnPool { DbId = row.Id, RespawnTime = row.New * 100, UpdateTimer = 0, SpawnSlot = new List<SpawnPoolSlot>(), AliveCreatures = 1 };
                Assert.IsTrue(pool.RespawnTime >= 60_000 && pool.RespawnTime <= 300_000, $"pool {row.Id}");
                var channel = new MapChannel { SpawnPools = new List<SpawnPool> { pool } };
                pool.UpdateTimer = 12_345;
                SpawnPoolManager.Instance.DecreaseAliveCreatureCount(channel, pool);
                Assert.AreEqual(0, pool.UpdateTimer, "the delay counts from the last death");
                SpawnPoolManager.Instance.SpawnPoolWorker(channel, pool.RespawnTime - 1000);
                Assert.IsTrue(pool.UpdateTimer < pool.RespawnTime, $"pool {row.Id} is still on cooldown one second before its delay");
            }
        }

        [TestMethod]
        public void EveryDividePoolStandsOnFloorTheForeasBaseWaypointReaches()
        {
            var query = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", "adv_foreas_concordia_divide.nav")));
            foreach (var pool in DivideOpenGroundPopulationsRows.Pools)
            {
                var ground = query.GroundHeight(new Vector3((float)pool.X, (float)pool.Y + 0.276f, (float)pool.Z));
                Assert.IsNotNull(ground, $"pool {pool.Id} has no navmesh ground");
                Assert.AreEqual(ground.Value - 0.276, pool.Y, 0.3, $"pool {pool.Id} is not on its floor");
                Assert.IsFalse(query.IsUnderground(new Vector3((float)pool.X, ground.Value, (float)pool.Z)), $"pool {pool.Id} is underground");
                query.FindPath(ForeasBase, new Vector3((float)pool.X, ground.Value, (float)pool.Z), out var complete);
                Assert.IsTrue(complete, $"pool {pool.Id} cannot be reached from Foreas Base");
            }
            // The new pools do not sit on top of the earlier ones.
            foreach (var pool in DivideOpenGroundPopulationsRows.Pools.Where(pool => pool.Id != 1148227))
                foreach (var earlier in ConcordiaAmbientPopulationsRows.Pools.Where(p => p.Map == 1148))
                    Assert.IsTrue(Math.Sqrt(Math.Pow(pool.X - earlier.X, 2) + Math.Pow(pool.Z - earlier.Z, 2)) > 60, $"pool {pool.Id} is within 60 m of {earlier.Id}");
        }

        [TestMethod]
        public void TheDivideGetsTheSpeciesItsTextPlacesAndOnlyThose()
        {
            var own = DivideOpenGroundPopulationsRows.Creatures.ToDictionary(c => c.Id);
            var earlier = ConcordiaAmbientPopulationsRows.Creatures.ToDictionary(c => c.Id);
            uint ClassOf(uint id) => own.TryGetValue(id, out var c) ? c.Class : earlier[id].Class;
            var classes = DivideOpenGroundPopulationsRows.Pools.SelectMany(pool => pool.Slots).Select(slot => ClassOf(slot.Creature)).Distinct().ToArray();
            // Boargar, Xanx, Filchers, Warnets, Thrax, Caretakers, Hominis Machina.
            CollectionAssert.AreEquivalent(new uint[] { 6031, 7510, 6421, 6262, 29769, 9244, 3868 }, classes);
            // Held: Predators (3902, flying), Amoeboids (6032/7696), Stalkers beyond the two seeded.
            foreach (var held in new uint[] { 3902, 6032, 7696, 3781 })
                Assert.IsFalse(classes.Contains(held), $"class {held}");
            Assert.AreEqual(21, DivideOpenGroundPopulationsRows.Pools.Length);
            Assert.AreEqual(68, DivideOpenGroundPopulationsRows.Pools.Sum(pool => pool.Slots.Sum(slot => slot.Count)));
            var drawn = DivideOpenGroundPopulationsRows.Pools.SelectMany(pool => pool.Slots).Select(slot => slot.Creature).ToHashSet();
            foreach (var creature in DivideOpenGroundPopulationsRows.Creatures)
            {
                Assert.IsTrue(drawn.Contains(creature.Id), $"creature {creature.Id} is drawn by no pool");
                Assert.IsTrue(creature.Level >= 12 && creature.Level <= 18, $"creature {creature.Id} lies outside the Divide band");
                Assert.IsTrue(creature.Comment.Length <= 50);
            }
            foreach (var pool in DivideOpenGroundPopulationsRows.Pools)
            {
                Assert.AreEqual(1148u, pool.Id / 1000, $"pool {pool.Id}");
                Assert.AreEqual((byte)0, pool.Anim, $"pool {pool.Id}");
                Assert.IsTrue(pool.Slots.Length is > 0 and <= 6 && pool.Slots.All(slot => slot.Count > 0 && slot.Creature / 1000 == 1148), $"pool {pool.Id}");
            }
            Assert.IsFalse(DivideOpenGroundPopulationsRows.Pools.Select(p => p.Id).Intersect(ConcordiaAmbientPopulationsRows.Pools.Select(p => p.Id)).Any());
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
                foreach (var (insert, delete) in new (Action<MigrationBuilder>, Action<MigrationBuilder>)[]
                         { (AmbientPoolRespawnRows.InsertData, AmbientPoolRespawnRows.DeleteData), (DivideOpenGroundPopulationsRows.InsertData, DivideOpenGroundPopulationsRows.DeleteData) })
                {
                    var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                    (up ? insert : delete)(migration);
                    foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                        Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                    foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                        Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                    foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                        Assert.AreEqual((row.KeyColumns.Length, row.Columns.Length), (row.KeyColumnTypes?.Length ?? 0, row.ColumnTypes?.Length ?? 0), row.Table);
                }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRowAndEveryRespawnItsChange()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            DivideOpenGroundPopulationsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == DivideOpenGroundPopulationsRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");
            var placeTiers = new Dictionary<string, int>();
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
                if (table == "spawnpool")
                {
                    // The respawn is inferred from the evidenced bracket, never an analogue; heights are measured.
                    Assert.AreEqual("inferred", fields.GetProperty("respown_time").GetProperty("tier").GetString(), $"{key}.respown_time");
                    Assert.AreEqual("measured", fields.GetProperty("pos_y").GetProperty("tier").GetString(), $"{key}.pos_y");
                    var tier = fields.GetProperty("pos_x").GetProperty("tier").GetString();
                    Assert.AreEqual(tier, fields.GetProperty("pos_z").GetProperty("tier").GetString());
                    placeTiers[tier] = placeTiers.GetValueOrDefault(tier) + 1;
                    if (tier == "analogue")
                        Assert.AreEqual("OD-188", fields.GetProperty("pos_x").GetProperty("decision").GetString());
                }
                if (table == "creature")
                    foreach (var column in new[] { "level", "max_hp", "action1", "run_speed", "walk_speed" })
                        Assert.AreEqual("analogue", fields.GetProperty(column).GetProperty("tier").GetString(), $"{key}.{column}");
            }
            // 14 places from text coordinates or landmarks, one from Brady's map, six analogues inside the named area.
            Assert.AreEqual((14, 1, 6), (placeTiers.GetValueOrDefault("inferred"), placeTiers.GetValueOrDefault("measured"), placeTiers.GetValueOrDefault("analogue")));

            // Every respawn replacement has its change entry, old and new as the migration writes them.
            var changes = document.RootElement.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() == AmbientPoolRespawnRows.Migration).ToList();
            Assert.AreEqual(AmbientPoolRespawnRows.Pools.Length, changes.Count);
            foreach (var pool in AmbientPoolRespawnRows.Pools)
            {
                var change = changes.Single(c => c.GetProperty("key").GetProperty("id").GetUInt32() == pool.Id);
                Assert.AreEqual(("spawnpool", "respown_time"), (change.GetProperty("table").GetString(), change.GetProperty("field").GetString()));
                Assert.AreEqual((pool.Old, pool.New), (change.GetProperty("old").GetUInt32(), change.GetProperty("new").GetUInt32()));
                Assert.AreEqual("inferred", change.GetProperty("tier").GetString());
                Assert.IsTrue(change.GetProperty("citations").GetArrayLength() > 0);
            }

            var gaps = document.RootElement.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-AMBIENT-POOL-RESPAWN-VALUE", "GAP-AMBIENT-RESPAWN-MECHANISM", "GAP-DIVIDE-BOSS-RESPAWN",
                         "GAP-DIVIDE-OPEN-GROUND-PLACES", "GAP-DIVIDE-OPEN-GROUND-STATS", "GAP-DIVIDE-UNPLACED-SPECIES" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            var decisions = document.RootElement.GetProperty("owner_decisions").EnumerateArray().Select(decision => decision.GetProperty("id").GetString()).ToHashSet();
            for (var id = 186; id <= 191; id++)
                Assert.IsTrue(decisions.Contains($"OD-{id}"), $"OD-{id}");
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactlyAndTheServerLoadsTheRows()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var respawn = all.Single(id => id.EndsWith("_" + AmbientPoolRespawnRows.Migration, StringComparison.Ordinal));
            var divide = all.Single(id => id.EndsWith("_" + DivideOpenGroundPopulationsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(respawn) - 1]);
            // Wilderness world-seed pools as the seed carries them: four hostile ambient pools (2.5, 2, 5 and 3 s) and a 50 s boss pool.
            foreach (var (id, respawnTime, creature) in new (uint, uint, uint)[] { (37, 25, 44), (55, 20, 33), (73, 50, 57), (160, 30, 87), (156, 500, 75) })
                context.Database.ExecuteSqlRaw("INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, creature_1_Id, creature_1_min_count, creature_1_max_count, " +
                    "creature_2_Id, creature_2_min_count, creature_2_max_count, creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, " +
                    "creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count) VALUES " +
                    $"({id}, 0, 0, {respawnTime}, 0, 0, 0, 0, 1220, {creature}, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            var before = Snapshot(connection);
            Assert.AreEqual(45L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE respown_time = 20 AND (id BETWEEN 1148200 AND 1148211 OR id BETWEEN 1244200 AND 1244213 OR id BETWEEN 1764200 AND 1764210 OR id BETWEEN 1759200 AND 1759207)"));

            migrator.Migrate(divide);
            Assert.AreNotEqual(before, Snapshot(connection));
            // As SpawnPoolManager.SpawnPoolInit reads them: every changed pool waits 60-300 s; the Divide pools load at their places.
            var loaded = context.SpawnPoolEntries.AsNoTracking().ToList().ToDictionary(entry => entry.Id);
            foreach (var pool in AmbientPoolRespawnRows.Pools.Where(pool => pool.Id > 1_000_000))
                Assert.AreEqual(pool.New * 100, loaded[pool.Id].RespawnTime * 100, $"pool {pool.Id}");
            foreach (var pool in DivideOpenGroundPopulationsRows.Pools)
            {
                var entry = loaded[pool.Id];
                Assert.AreEqual((1148u, pool.Respawn), (entry.MapContextId, entry.RespawnTime));
                Assert.AreEqual(pool.X, entry.PosX, 0.0001);
                Assert.AreEqual(pool.Y, entry.PosY, 0.0001);
                Assert.AreEqual(pool.Z, entry.PosZ, 0.0001);
                Assert.AreEqual(pool.Slots[0].Creature, entry.Creature1Id);
                Assert.AreEqual((pool.Slots[0].Count, pool.Slots[0].Count), (entry.Creature1MinCount, entry.Creature1MaxCount));
            }
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 1148008 AND 1148012"));
            foreach (var id in new uint[] { 37, 55, 73, 160 })
                Assert.AreEqual((long)(600 + 100 * (id * 7 % 25)), Scalar(connection, $"SELECT respown_time FROM spawnpool WHERE id = {id}"), $"Wilderness pool {id}");
            Assert.AreEqual(500L, Scalar(connection, "SELECT respown_time FROM spawnpool WHERE id = 156"), "the boss pool keeps its respawn");

            migrator.Migrate(all[all.IndexOf(respawn) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "creature", "spawnpool" })
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
