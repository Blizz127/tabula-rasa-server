using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rasa.ClientData
{
    /// <summary>
    /// Answers whether a point sits in the air inside a set of collision meshes: straight below it the first of their
    /// surfaces is a floor (facing up, no steeper than a walkable slope) and straight above it the first is a ceiling
    /// (facing down).
    ///
    /// The terrain heightmap runs on through buildings and tunnels sunk into it. The client walks players through
    /// them - Pravus Research's entrance ramp climbs from the interior at 8 m to the tunnel jamb at 41.6 m through the
    /// facility plateau's flat 40.27 m heightmap - so the heightmap cannot be solid there. Fed to Recast, that stretch
    /// of terrain is a sheet across the tunnel: less than an agent height above the ramp it walls the ramp off, and
    /// higher up it is a floating floor. MapGeometry leaves such terrain out for the meshes a map's terrain cuts name
    /// (Rasa.NavMesh data/terrain_cuts.csv).
    ///
    /// The index is built from those meshes only. Over every placed mesh the same test also fires under rock
    /// overhangs, trees and pond surfaces, where the terrain is real ground: built that way, Pravus Research lost
    /// terrain across its hills and the AFS camp was cut off from the Frontlines.
    ///
    /// Near-vertical triangles are ignored: a vertical ray does not meet a wall, only the floors and ceilings between them.
    /// </summary>
    public sealed class EnclosureIndex
    {
        public const float CellSize = 4f;

        /// <summary>Floors and ceilings further than this from the point do not count.</summary>
        public const float MaxReach = 64f;

        /// <summary>Normal y of the steepest floor that counts (50 degrees, the agent's walkable slope).</summary>
        public const float MinFloorNormalY = 0.6428f;

        private const float MinSurfaceNormalY = 0.05f;

        private readonly List<float> _vertices;
        private readonly List<int> _triangles;
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

        /// <param name="vertices">x, y, z triples</param>
        /// <param name="triangles">index triples into <paramref name="vertices"/>; every triangle is indexed</param>
        public EnclosureIndex(List<float> vertices, List<int> triangles)
        {
            _vertices = vertices;
            _triangles = triangles;

            for (var t = 0; t < triangles.Count; t += 3)
            {
                var a = Vertex(triangles[t]);
                var b = Vertex(triangles[t + 1]);
                var c = Vertex(triangles[t + 2]);
                var n = Normal(a, b, c);

                if (float.IsNaN(n.Y) || Math.Abs(n.Y) < MinSurfaceNormalY)
                    continue;

                var x0 = Cell(Math.Min(a.X, Math.Min(b.X, c.X)));
                var x1 = Cell(Math.Max(a.X, Math.Max(b.X, c.X)));
                var z0 = Cell(Math.Min(a.Z, Math.Min(b.Z, c.Z)));
                var z1 = Cell(Math.Max(a.Z, Math.Max(b.Z, c.Z)));

                for (var x = x0; x <= x1; x++)
                    for (var z = z0; z <= z1; z++)
                    {
                        var key = Key(x, z);

                        if (!_cells.TryGetValue(key, out var list))
                            _cells[key] = list = new List<int>();

                        list.Add(t);
                    }
            }
        }

        public bool IsEnclosed(Vector3 p)
        {
            if (!_cells.TryGetValue(Key(Cell(p.X), Cell(p.Z)), out var list))
                return false;

            var below = float.NegativeInfinity;
            var belowNormalY = 0f;
            var above = float.PositiveInfinity;
            var aboveNormalY = 0f;

            foreach (var t in list)
            {
                var a = Vertex(_triangles[t]);
                var b = Vertex(_triangles[t + 1]);
                var c = Vertex(_triangles[t + 2]);

                if (!HeightAt(a, b, c, p.X, p.Z, out var y))
                    continue;

                var ny = Normal(a, b, c).Y;

                if (y <= p.Y)
                {
                    if (y > below)
                    {
                        below = y;
                        belowNormalY = ny;
                    }
                }
                else if (y < above)
                {
                    above = y;
                    aboveNormalY = ny;
                }
            }

            return p.Y - below <= MaxReach && above - p.Y <= MaxReach
                   && belowNormalY >= MinFloorNormalY && aboveNormalY < 0f;
        }

        private Vector3 Vertex(int i) => new Vector3(_vertices[i * 3], _vertices[i * 3 + 1], _vertices[i * 3 + 2]);

        private static Vector3 Normal(Vector3 a, Vector3 b, Vector3 c) => Vector3.Normalize(Vector3.Cross(b - a, c - a));

        private static int Cell(float v) => (int)Math.Floor(v / CellSize);

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        /// <summary>The triangle's height at (x, z) when (x, z) is inside its footprint.</summary>
        private static bool HeightAt(Vector3 a, Vector3 b, Vector3 c, float x, float z, out float y)
        {
            y = 0f;
            var d = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);

            if (Math.Abs(d) < 1e-6f)
                return false;

            var u = ((b.Z - c.Z) * (x - c.X) + (c.X - b.X) * (z - c.Z)) / d;
            var v = ((c.Z - a.Z) * (x - c.X) + (a.X - c.X) * (z - c.Z)) / d;
            var w = 1f - u - v;

            if (u < 0f || v < 0f || w < 0f)
                return false;

            y = u * a.Y + v * b.Y + w * c.Y;
            return true;
        }
    }
}
