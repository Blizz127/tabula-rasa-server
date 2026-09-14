using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Rasa.Managers
{
    using Game;
    using Navigation;
    using Structures;

    /// <summary>
    /// Loads each map's navmesh (built offline by Rasa.NavMesh from the client's terrain and
    /// collision meshes) and answers the ground and path questions creature AI asks. A map with no
    /// <c>.nav</c> file keeps the old behaviour: straight lines at spawn height.
    ///
    /// The queries are only made from the world loop (BehaviorManager, spawning) and from GM
    /// commands on the packet thread; the two never run concurrently, since packets are processed
    /// on the loop too. A <see cref="NavMeshQuery"/> per map is therefore enough.
    /// </summary>
    public class NavMeshManager
    {
        private static NavMeshManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>The default folder, relative to the working directory, when the config names none.</summary>
        public const string DefaultDirectory = "navmesh";

        /// <summary>
        /// How far the navmesh ground may be from a position we snap to it. A creature standing on
        /// a bridge must not be pulled down to the road beneath, nor one under it lifted up.
        /// </summary>
        public const float SnapTolerance = 3.0f;

        public static NavMeshManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new NavMeshManager();
                    }
                }

                return _instance;
            }
        }

        private NavMeshManager()
        {
        }

        public string Directory { get; private set; }
        public int LoadedMaps { get; private set; }

        /// <summary>Loads a navmesh for every map channel that has one. Runs after MapChannelInit.</summary>
        public void NavMeshInit(string directory)
        {
            Directory = string.IsNullOrWhiteSpace(directory) ? DefaultDirectory : directory;
            LoadedMaps = 0;

            if (!System.IO.Directory.Exists(Directory))
            {
                Logger.WriteLog(LogType.Initialize, $"No navmesh folder at {Path.GetFullPath(Directory)}; creatures will move in straight lines");
                return;
            }

            var missing = new List<string>();

            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
            {
                var path = NavMeshFile.PathFor(Directory, mapChannel.MapInfo.MapName);

                if (!File.Exists(path))
                {
                    missing.Add(mapChannel.MapInfo.MapName);
                    continue;
                }

                try
                {
                    mapChannel.NavMesh = new NavMeshQuery(NavMeshFile.Read(path));
                    LoadedMaps++;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Could not load navmesh {path}: {e.Message}");
                    MapErrorManager.Instance.Record(mapChannel.MapInfo.MapContextId, $"navmesh {path} could not be loaded: {e.Message}");
                }
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded navmeshes for {LoadedMaps} of {MapChannelManager.Instance.MapChannelArray.Count} maps from {Path.GetFullPath(Directory)}");

            if (missing.Count > 0 && missing.Count <= 12)
                Logger.WriteLog(LogType.Initialize, $"  no navmesh for: {string.Join(", ", missing)}");
        }

        /// <summary>
        /// The position moved onto the walkable surface nearest to it, when the map has a navmesh
        /// and that surface is within <see cref="SnapTolerance"/> vertically; otherwise unchanged.
        /// </summary>
        public static Vector3 SnapToGround(MapChannel mapChannel, Vector3 position)
        {
            var navMesh = mapChannel?.NavMesh;

            if (navMesh == null)
                return position;

            var ground = navMesh.GroundHeight(position);

            if (ground == null || Math.Abs(ground.Value - position.Y) > SnapTolerance)
                return position;

            return new Vector3(position.X, ground.Value, position.Z);
        }

        /// <summary>
        /// A place to stand within <paramref name="radius"/> of <paramref name="centre"/>, on the
        /// navmesh when there is one and the centre is near it; null when there is no navmesh or
        /// the centre is off it, so the caller can fall back to picking a point itself.
        /// </summary>
        public static Vector3? RandomPointAround(MapChannel mapChannel, Vector3 centre, float radius)
        {
            return mapChannel?.NavMesh?.RandomPointAround(centre, radius);
        }

        /// <summary>
        /// The corners to walk through from <paramref name="start"/> to <paramref name="end"/>,
        /// ending as near to <paramref name="end"/> as the mesh allows. Null when the map has no
        /// navmesh or either point is off it: the caller walks straight.
        /// </summary>
        public static List<Vector3> FindPath(MapChannel mapChannel, Vector3 start, Vector3 end)
        {
            return mapChannel?.NavMesh?.FindPath(start, end, out _);
        }
    }
}
