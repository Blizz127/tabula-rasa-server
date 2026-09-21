using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    /// <summary>
    /// No body stands inside the furniture.
    ///
    /// The floor sweep (WorldFloorSweepTests) proves every NPC stands on walkable ground, and it cannot see this:
    /// a cot is half a metre tall, so an NPC standing in one reads as being on the floor. A live report on
    /// 2026-09-20 - "npc placement seems off too", with a shot of an NPC in a cot at Alia Das - is what this
    /// checks for. The props come from the client's own map files (see mapprops/README.md) and are therefore
    /// original-tier evidence of where a body cannot stand.
    ///
    /// Only furniture is carried: buildings, tents and walls are left out of the data, because an NPC is meant to
    /// stand inside those.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PropOverlapAuditTests
    {
        /// <summary>How far above the body's feet a prop's top has to reach before the body is inside it.</summary>
        private const double Bite = 0.15;

        /// <summary>
        /// Bodies that stand in a prop on purpose, or whose position is sourced from something better than this
        /// data. Each one names why; nothing goes in here to silence a measurement that was never made.
        /// </summary>
        private static readonly Dictionary<string, string> Allowed = new()
        {
            { "placement 199700", "mission 430's mortar, placed on the client map's own mortar launcher" },
            { "placement 199701", "mission 430's mortar, placed on the client map's own mortar launcher" },
            { "placement 199702", "mission 430's mortar, placed on the client map's own mortar launcher" },
            { "placement 199703", "mission 430's mortar, placed on the client map's own mortar launcher" },
            { "placement 198652", "the 1992 practice dummy stands on the gate catwalk, measured from footage" },
            { "placement 198653", "the 1992 target dummy stands on the gate catwalk, measured from footage" },
            { "placement 198661", "Thrax Initiate courtyard.2, position measured from D11 footage" },
            { "placement 198664", "Thrax Initiate courtyard.5, position measured from D11 footage" },
            { "pool 155", "Weapon Supply Twin Pillars is the original server's own spawn, behind its counter" }
        };

        [TestMethod]
        public void NoBodyStandsInsideAProp()
        {
            var root = RepositoryRoot();
            using var connection = OpenWorld(root);

            var maps = new Dictionary<long, string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT map_context_id, map_name FROM map_info";
                using var reader = command.ExecuteReader();
                while (reader.Read()) maps[reader.GetInt64(0)] = reader.GetString(1);
            }

            var bodies = Read(connection,
                    "SELECT id, map_context_id, pos_x, pos_y, pos_z, COALESCE(comment, '') FROM content_placement",
                    "placement")
                .Concat(Read(connection,
                    "SELECT s.id, s.map_context_id, s.pos_x, s.pos_y, s.pos_z, COALESCE(c.comment, '') " +
                    "FROM spawnpool s LEFT JOIN creature c ON c.id = s.creature_1_Id",
                    "pool"))
                .GroupBy(body => body.Map);

            var inside = new List<string>();
            var checkedBodies = 0;

            foreach (var group in bodies)
            {
                if (!maps.TryGetValue(group.Key, out var mapName)) continue;
                var propsPath = Path.Combine(root, "mapprops", mapName + ".csv");
                if (!File.Exists(propsPath)) continue;

                var props = ReadProps(propsPath);

                foreach (var body in group)
                {
                    checkedBodies++;
                    var key = $"{body.Kind} {body.Id}";

                    foreach (var prop in props)
                    {
                        var dx = body.X - prop.X;
                        var dz = body.Z - prop.Z;
                        if (Math.Abs(dx) > 4 || Math.Abs(dz) > 4) continue;

                        // The map turns props about Y only, so the point goes into the prop's own frame first.
                        var cos = Math.Cos(-prop.Yaw);
                        var sin = Math.Sin(-prop.Yaw);
                        var lx = dx * cos - dz * sin;
                        var lz = dx * sin + dz * cos;

                        if (lx < prop.MinX || lx > prop.MaxX || lz < prop.MinZ || lz > prop.MaxZ) continue;
                        if (prop.Y + prop.MaxY - body.Y < Bite) continue;
                        if (Allowed.ContainsKey(key)) break;

                        inside.Add(string.Format(CultureInfo.InvariantCulture,
                            "{0} on {1} stands {2:0.##} m inside prop class {3} at {4:0.#},{5:0.#} - {6}",
                            key, mapName, prop.Y + prop.MaxY - body.Y, prop.ClassId, body.X, body.Z, body.Comment));
                        break;
                    }
                }
            }

            Assert.IsTrue(checkedBodies > 500, $"the world should have its bodies, checked {checkedBodies}");
            Assert.AreEqual(0, inside.Count, "bodies standing inside the map's own furniture:\n" + string.Join("\n", inside));
        }

        private readonly struct Prop
        {
            public Prop(long classId, double x, double y, double z, double yaw,
                double minX, double maxX, double minZ, double maxZ, double maxY)
            {
                ClassId = classId; X = x; Y = y; Z = z; Yaw = yaw;
                MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; MaxY = maxY;
            }

            public long ClassId { get; } public double X { get; } public double Y { get; } public double Z { get; }
            public double Yaw { get; } public double MinX { get; } public double MaxX { get; }
            public double MinZ { get; } public double MaxZ { get; } public double MaxY { get; }
        }

        private static List<Prop> ReadProps(string path)
        {
            var props = new List<Prop>();
            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var f = line.Split(',');
                if (f.Length < 10) continue;
                props.Add(new Prop(long.Parse(f[0], CultureInfo.InvariantCulture),
                    Number(f[1]), Number(f[2]), Number(f[3]), Number(f[4]),
                    Number(f[5]), Number(f[6]), Number(f[7]), Number(f[8]), Number(f[9])));
            }
            return props;
        }

        private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

        private readonly struct Body
        {
            public Body(string kind, long id, long map, double x, double y, double z, string comment)
            { Kind = kind; Id = id; Map = map; X = x; Y = y; Z = z; Comment = comment; }
            public string Kind { get; } public long Id { get; } public long Map { get; }
            public double X { get; } public double Y { get; } public double Z { get; } public string Comment { get; }
        }

        private static List<Body> Read(SqliteConnection connection, string sql, string kind)
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
