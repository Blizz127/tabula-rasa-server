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
    /// PalisadesDossierMissions: the seed is the dossier's finishable set with its defaults (OD-153 to OD-160), every
    /// seeded value is the one its manifest row records, every giver, target and boss the five missions need stands on
    /// navmesh floor a Palisades waypoint reaches (the Man shrine's gorge excepted, as its gap records), the deployed
    /// world has what the seed stands on, and the rollback gives the world back exactly. Mission loading, offer and the
    /// bosses' drops are asserted on the migrated world in MissionContentLoadingTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PalisadesDossierMissionsTests
    {
        private const uint Palisades = 1244;

        // Client waypoint markers (uimapmarker, research/20260927-palisades-dossiers/work/markers-1244.txt).
        private static readonly Vector3 NewCumbria = new(-766.65f, 139.5f, 584.46f);
        private static readonly Vector3 CumbriaResearch = new(-503.11f, 147.94f, 630.65f);
        private static readonly Vector3 RiverBaseKrimm = new(185.6f, 109.84f, 295.64f);
        private static readonly Vector3 WalkOfGiants = new(-578.83f, 190.1f, 159.04f);
        private static readonly Vector3 ViandsVillage = new(-264.82f, 176.62f, -457.75f);
        private static readonly Vector3 CampElinor = new(361.41f, 130.07f, 625.21f);
        private static readonly Vector3 Hightower = new(-72.45f, 147.59f, 70.52f);

        /// <summary>Written out independently of the Rows class: what each mission needs, its world position and a waypoint.</summary>
        private static readonly (string What, float X, float Y, float Z, Vector3 From)[] Reachable =
        {
            ("Arizpe 199085 (1812, 1813)", -538.0f, 142.095f, 665.0f, CumbriaResearch),
            ("Brocail 199102 (1988)", -531.0f, 186.0f, -41.0f, WalkOfGiants),
            ("Matlin 199053 (2014)", -809.0f, 138.026f, 601.0f, NewCumbria),
            ("Derac 199087 (2014/1)", -798.0f, 140.17f, 766.0f, NewCumbria),
            ("Gantic 199054 (2014/2)", -60.0f, 118.518f, 597.0f, RiverBaseKrimm),
            ("Mullen 510135 (1795)", 218.1f, 110.03f, 338.5f, RiverBaseKrimm),
            ("Barbrix 199095 (1795/1)", 483.0f, 120.755f, 171.0f, RiverBaseKrimm),
            ("shrine 333 True (1812)", -423.64844f, 150.48828f, -417.02344f, ViandsVillage),
            ("shrine 322 Through (1813)", -404.82812f, 168.70312f, 635.79297f, CumbriaResearch),
            ("shrine 208 Knowledge (1988/1)", -289.9961f, 162.6875f, 984.0f, CumbriaResearch),
            ("shrine 266 Planet (1988/3)", 416.04297f, 112.359375f, 386.46094f, CampElinor)
        };

        [TestMethod]
        public void TheSeedIsTheDossiersFinishableSet()
        {
            CollectionAssert.AreEqual(new[] { (1812u, 199085u, 199085u, 25u), (1813u, 199085u, 199085u, 25u), (1988u, 199102u, 199102u, 15u), (2014u, 199053u, 199053u, 30u), (1795u, 510135u, 510135u, 20u) },
                PalisadesDossierMissionsRows.Missions.Select(m => (m.Id, m.Giver, m.Receiver, m.Level)).ToArray());
            CollectionAssert.AreEqual(new[] { (1812u, 18u, 333u), (1813u, 19u, 322u), (1988u, 1u, 208u), (1988u, 2u, 219u), (1988u, 3u, 266u) },
                PalisadesDossierMissionsRows.Shrines.Select(s => (s.Mission, s.Objective, s.Logos)).ToArray());
            CollectionAssert.AreEqual(new[] { (2014u, 2u, 199054u, 123352u), (1795u, 1u, 199095u, 118803u) },
                PalisadesDossierMissionsRows.Drops.Select(d => (d.Mission, d.Objective, d.Creature, d.Item)).ToArray());
            CollectionAssert.AreEqual(new[] { (2014u, 1u, 2u), (2014u, 2u, 6u) },
                PalisadesDossierMissionsRows.Transitions.Select(t => (t.Mission, t.Completed, t.Revealed)).ToArray());
            // 2014 pays items only (TaRapedia 0/0); 1812/1813/1795 pre-1.4 amounts; 1988 post-1.4 amounts and items.
            Assert.IsFalse(PalisadesDossierMissionsRows.Amounts.Any(a => a.Mission == 2014));
            CollectionAssert.AreEqual(new uint[] { 122719, 122720, 122721, 130322, 130323, 130324 }, PalisadesDossierMissionsRows.Items.Select(i => i.Template).ToArray());
            CollectionAssert.AreEqual(new[] { (199087u, 136u), (199053u, 1214u), (199086u, 134u) },
                PalisadesDossierMissionsRows.Packages.Select(p => (p.Creature, p.Package)).ToArray());
            // Held: the Skive Base missions (OD-154), 337 (OD-153), everything else the dossier holds; 368 untouched (OD-157).
            var seeded = PalisadesDossierMissionsRows.Missions.Select(m => m.Id).ToHashSet();
            foreach (var held in new uint[] { 1799, 1800, 1801, 1802, 337, 368, 1630, 2006, 1809, 1817, 331 })
                Assert.IsFalse(seeded.Contains(held), $"mission {held} is not part of this seed");

            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            PalisadesDossierMissionsRows.InsertData(migration);
            // No new creature, placement, pool or content row (the dossier's 1244xxx blocks stay unused); Gantic and Barbrix keep
            // their harmless stats (OD-156), and nothing touches 368 or the prerequisite table.
            var tables = migration.Operations.OfType<InsertDataOperation>().Select(o => o.Table).Distinct().ToList();
            CollectionAssert.AreEquivalent(new[] { "npc_package", "npc_mission", "npc_mission_objective", "npc_mission_objective_transition",
                "npc_mission_objective_binding", "npc_mission_objective_counter", "npc_mission_reward" }, tables);
            Assert.IsFalse(migration.Operations.OfType<UpdateDataOperation>().Any(o => o.Table == "creature"));
            Assert.IsFalse(migration.Operations.OfType<InsertDataOperation>().Any(o => o.Table == "npc_mission" &&
                Enumerable.Range(0, o.Values.GetLength(0)).Any(i => (uint)o.Values[i, 0] == 368)));
        }

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) PalisadesDossierMissionsRows.InsertData(migration);
                else PalisadesDossierMissionsRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                var updates = migration.Operations.OfType<UpdateDataOperation>().ToList();
                CollectionAssert.AreEquivalent(new[] { ("spawnpool", 510196u), ("content_placement", 199054u), ("content_placement", 199095u), ("teleporter", 624u) },
                    updates.Select(u => (u.Table, (uint)u.KeyValues[0, 0])).ToArray());
                foreach (var update in updates)
                    Assert.AreEqual(update.Columns.Length, update.ColumnTypes?.Length ?? 0, update.Table);
                Assert.AreEqual(up ? 60000u : 0u, updates.Where(u => u.Table == "content_placement").Select(u => (uint)u.Values[0, 0]).Distinct().Single());
                Assert.AreEqual(up ? "Waypoint: Viands Village" : "I thnk Viands Village", updates.Single(u => u.Table == "teleporter").Values[0, 0]);
                Assert.AreEqual(up ? (byte)0 : (byte)1, updates.Single(u => u.Table == "spawnpool").Values[0, 0]);
                Assert.IsFalse(migration.Operations.OfType<SqlOperation>().Any());
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            PalisadesDossierMissionsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                // The skeleton objectives' re-insert on Down is not part of Up; Up re-inserts them with flags, which are seeded rows.
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == PalisadesDossierMissionsRows.Migration).ToList();
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
                    // The only analogue among the rows is the mission level, under OD-155.
                    if (field.Value.GetProperty("tier").GetString() == "analogue")
                    {
                        Assert.AreEqual(("npc_mission", "level", "OD-155"), (table, field.Name, field.Value.GetProperty("decision").GetString()));
                        Assert.AreEqual("approved", decisions["OD-155"]);
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
                    Assert.AreEqual("analogue", fields.GetProperty("level").GetProperty("tier").GetString());
                // Every amount carries its era: pre-1.4 for 1812, 1813 and 1795 (OD-159), post-1.4 for 1988.
                if (table == "npc_mission_reward" && key.GetProperty("type").GetInt32() != 5)
                    Assert.AreEqual(key.GetProperty("id").GetInt32() == 1988 ? "post-1.4" : "pre-1.4",
                        fields.GetProperty("credits").GetProperty("era").GetString(), $"{key}");
            }

            // The updates and the Pools Orton's package removal are recorded changes; the respawn is OD-160's analogue.
            var changes = root.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() == PalisadesDossierMissionsRows.Migration).ToList();
            foreach (var (table, id) in new[] { ("npc_package", 510196), ("spawnpool", 510196), ("content_placement", 199054), ("content_placement", 199095), ("teleporter", 624) })
                Assert.AreEqual(1, changes.Count(change => change.GetProperty("table").GetString() == table && change.GetProperty("key").GetProperty("id").GetInt32() == id), $"{table} {id}");
            Assert.IsTrue(changes.Where(change => change.GetProperty("table").GetString() == "content_placement")
                .All(change => change.GetProperty("tier").GetString() == "analogue" && change.GetProperty("decision").GetString() == "OD-160" && change.GetProperty("new").GetInt32() == 60000));
            Assert.AreEqual(9, changes.Count(change => change.GetProperty("table").GetString() == "npc_mission_objective"));

            var gaps = root.GetProperty("gaps").EnumerateArray().Select(gap => gap.GetProperty("id").GetString()).ToHashSet();
            foreach (var gap in new[] { "GAP-NEW-CUMBRIA-ARIZPE", "GAP-NEW-CUMBRIA-KAVEN", "GAP-NEW-CUMBRIA-ALDRIN", "GAP-PALISADES-AMBIENT-POPULATION",
                         "GAP-SKIVE-BASE-POPULATION", "GAP-PALISADES-CREATURE-STATS", "GAP-PALISADES-LOGOS-DOORS", "GAP-PALISADES-MAN-CAVE-NAVMESH",
                         "GAP-ELOH-TEMPLES-GATE", "GAP-2014-CONNER-COMPANION", "GAP-2014-REWARD-ZERO", "GAP-2014-AFTER-337", "GAP-BARBRIX-PATROL",
                         "GAP-KRIMM-CP-NPC-PRESENCE", "GAP-PALISADES-368-KILL-TIMER", "GAP-PALISADES-NAMED-BOSS-RESPAWN", "GAP-PALISADES-366-PREREQ-1988",
                         "GAP-ORTON-199086-FLOOR", "GAP-MULLEN-LEVEL", "GAP-2014-REWARD-ITEM-GRADE", "GAP-MISSION-LEVEL", "GAP-REWARD-ERA" })
                Assert.IsTrue(gaps.Contains(gap), gap);
            for (var id = 153; id <= 160; id++)
                Assert.AreEqual("approved", decisions[$"OD-{id}"], $"OD-{id}");
            var omitted = root.GetProperty("omitted").EnumerateArray().Select(entry => entry.GetProperty("what").GetString()).ToList();
            Assert.IsTrue(omitted.Any(what => what.StartsWith("1799 Grid Disruption, 1800 Info Repro, 1801 Big Risk", StringComparison.Ordinal)));
            Assert.IsTrue(omitted.Any(what => what.StartsWith("337 The Reformists", StringComparison.Ordinal)));
        }

        /// <summary>
        /// Every NPC, boss and shrine the five missions need stands on navmesh floor that a complete path from a Palisades
        /// waypoint reaches. The Man shrine's gorge floor is a navmesh island (GAP-PALISADES-MAN-CAVE-NAVMESH): it is floor,
        /// open to the sky, and binds NPC pathing only.
        /// </summary>
        [TestMethod]
        public void EveryGiverTargetAndBossIsOnFloorAWaypointReaches()
        {
            var mesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(RepositoryRoot(), "navmesh", "adv_foreas_concordia_palisades.nav")));
            foreach (var (what, x, y, z, from) in Reachable)
            {
                var surface = mesh.GroundHeight(new Vector3(x, y + 2f, z));
                Assert.IsNotNull(surface, what);
                Assert.AreEqual(y, surface.Value, 2.5f, $"{what} stands at the floor");
                Assert.IsNotNull(mesh.FindPath(from, new Vector3(x, surface.Value, z), out var complete), what);
                Assert.IsTrue(complete, $"{what} is reached from its waypoint");
            }

            var man = mesh.GroundHeight(new Vector3(-184.00781f, 66.69531f, -119.99609f));
            Assert.IsNotNull(man, "the Man shrine has floor");
            Assert.IsFalse(mesh.IsUnderground(new Vector3(-184.00781f, man.Value, -119.99609f)), "the gorge is open above");
            mesh.FindPath(Hightower, new Vector3(-184.00781f, man.Value, -119.99609f), out var manComplete);
            Assert.IsFalse(manComplete, "the gorge floor is an island (GAP-PALISADES-MAN-CAVE-NAVMESH)");
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            // Teleporter 624 is a 2023 Add_data_to_world row, which the previous world does not replay.
            context.Database.ExecuteSqlRaw("INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) VALUES " +
                "(624, 25651, 2, 'I thnk Viands Village', -265.54297, 176.41016, -454.66406, 6.26, 1244)");
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + PalisadesDossierMissionsRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1812, 1813, 1988, 2014, 1795) AND ordinal IS NULL"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 510196 AND package_id = 134"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199053, 199086, 199087) OR package_id IN (136, 1214)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199054, 199095) AND map_context_id = 1244 AND respawn_ms = 0"));

            migrator.Migrate(self);
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM teleporter WHERE id = 624 AND description = 'Waypoint: Viands Village'"));
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1812, 1813, 1988, 2014, 1795)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510196 AND creature_1_min_count = 0 AND creature_1_max_count = 0"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only), which predates this migration: the shrines are the client's logosstone rows on
        /// 1244, the givers and bosses stand where the Rows assume, the client's own completion rows for 2014 name
        /// packages 136 and 1214, nothing carries them, 510196 holds package 134 on Valverde Pools, the item templates map
        /// to the client's mission item classes, and the dossier's reserved 1244xxx blocks are still free.
        /// </summary>
        [TestMethod]
        public void TheDeployedWorldHasWhatTheSeedStandsOn()
        {
            using var world = OpenWorld(RepositoryRoot());
            foreach (var (id, word) in new[] { (333, "True"), (322, "Through"), (208, "Knowledge"), (219, "Man"), (266, "Planet") })
                Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM logos WHERE id = {id} AND name = '{word}' AND map_context_id = {Palisades}"), word);
            Assert.AreEqual(6L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id IN (199053, 199054, 199085, 199087, 199095, 199102) AND creature_id = id AND map_context_id = 1244 AND kind = 1"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE id = 510135 AND creature_1_Id = 510135 AND map_context_id = 1244 AND creature_1_max_count = 1"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id IN (199054, 199095) AND faction = 0 AND class_id = 10504 AND action1 = 0"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 199053 AND level = 30"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 199102 AND level = 15"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 199085 AND level = 25"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id = 2014 AND ((objective_id = 1 AND npc_package_id = 136) OR (objective_id = 6 AND npc_package_id = 1214)) AND convo_type = 1"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id IN (1812, 1813, 1988, 1795)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_package WHERE package_id IN (136, 1214) OR id IN (199053, 199086, 199087)"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM npc_package WHERE id = 510196 AND package_id = 134 AND comment = 'Corporal Orton'"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE id = 510196 AND map_context_id = 1304 AND creature_1_min_count = 1 AND creature_1_max_count = 1"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM teleporter WHERE id = 624 AND map_context_id = 1244 AND description = 'I thnk Viands Village'"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM itemtemplate_itemclass WHERE (itemTemplateId = 123352 AND itemClassId = 29949) OR (itemTemplateId = 118803 AND itemClassId = 28451)"));
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM itemtemplate_itemclass WHERE itemClassId IN (29949, 28451)"), "each proof item class has one template");
            Assert.AreEqual(6L, Scalar(world, "SELECT COUNT(*) FROM itemtemplate WHERE id IN (122719, 122720, 122721, 130322, 130323, 130324)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1812, 1813, 1988, 2014, 1795)"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 1244001 AND 1244099"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 1244100 AND 1244499"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_item_set WHERE item_set_id BETWEEN 1244800 AND 1244899"));
            Assert.AreEqual(0L, Scalar(world, "SELECT COUNT(*) FROM content_rule WHERE id BETWEEN 1244900 AND 1244999"));
            // Commander Aldrin still spawns (OD-153).
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM spawnpool WHERE id = 510123 AND map_context_id = 1244 AND creature_1_max_count = 1"));
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var (table, filter) in new[]
                     {
                         ("npc_mission", ""), ("npc_mission_objective", ""), ("npc_mission_objective_binding", ""), ("npc_mission_objective_counter", ""),
                         ("npc_mission_objective_transition", ""), ("npc_mission_reward", ""), ("npc_mission_prerequisite", ""), ("npc_package", ""),
                         ("content_placement", ""), ("spawnpool", " WHERE id = 510196"), ("teleporter", "")
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
