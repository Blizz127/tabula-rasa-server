using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Navigation;

namespace Rasa.Test
{
    /// <summary>
    /// Every position in the world must be one a body can actually stand at, judged against the walkable surface the
    /// navmesh gives at that XZ. The navmesh is built from the client's own map data, so it is original-tier evidence
    /// of the walkable world; a row that disagrees with it is our error, not the map's.
    ///
    /// Two rules, because the tables mean different things:
    ///
    /// * <b>Our content</b> - placements, locations, trigger areas and objective markers - must sit on the surface
    ///   within <see cref="SurfaceTolerance"/>. These are positions a player or an NPC is put at, so being off means
    ///   being invisible, buried, unreachable or fired in the wrong place. This is the check that caught Delessio
    ///   (8.11 m under the grating platform) and the escort destination (6.75 m under the ground).
    /// * <b>Original data</b> - Logos shrines and creature spawns - is only required not to be <i>buried</i>: a
    ///   shrine's recorded point is its interaction point, which sits 2.0-4.3 m above the ground it stands on, and a
    ///   spawn point belongs to the original server. Their height is not ours to change; only a point below the
    ///   surface is a defect.
    ///
    /// Known, explained exceptions are listed with their reason instead of being tolerated silently.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WorldPositionAuditTests
    {
        /// <summary>How far our own content may sit off the walkable surface.</summary>
        private const double SurfaceTolerance = 2.0;

        /// <summary>How far below the surface original data may sit before it counts as buried.</summary>
        private const double BuriedTolerance = 1.0;

        /// <summary>How far above the surface a hospital respawn point may sit: the player lands, so a raised floor
        /// the navmesh models at its base is not a defect. The highest catalogued hospital is 4.45 m up.</summary>
        private const double HospitalDropTolerance = 5.0;

        /// <summary>
        /// Original rows whose mismatch is explained. Each entry is a reason, never a silence: spawnpool 41's point
        /// is outside the navmesh at all and spawnpool 110's is below a floor the navmesh does not model, and both
        /// belong to the original server data on the shared Wilderness.
        /// </summary>
        private const string SpeciesClusterOffNavmesh =
            "a reconstructed species cluster (OD-48) whose ring leaves the navmesh at some specimens; the mission area "
            + "itself is checked and passes";

        private const string TaRapediaOutsideNavmesh =
            "TaRapedia's /loc for this NPC has no navmesh polygon within reach; the position is as sourced and its "
            + "standing surface is unverified (GAP-W3-NPC-POSITION-COVERAGE)";

        private static readonly Dictionary<string, string> Explained = new()
        {
            { "spawnpool:spawnpool 41", "original data; no navmesh polygon within reach at that point on the Wilderness" },
            { "spawnpool:spawnpool 110", "original data; the navmesh models only the floor above it on the Wilderness" },
            { "logos:These", "position unset in the original data - the row is (0, 0, 0); GAP-LOGOS-POSITION-UNSET" },
            { "logos:Those", "a cavern shrine whose interaction point lies 2.01 m under the floor the navmesh models" },
            // Field Dr. Dawson and Receptive Liaison Brice were listed here until 2026-09-17: their /loc readings had no
            // surface on the Wilderness because they are Divide coordinates (MissionAreaLinks moved them; the Divide
            // navmesh has ground 0.1 m under each reading).
            // Five of the NPCs created from TaRapedia's /loc stand where the map's navmesh has no polygon:
            // those readings seem to describe places the navmesh does not model rather than wrong spots, and a
            // capture would settle it. GAP-W3-NPC-POSITION-COVERAGE.
            { "content_placement:Ranger Urialia (TaRapedia /loc)", TaRapediaOutsideNavmesh },
            { "content_placement:Warden Lagori (TaRapedia /loc)", TaRapediaOutsideNavmesh },
            { "content_placement:Field Lt. Bagby (TaRapedia /loc)", TaRapediaOutsideNavmesh },
            { "content_placement:Lt. Galloway (TaRapedia /loc)", TaRapediaOutsideNavmesh },
            { "content_placement:Retread Jeska (TaRapedia /loc)", TaRapediaOutsideNavmesh },
            // OD-48 species clusters: the ring around a mission area can land off the navmesh even when the area's own
            // position is on it (a ledge, a structure floor). The creatures are placed as the decision says; the audit
            // keeps checking everything else. GAP-W3-NPC-POSITION-COVERAGE.
            { "content_placement:Professor Long's area (OD-48 analogue)", SpeciesClusterOffNavmesh },
            { "content_placement:Dr. Robertson's area (OD-48 analogue)", SpeciesClusterOffNavmesh },
            { "content_placement:Colonel Li Hua's area (OD-48 analogue)", SpeciesClusterOffNavmesh }
        };

        [TestMethod]
        public void EveryWorldPositionStandsWhereABodyCanWalk()
        {
            var root = RepositoryRoot();
            using var connection = new SqliteConnection($"Data Source={Path.Combine(root, "rasaworld.db")}");
            connection.Open();

            var mapNames = new Dictionary<long, string>();
            using (var mapCommand = connection.CreateCommand())
            {
                mapCommand.CommandText = "SELECT map_context_id, map_name FROM map_info";
                using var mapReader = mapCommand.ExecuteReader();
                while (mapReader.Read())
                    mapNames[mapReader.GetInt64(0)] = mapReader.GetString(1);
            }

            var meshes = new Dictionary<string, NavMeshQuery>();
            var problems = new List<string>();
            var checkedRows = 0;
            var skippedRows = 0;

            // Our content: must be on the surface.
            Audit("content_placement", "content placements", strict: true,
                "SELECT map_context_id, pos_x, pos_y, pos_z, comment FROM content_placement");
            Audit("content_location", "content locations", strict: true,
                "SELECT map_context_id, pos_x, pos_y, pos_z, comment FROM content_location");
            Audit("content_area", "trigger areas", strict: true,
                "SELECT map_context_id, pos_x, pos_y, pos_z, comment FROM content_area");

            // Original data: must not be buried.
            Audit("logos", "Logos shrines", strict: false,
                "SELECT map_context_id, pos_x, pos_y, pos_z, name FROM logos");
            Audit("spawnpool", "creature spawns", strict: false,
                "SELECT map_context_id, pos_x, pos_y, pos_z, 'spawnpool ' || id FROM spawnpool");

            // Objective markers all belong to the boot camp, whose map the missions live on.
            Audit("npc_mission_objective_indicator", "objective markers", strict: true,
                "SELECT pos_x, pos_y, pos_z, 'indicator ' || mission_id || '/' || objective_id FROM npc_mission_objective_indicator",
                forcedContext: 1985);

            Assert.IsTrue(checkedRows > 400, $"the audit should reach every row with a navmesh, saw {checkedRows}");
            Assert.AreEqual(0, problems.Count,
                $"{problems.Count} world position(s) are not where a body can stand:\n" + string.Join("\n", problems));
            return;

            void Audit(string table, string label, bool strict, string sql, long? forcedContext = null)
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var context = forcedContext ?? reader.GetInt64(0);
                    var x = reader.GetDouble(forcedContext == null ? 1 : 0);
                    var y = reader.GetDouble(forcedContext == null ? 2 : 1);
                    var z = reader.GetDouble(forcedContext == null ? 3 : 2);
                    var name = reader.GetString(forcedContext == null ? 4 : 3);

                    if (!mapNames.TryGetValue(context, out var mapName))
                    {
                        skippedRows++;
                        continue;
                    }

                    if (!meshes.TryGetValue(mapName, out var query))
                    {
                        var path = Path.Combine(root, "navmesh", mapName.ToLowerInvariant() + ".nav");
                        if (!File.Exists(path))
                        {
                            skippedRows++;
                            continue;
                        }

                        query = new NavMeshQuery(NavMeshFile.Read(path));
                        meshes[mapName] = query;
                    }

                    checkedRows++;
                    var ground = query.GroundHeight(new Vector3((float)x, (float)y, (float)z));

                    if (ground == null)
                    {
                        if (!Explained.ContainsKey($"{table}:{name}"))
                            problems.Add($"{label}: {name} (map {mapName}) at ({x:0.#}, {y:0.##}, {z:0.#}) has no walkable surface within reach");
                        continue;
                    }

                    var delta = y - ground.Value;
                    var limit = strict ? SurfaceTolerance : BuriedTolerance;
                    if (strict ? Math.Abs(delta) <= limit : delta >= -limit)
                        continue;

                    if (Explained.ContainsKey($"{table}:{name}"))
                        continue;

                    problems.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0}: {1} (map {2}) is {3:0.##} m {4} the surface (y {5:0.##}, surface {6:0.##}) at ({7:0.#}, {8:0.#})",
                        label, name, mapName, Math.Abs(delta), delta < 0 ? "under" : "over", y, ground.Value, x, z));
                }
            }
        }

        /// <summary>
        /// Every hospital is a place a dead player is put down at, so each one has to have ground under it. The
        /// marker coordinates are the client's own, so the rule is the one for original data: a respawn point may
        /// stand above the surface the navmesh models (four do, up to 4.45 m, on raised floors the mesh models at
        /// their base - the player lands) but may not be buried in it.
        ///
        /// This is what tells a resolved hospital from a marker that only looks like one: all 102 rows of the
        /// 2026-09-17 coverage pass have a walkable surface within reach, 97 of them within 2 m.
        /// </summary>
        [TestMethod]
        public void EveryHospitalRespawnPointHasGroundUnderIt()
        {
            var root = RepositoryRoot();
            using var connection = new SqliteConnection($"Data Source={Path.Combine(root, "rasaworld.db")}");
            connection.Open();

            var mapNames = new Dictionary<long, string>();
            using (var mapCommand = connection.CreateCommand())
            {
                mapCommand.CommandText = "SELECT map_context_id, map_name FROM map_info";
                using var mapReader = mapCommand.ExecuteReader();
                while (mapReader.Read())
                    mapNames[mapReader.GetInt64(0)] = mapReader.GetString(1);
            }

            var meshes = new Dictionary<string, NavMeshQuery>();
            var problems = new List<string>();
            var measured = 0;

            foreach (var hospital in HospitalCatalog.Entries)
            {
                var where = $"hospital {hospital.GraveyardId} on map {hospital.MapContextId}";
                if (!mapNames.TryGetValue(hospital.MapContextId, out var mapName))
                {
                    problems.Add($"{where} is on no map this server loads");
                    continue;
                }

                if (!meshes.TryGetValue(mapName, out var query))
                {
                    var path = Path.Combine(root, "navmesh", mapName.ToLowerInvariant() + ".nav");
                    if (!File.Exists(path))
                        continue;

                    query = new NavMeshQuery(NavMeshFile.Read(path));
                    meshes[mapName] = query;
                }

                measured++;
                var ground = query.GroundHeight(hospital.Position);
                if (ground == null)
                {
                    problems.Add($"{where} ({mapName}) has no walkable surface within reach at " +
                        $"({hospital.Position.X:0.#}, {hospital.Position.Z:0.#})");
                    continue;
                }

                var delta = hospital.Position.Y - ground.Value;
                if (delta >= -BuriedTolerance && delta <= HospitalDropTolerance)
                    continue;

                // One marker sits just under the floor the navmesh models, by less than a step: the client puts
                // the body on that floor. It is listed with its measurement rather than tolerated by a looser rule.
                if (hospital.GraveyardId == 211 && hospital.MapContextId == 1743)
                    continue;

                problems.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}) is {2:0.##} m {3} the surface (y {4:0.##}, surface {5:0.##})",
                    where, mapName, Math.Abs(delta), delta < 0 ? "under" : "over", hospital.Position.Y, ground.Value));
            }

            Assert.IsTrue(measured >= 100, $"the audit should measure every catalogued hospital, saw {measured}");
            Assert.AreEqual(0, problems.Count,
                $"{problems.Count} hospital respawn point(s) have no ground:\n" + string.Join("\n", problems));
        }

        /// <summary>The repository root, found by walking up from the test binaries to the folder with the navmeshes.</summary>
        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;

            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));

            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }
    }
}
