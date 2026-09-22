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
        ///
        /// 2026-09-19: DevilsDenFransisco closed 670/2 and QuestNpcDialogueBatch eight more, each an NPC TaRapedia
        /// gives a /loc for. 1112/1 joined the list the same day: Captain Reyko carried the Irendas console's line
        /// and now carries his own, so that objective waits for the console, which no source places. The nine left
        /// are the ones no source places: 321 is not offered at all, 332's two delivery points, 442/2 (an analyser's
        /// readout, and the Duncan in the world already speaks for another mission), 451/3 and 977/2 (speakers no
        /// page names), 836/1 (Lieutenant Seguine, no page), and 1186/1 (an Eloh artifact, not a person).
        ///
        /// 2026-09-21: MissionPropSpeakers closed 442/2 and 1186/1. Neither was a person - the client's own tables
        /// name the first the Blood Analyzation Terminal (usablenameoverride 73) and the second's line ends "The
        /// obelisk is too large to move on your own" - and both now stand in the world as creatures on a class
        /// that can be spoken to, the shape TarapediaMachineClass established for a talking machine. 451/3 stays:
        /// its speaker has no name in any source and the owner chose not to invent one.
        /// </summary>
        private static readonly HashSet<(long Mission, long Objective, long Package)> UnboundPackages = new()
        {
            (451, 3, 569)
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
                // The placement's package wins when set (CreatureManager), else the npc_package row - so a creature
                // that a placement spawns speaks the placement's package and nothing else. Counting its npc_package
                // row as well is what let Field Lt. Brody's correction pass while he still carried the old one in
                // play (2026-09-19): only a creature with no placement of its own speaks from that table.
                packages.CommandText = @"
                    SELECT p.creature_id, CASE WHEN p.npc_package_id <> 0 THEN p.npc_package_id ELSE COALESCE(n.package_id, 0) END
                    FROM content_placement p LEFT JOIN npc_package n ON n.id = p.creature_id WHERE p.kind = 1 AND p.creature_id <> 0
                    UNION ALL
                    SELECT n.id, n.package_id FROM npc_package n
                    WHERE NOT EXISTS (SELECT 1 FROM content_placement p WHERE p.kind = 1 AND p.creature_id = n.id)";
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

        /// <summary>
        /// Every map marker names a teleporter or a crafting station the world has. The markers are keyed by the
        /// client's own marker entity ids (InfiniteRasa 492954a, generated from uimapmarker by position match), and
        /// the state the map window draws comes from the row each one points at - a marker pointing at nothing would
        /// simply never light up.
        /// </summary>
        [TestMethod]
        public void EveryMapMarkerPointsAtSomethingInTheWorld()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT m.marker_entity_id, m.object_kind, m.object_id, m.comment FROM map_marker m
                WHERE (m.object_kind = 1 AND NOT EXISTS (SELECT 1 FROM teleporter t WHERE t.id = m.object_id))
                   OR (m.object_kind = 2 AND NOT EXISTS (SELECT 1 FROM kraftwerks k WHERE k.id = m.object_id))";

            var problems = new List<string>();
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    problems.Add($"marker {reader.GetInt64(0)} ({reader.GetString(3)}) points at {(reader.GetInt64(1) == 1 ? "teleporter" : "kraftwerks")} {reader.GetInt64(2)}, which the world does not have");

            Assert.AreEqual(0, problems.Count, string.Join(" | ", problems));

            using var count = connection.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM map_marker";
            Assert.AreEqual(307L, (long)count.ExecuteScalar());
        }

        /// <summary>
        /// Every dropship pad the travel window offers stands somewhere. Ligo Crucible (359) carried no position and
        /// no map, so the zone could not be flown to at all, and "Shadow Edge Post" (536) was a second row 0.4 m from
        /// Ashen Desert (263) with a name the client cannot translate, so the window listed the same place twice
        /// (InfiniteRasa 492954a).
        /// </summary>
        [TestMethod]
        public void TheDropshipPadsAreReachable()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, description, map_context_id, pos_x, pos_y, pos_z FROM teleporter WHERE type = 4";

            var problems = new List<string>();
            var seen = new List<(long Id, string Name, double X, double Y, double Z, long Map)>();

            using (var reader = command.ExecuteReader())
                while (reader.Read())
                {
                    var row = (reader.GetInt64(0), reader.GetString(1), reader.GetDouble(3), reader.GetDouble(4), reader.GetDouble(5), reader.GetInt64(2));

                    if (row.Item6 == 0 || (row.Item3 == 0 && row.Item4 == 0 && row.Item5 == 0))
                        problems.Add($"pad {row.Item1} ({row.Item2}) has no position or no map");

                    foreach (var other in seen)
                        if (other.Map == row.Item6 && System.Math.Abs(other.X - row.Item3) < 2 && System.Math.Abs(other.Z - row.Item5) < 2)
                            problems.Add($"pad {row.Item1} ({row.Item2}) stands on pad {other.Id} ({other.Name})");

                    seen.Add(row);
                }

            Assert.AreEqual(0, problems.Count, string.Join(" | ", problems));
        }

        /// <summary>
        /// Every hospital stands on a map that has something on it.
        ///
        /// A hospital is where a dead player is put back down, so a hospital on a map nothing else occupies is a
        /// map whose dead have nowhere to go - and a map with a hospital and nothing else is a row that was filed
        /// under the wrong context. That is exactly what "Hospital: CELLAR Arena Medic" was: it sat on 2259, the
        /// wargame indoor arena, which carries no spawn, no placement and no map link, while the medic who
        /// defines it stands at the identical coordinates on 20000009 and the client's own marker for it (UI map
        /// key 2232) says 20000009 too.
        /// </summary>
        [TestMethod]
        public void EveryHospitalIsOnAMapThatCarriesSomething()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT t.id, t.description, t.map_context_id, " +
                "  (SELECT COUNT(*) FROM spawnpool s WHERE s.map_context_id = t.map_context_id) " +
                "+ (SELECT COUNT(*) FROM content_placement p WHERE p.map_context_id = t.map_context_id) " +
                "+ (SELECT COUNT(*) FROM map_link l WHERE l.map_context_id = t.map_context_id) " +
                "FROM teleporter t WHERE t.type = 5 AND t.map_context_id <> 0";

            // The Gauntlet's two hospitals (513, 535) sit on adv_zepic_pve_arena (2278), a client arena this world
            // holds no content for at all. They are on the right map - the map is simply unbuilt - which is a
            // different thing from the arena medic filed under the wrong context that this guard exists for.
            var unbuiltArena = new HashSet<long> { 513, 535 };

            var stranded = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                if (reader.GetInt64(3) == 0 && !unbuiltArena.Contains(reader.GetInt64(0)))
                    stranded.Add($"{reader.GetString(1)} (teleporter {reader.GetInt64(0)}) is on map {reader.GetInt64(2)}, which carries nothing at all");

            Assert.AreEqual(0, stranded.Count, string.Join("\n", stranded));
        }

        /// <summary>
        /// A prop that finishes a mission by being spoken to needs the client's Creature augmentation (1), or
        /// CreatureManager cannot build an actor for it at all, and the NPC augmentation (52), or the client has
        /// nothing to open a conversation on. The Blood Analyzation Terminal and the Eloh obelisk stand on such
        /// classes; entityclass is seed data, so this is checked against the real world rather than the migrations.
        /// </summary>
        [TestMethod]
        public void TheTalkingPropsStandOnClassesThatCanBeSpokenTo()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT c.id, e.aug_list FROM creature c JOIN entityclass e ON e.id = c.class_id WHERE c.id IN (199912, 199913)";
            var seen = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                seen++;
                var augmentations = ("," + reader.GetString(1).Replace(" ", "") + ",");
                Assert.IsTrue(augmentations.Contains(",1,"), $"prop {reader.GetInt64(0)} has no Creature augmentation: {reader.GetString(1)}");
                Assert.IsTrue(augmentations.Contains(",52,"), $"prop {reader.GetInt64(0)} has no NPC augmentation: {reader.GetString(1)}");
            }
            Assert.AreEqual(2, seen, "both talking props are in the world");
        }

        /// <summary>The fifteen classes once labelled Missing_ItemClassId carry the names the client gives them.</summary>
        [TestMethod]
        public void NoEntityClassIsStillAPlaceholderName()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM entityclass WHERE class_name LIKE 'Missing_ItemClassId%'";
            Assert.AreEqual(0L, (long)command.ExecuteScalar(), "classes still carrying a placeholder name");
            command.CommandText = "SELECT COUNT(*) FROM entityclass WHERE (id = 3180 AND class_name = 'Rifle Ammo') OR (id = 4327 AND class_name = 'Botany Kit')";
            Assert.AreEqual(2L, (long)command.ExecuteScalar(), "Rifle Ammo and the Botany Kit carry the client's names");
        }

        /// <summary>
        /// Lieutenant Burke is the one original-seed spawn CodexNpcCorrections overrules, on two independent
        /// sources 4 m apart that both name Monarch Grove, plus the seed row's own height matching the new spot
        /// and not the old. Council Elder Solis, the other seed candidate, is deliberately left where the seed
        /// puts him (GAP-SOLIS-SEED-POSITION).
        /// </summary>
        [TestMethod]
        public void BurkeStandsAtMonarchGroveAndSolisIsLeftToTheSeed()
        {
            using var connection = OpenWorld();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM spawnpool WHERE id = 171 AND pos_x = -46 AND pos_z = 418 AND map_context_id = 1220";
            Assert.AreEqual(1L, (long)command.ExecuteScalar(), "Lieutenant Burke at Monarch Grove");
            command.CommandText = "SELECT COUNT(*) FROM spawnpool WHERE id = 184 AND pos_x <> 784.7";
            Assert.AreEqual(1L, (long)command.ExecuteScalar(), "Council Elder Solis where the seed put him");
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
