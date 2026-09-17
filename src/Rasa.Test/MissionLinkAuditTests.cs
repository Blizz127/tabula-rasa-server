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
        // Missions recorded without a giver on purpose. 321 has neither giver nor receiver in the client tables
        // (mission-research.md, 2026-09-13); the other four are dispensed by a content rule instead of by an NPC,
        // so a giver creature would be wrong rather than missing:
        //   1990 Initiation      - the boot camp's forced radio offer on entering the camp (S1)
        //   1526 Training Day    - the forced radio offer on arriving at Alia Das (W1, GAP-W1-OFFER-RULE)
        //   2010/2011 class gear - dispatched on the class choice (W2, OD-43)
        private static readonly HashSet<long> KnownUnplaced = new() { 321, 1526, 1990, 2010, 2011 };

        /// <summary>
        /// The objectives that still complete through a dialogue package no NPC in the world carries, named one by
        /// one so a new one cannot appear quietly. Each is a mission a player can accept and then not finish: the
        /// client's objectiveconversation row says which package completes it, and the server only offers a
        /// conversation from a creature carrying that package.
        ///
        /// It started at 34. WildernessDialogueBinding closed 15 of them without building anything, because the
        /// NPCs were already in the world seed - named, classed, levelled and spawned - with no npc_package row
        /// to speak through. Look there first before reaching for the OD-45 pipeline that built Mining Coord.
        /// Richards. GAP-W3-UNBOUND-CONVERSATION-PACKAGE.
        /// </summary>
        private static readonly HashSet<(long Mission, long Objective, long Package)> UnboundPackages = new()
        {
            (321, 310, 105), (332, 2, 32), (332, 3, 98),
            (382, 1, 177), (442, 2, 1486), (451, 3, 569),
            (670, 2, 145), (836, 1, 802), (969, 3, 1065),
            (969, 4, 1092), (977, 2, 1075), (977, 3, 1051),
            (1040, 2, 1118), (1040, 3, 1117), (1119, 1, 1203),
            (1125, 1, 1200), (1183, 1, 1273), (1186, 1, 1300),
            (1310, 1, 1200)
        };

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
            var closed = new List<string>();
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var triple = (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2));
                    if (carried.Contains(reader.GetInt64(2)))
                    {
                        if (UnboundPackages.Contains(triple))
                            closed.Add($"mission {triple.Item1}/{triple.Item2} package {triple.Item3}");
                        continue;
                    }

                    if (!UnboundPackages.Contains(triple))
                        problems.Add($"mission {reader.GetInt64(0)}/{reader.GetInt64(1)} ({reader.GetString(3)}) completes through package {reader.GetInt64(2)}, which no spawned creature carries");
                }

            Assert.AreEqual(0, problems.Count,
                "these objectives complete through a package no spawned creature carries, and are not in the recorded set:\n"
                + string.Join("\n", problems));
            // A closed one must leave the list, or the list stops meaning anything.
            Assert.AreEqual(0, closed.Count,
                "these objectives now have their NPC and must be taken out of UnboundPackages:\n" + string.Join("\n", closed.Distinct()));
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
