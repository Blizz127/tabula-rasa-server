using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    /// <summary>
    /// Every seeded mission must be reachable in the world: its giver and receiver have to be creatures that
    /// actually spawn, and every dialogue package its objectives complete through has to be carried by one of
    /// them. The mission-area audit of 2026-09-17 found six Wilderness missions whose giver sat in a world-seed
    /// spawnpool with counts 0/0 (never drawn) and one whose objectives named packages no NPC had, so this reads
    /// the repository's rasaworld.db the way the position audits do. Without that file (the image, a scratch copy
    /// made with --exclude '*.db') it is inconclusive rather than failed.
    /// </summary>
    [TestClass]
    public class MissionLinkAuditTests
    {
        // Missions recorded without a giver or receiver on purpose (mission-research.md, 2026-09-13).
        private static readonly HashSet<long> KnownUnplaced = new() { 321 };

        [TestMethod]
        public void EveryMissionGiverAndReceiverIsACreatureThatSpawns()
        {
            using var connection = OpenWorld();
            var spawned = SpawnedCreatures(connection);
            var problems = new List<string>();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, giver_id, reciver_id, comment FROM npc_mission ORDER BY id";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var missionId = reader.GetInt64(0);
                var comment = reader.GetString(3);
                foreach (var (role, column) in new[] { ("giver", 1), ("receiver", 2) })
                {
                    var creatureId = reader.GetInt64(column);
                    if (creatureId == 0)
                    {
                        if (!KnownUnplaced.Contains(missionId))
                            problems.Add($"mission {missionId} ({comment}) has no {role}");
                        continue;
                    }

                    if (!spawned.ContainsKey(creatureId))
                        problems.Add($"mission {missionId} ({comment}): {role} creature {creatureId} is never spawned by a placement or a spawnpool slot with a count");
                }
            }

            Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
        }

        [TestMethod]
        public void EveryObjectiveConversationPackageIsCarriedByASpawnedCreature()
        {
            using var connection = OpenWorld();
            var spawned = SpawnedCreatures(connection);

            var carried = new HashSet<long>();
            using (var packages = connection.CreateCommand())
            {
                // The placement's package wins when set (CreatureManager), else the npc_package row.
                packages.CommandText = @"
                    SELECT p.creature_id, CASE WHEN p.npc_package_id <> 0 THEN p.npc_package_id ELSE COALESCE(n.package_id, 0) END
                    FROM content_placement p LEFT JOIN npc_package n ON n.id = p.creature_id WHERE p.kind = 1 AND p.creature_id <> 0
                    UNION ALL SELECT n.id, n.package_id FROM npc_package n";
                using var reader = packages.ExecuteReader();
                while (reader.Read())
                    if (spawned.ContainsKey(reader.GetInt64(0)) && reader.GetInt64(1) != 0)
                        carried.Add(reader.GetInt64(1));
            }

            var problems = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT c.mission_id, c.objective_id, c.npc_package_id, m.comment
                FROM npc_mission_objective_conversation c JOIN npc_mission m ON m.id = c.mission_id
                ORDER BY c.mission_id, c.objective_id";
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    if (!carried.Contains(reader.GetInt64(2)))
                        problems.Add($"mission {reader.GetInt64(0)}/{reader.GetInt64(1)} ({reader.GetString(3)}) completes through package {reader.GetInt64(2)}, which no spawned creature carries");

            Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
        }

        /// <summary>Creature ids that exist in the world: placed, or in a spawnpool slot that draws at least one.</summary>
        private static Dictionary<long, string> SpawnedCreatures(SqliteConnection connection)
        {
            var spawned = new Dictionary<long, string>();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT creature_id, 'placement ' || id FROM content_placement WHERE kind = 1 AND creature_id <> 0
                UNION ALL SELECT creature_1_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_1_Id <> 0 AND creature_1_max_count >= 1
                UNION ALL SELECT creature_2_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_2_Id <> 0 AND creature_2_max_count >= 1
                UNION ALL SELECT creature_3_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_3_Id <> 0 AND creature_3_max_count >= 1
                UNION ALL SELECT creature_4_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_4_Id <> 0 AND creature_4_max_count >= 1
                UNION ALL SELECT creature_5_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_5_Id <> 0 AND creature_5_max_count >= 1
                UNION ALL SELECT creature_6_Id, 'spawnpool ' || id FROM spawnpool WHERE creature_6_Id <> 0 AND creature_6_max_count >= 1";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                spawned.TryAdd(reader.GetInt64(0), reader.GetString(1));
            return spawned;
        }

        private static SqliteConnection OpenWorld()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));

            var path = directory == null ? null : Path.Combine(directory, "rasaworld.db");
            if (path == null || !File.Exists(path))
                Assert.Inconclusive("rasaworld.db is not next to the navmesh folder; this audit reads the repository's world database");

            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }
    }
}
