using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Rasa.NavMesh
{
    /// <summary>
    /// <c>data/map_build_settings.csv</c>: per map, a finer voxel grid than the default
    /// (<c>map,cell_size,cell_height,reason</c>). The geometry is the client's either way; only how finely Recast samples it
    /// changes. Minos Caverns needs this: its tunnel is laid from cavern tiles whose collision floors overlap at every
    /// joint, a metre or so apart, and the walkable strip left through several joints is two metres wide. At 0.4 m cells
    /// the agent's 0.6 m radius erodes that strip away (two whole cells a side), so the cave fell into islands the client
    /// walks straight through. Command-line --cell / --cell-height still apply to every other map.
    /// </summary>
    public sealed class MapBuildSettings
    {
        private readonly Dictionary<string, (float Cell, float CellHeight)> _maps =
            new Dictionary<string, (float Cell, float CellHeight)>(StringComparer.OrdinalIgnoreCase);

        public static MapBuildSettings Load(string path)
        {
            var settings = new MapBuildSettings();

            if (!File.Exists(path))
                return settings;

            foreach (var line in File.ReadLines(path).Skip(1))
            {
                var fields = line.Split(',');

                if (fields.Length < 3 || string.IsNullOrWhiteSpace(fields[0]))
                    continue;

                settings._maps[fields[0].Trim()] = (float.Parse(fields[1], CultureInfo.InvariantCulture),
                    float.Parse(fields[2], CultureInfo.InvariantCulture));
            }

            return settings;
        }

        /// <summary>The settings to build a map with: <paramref name="defaults"/>, or a copy with the map's own grid.</summary>
        public BuildSettings For(string map, BuildSettings defaults)
        {
            if (!_maps.TryGetValue(map, out var grid))
                return defaults;

            var settings = defaults.Clone();
            settings.CellSize = grid.Cell;
            settings.CellHeight = grid.CellHeight;
            return settings;
        }
    }
}
