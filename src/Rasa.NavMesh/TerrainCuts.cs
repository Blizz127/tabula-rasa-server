using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Rasa.NavMesh
{
    /// <summary>
    /// <c>data/terrain_cuts.csv</c>: per map, the meshes the terrain heightmap must not run through
    /// (<c>map,mesh_prefix,reason</c>; a mesh file name starting with the prefix counts). Where a
    /// building or tunnel is sunk into a hill the heightmap carries on through it, and Recast would
    /// take that stretch of terrain for a floor or a ceiling inside it. The client walks players
    /// through those spaces, so their terrain comes out (see Rasa.ClientData.EnclosureIndex).
    ///
    /// The list is per map and per mesh family on purpose: applied to every mesh, the same test also
    /// fires under rock overhangs, trees and water surfaces, where the terrain is real ground.
    ///
    /// A prefix may name one placement, <c>mesh_prefix@x:z</c>: only entities of that mesh placed within
    /// <see cref="PlacementTolerance"/> of (x, z) count. Timora Mines needs this: the chunnel entrance
    /// the terrain crosses at the Fuel Egress tunnel also stands, with its awning over open ground, in
    /// front of the lower mines, where cutting the terrain under the awning opens a hole in the apron.
    /// </summary>
    public sealed class TerrainCuts
    {
        public const float PlacementTolerance = 1f;

        private readonly Dictionary<string, List<Entry>> _entries = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);

        private sealed class Entry
        {
            public string Prefix;
            public Vector2? At;

            public bool Matches(string mesh, Vector3 position) =>
                mesh.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
                (At == null || Vector2.Distance(At.Value, new Vector2(position.X, position.Z)) <= PlacementTolerance);
        }

        public static TerrainCuts Load(string path)
        {
            var cuts = new TerrainCuts();

            if (!File.Exists(path))
                return cuts;

            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var fields = line.Split(',');

                if (fields.Length < 2 || string.IsNullOrWhiteSpace(fields[0]) || string.IsNullOrWhiteSpace(fields[1]))
                    continue;

                if (!cuts._entries.TryGetValue(fields[0].Trim(), out var list))
                    cuts._entries[fields[0].Trim()] = list = new List<Entry>();

                list.Add(Parse(fields[1].Trim()));
            }

            return cuts;
        }

        private static Entry Parse(string field)
        {
            var at = field.IndexOf('@');

            if (at < 0)
                return new Entry { Prefix = field };

            var xz = field.Substring(at + 1).Split(':');

            if (xz.Length != 2)
                throw new FormatException($"terrain cut '{field}': expected mesh_prefix@x:z");

            return new Entry
            {
                Prefix = field.Substring(0, at),
                At = new Vector2(float.Parse(xz[0], CultureInfo.InvariantCulture), float.Parse(xz[1], CultureInfo.InvariantCulture))
            };
        }

        public IEnumerable<string> PrefixesFor(string map) =>
            _entries.TryGetValue(map, out var list) ? list.Select(e => e.Prefix) : Enumerable.Empty<string>();

        /// <summary>The placed-mesh test for a map (mesh file name, entity position), or null when nothing on it is cut.</summary>
        public Func<string, Vector3, bool> For(string map)
        {
            if (!_entries.TryGetValue(map, out var list) || list.Count == 0)
                return null;

            return (mesh, position) => list.Any(entry => entry.Matches(mesh, position));
        }
    }
}
