using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Navigation;

namespace Rasa.Test
{
    /// <summary>
    /// The camp audit (PlacementHeightAuditTests) proves the bootcamp stands on its floor. This one does the same
    /// sweep for the whole world: every content placement and every spawn pool on every map we have a navmesh for,
    /// ours and the ones taken in from upstream alike.
    ///
    /// It exists because a live report found an NPC standing on a cot at Alia Das - a body half a metre above the
    /// floor, which no audit we had could see: the camp audit only reads map 1985, and the mission/position audits
    /// only read content_placement, so the several hundred upstream spawn pools were never checked at all.
    ///
    /// Two thresholds, because they mean different things:
    ///   - Defect (>1.5 m, or no walkable surface at all): the body is in the air or inside the terrain. Nobody can
    ///     reach it and in the worst case it is invisible. These fail the test.
    ///   - Suspect (>0.4 m): the body is standing on something - a crate, a cot, a step - rather than on the floor.
    ///     Original map data does this on purpose in places, so these are written to the report, not failed.
    /// The report goes to world-floor-sweep.txt at the repository root so the full list survives a truncated
    /// assertion message.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WorldFloorSweepTests
    {
        private const double DefectTolerance = 1.5;
        private const double SuspectTolerance = 0.5;

        /// <summary>
        /// The floor is the navmesh surface less 0.276 m - the offset the original server's own creature spawns
        /// sit at, measured in WorldPlacementFloorSnapRows. Measuring against the raw surface would read every
        /// correctly standing body as a quarter metre buried.
        /// </summary>
        private const double Offset = -0.276;

        /// <summary>
        /// Placements and pools that are meant to sit off the floor, with the reason. Anything listed here is a
        /// deliberate piece of world data; a measurement that was never made does not belong in this list.
        /// </summary>
        private static readonly Dictionary<string, string> OffGround = new()
        {
            { "placement 198677", "the bomb rests on the wreck hull (+1.06 m)" },
            { "pool 9", "AFS_Turret_Mini is mounted on its post, not standing (+1.27 m)" },
            { "pool 106", "AFS_Turret_Mini is mounted on its post, not standing" },
            { "pool 107", "AFS_Turret_Mini is mounted on its post, not standing" }
        };

        [TestMethod]
        public void EveryBodyInTheWorldStandsOnTheWalkableSurface()
        {
            var root = RepositoryRoot();
            using var connection = OpenWorld(root);

            var maps = new Dictionary<long, string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT map_context_id, map_name FROM map_info";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    maps[reader.GetInt64(0)] = reader.GetString(1);
            }

            var rows = ReadRows(connection,
                    "SELECT id, map_context_id, pos_x, pos_y, pos_z, COALESCE(comment, '') FROM content_placement",
                    "placement")
                .Concat(ReadRows(connection,
                    "SELECT s.id, s.map_context_id, s.pos_x, s.pos_y, s.pos_z, COALESCE(c.comment, '') " +
                    "FROM spawnpool s LEFT JOIN creature c ON c.id = s.creature_1_Id",
                    "pool"))
                .GroupBy(row => row.Map)
                .OrderBy(group => group.Key)
                .ToList();

            var defects = new List<string>();
            var suspects = new List<string>();
            var report = new List<string>();
            var probed = 0;
            var withoutMesh = new List<string>();

            foreach (var group in rows)
            {
                if (!maps.TryGetValue(group.Key, out var mapName))
                {
                    withoutMesh.Add($"map {group.Key} is not in map_info ({group.Count()} bodies)");
                    continue;
                }

                var meshPath = Path.Combine(root, "navmesh", mapName + ".nav");
                if (!File.Exists(meshPath))
                {
                    withoutMesh.Add($"map {group.Key} {mapName} has no navmesh ({group.Count()} bodies)");
                    continue;
                }

                var query = new NavMeshQuery(NavMeshFile.Read(meshPath));

                foreach (var row in group.OrderBy(row => row.Id))
                {
                    probed++;
                    var ground = query.GroundHeight(new Vector3((float)row.X, (float)row.Y, (float)row.Z));
                    var key = $"{row.Kind} {row.Id}";

                    if (ground == null)
                    {
                        var line = string.Format(CultureInfo.InvariantCulture,
                            "{0} on {1}: no walkable surface at {2:0.#},{3:0.##},{4:0.#} - {5}",
                            key, mapName, row.X, row.Y, row.Z, row.Comment);
                        report.Add("MISSING  " + line);
                        if (!OffGround.ContainsKey(key))
                            defects.Add(line);
                        continue;
                    }

                    var delta = row.Y - (ground.Value + Offset);
                    if (Math.Abs(delta) <= SuspectTolerance)
                        continue;

                    var text = string.Format(CultureInfo.InvariantCulture,
                        "{0} on {1}: {2:+0.##;-0.##} m off the floor (y {3:0.##}, ground {4:0.##}) at {5:0.#},{6:0.#} - {7}",
                        key, mapName, delta, row.Y, ground.Value + Offset, row.X, row.Z, row.Comment);

                    if (OffGround.ContainsKey(key))
                    {
                        report.Add("ALLOWED  " + text);
                        continue;
                    }

                    if (Math.Abs(delta) > DefectTolerance)
                    {
                        report.Add("DEFECT   " + text);
                        defects.Add(text);
                    }
                    else
                    {
                        report.Add("SUSPECT  " + text);
                        suspects.Add(text);
                    }
                }
            }

            report.Insert(0, $"probed {probed} bodies; {defects.Count} defects, {suspects.Count} suspects");
            report.InsertRange(1, withoutMesh.Select(line => "NOMESH   " + line));
            File.WriteAllLines(Path.Combine(root, "world-floor-sweep.txt"), report);

            Assert.IsTrue(probed > 500, $"the world should have its bodies, probed {probed}");
            Assert.AreEqual(0, defects.Count,
                "bodies that do not stand where a body can walk (full report in world-floor-sweep.txt):\n"
                + string.Join("\n", defects.Take(60)));
        }

        private readonly struct Body
        {
            public Body(string kind, long id, long map, double x, double y, double z, string comment)
            {
                Kind = kind; Id = id; Map = map; X = x; Y = y; Z = z; Comment = comment;
            }

            public string Kind { get; }
            public long Id { get; }
            public long Map { get; }
            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public string Comment { get; }
        }

        private static List<Body> ReadRows(SqliteConnection connection, string sql, string kind)
        {
            var bodies = new List<Body>();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                bodies.Add(new Body(kind, reader.GetInt64(0), reader.GetInt64(1), reader.GetDouble(2),
                    reader.GetDouble(3), reader.GetDouble(4), reader.GetString(5)));
            return bodies;
        }

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
                Assert.Inconclusive("rasaworld.db is not in the repository root; this audit reads the world database");
            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }
    }
}
