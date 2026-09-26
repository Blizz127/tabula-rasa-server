using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    /// </summary>
    public sealed class TerrainCuts
    {
        private readonly Dictionary<string, List<string>> _prefixes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

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

                if (!cuts._prefixes.TryGetValue(fields[0].Trim(), out var list))
                    cuts._prefixes[fields[0].Trim()] = list = new List<string>();

                list.Add(fields[1].Trim());
            }

            return cuts;
        }

        public IEnumerable<string> PrefixesFor(string map) =>
            _prefixes.TryGetValue(map, out var list) ? list : Enumerable.Empty<string>();

        /// <summary>The mesh test for a map, or null when nothing on it is cut.</summary>
        public Func<string, bool> For(string map)
        {
            if (!_prefixes.TryGetValue(map, out var list) || list.Count == 0)
                return null;

            return mesh => list.Any(prefix => mesh.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
