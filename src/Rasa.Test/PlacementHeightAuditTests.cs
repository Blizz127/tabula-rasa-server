using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Navigation;

namespace Rasa.Test
{
    /// <summary>
    /// Every content placement must stand on the walkable surface the map gives at its XZ.
    ///
    /// This is the check that would have caught Delessio: his placement was 6.44 m under the grating platform the
    /// player walks on, so he was materialized, unconditional and invisible, and only a live report found him. The
    /// ground comes from the navmesh built out of the client's own map data, so it is original-tier evidence of
    /// where a body can stand, and a placement that disagrees with it is our error, not the map's.
    ///
    /// Deliberate exceptions are listed with their reason rather than tolerated silently.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlacementHeightAuditTests
    {
        /// <summary>How far a placement may sit off the surface before it is a defect.</summary>
        private const double Tolerance = 1.5;

        /// <summary>The camp map, and the navmesh built from its client map data.</summary>
        private const uint CampContext = 1985u;
        private const string CampNavMesh = "adv_bootcamp.nav";

        /// <summary>
        /// Placements that are meant to sit off the ground, with the reason. Kept short on purpose: anything added
        /// here should be a deliberate piece of world data, never a measurement that was not made.
        /// </summary>
        private static readonly Dictionary<long, string> OffGround = new()
        {
            { 198677, "the bomb rests on the wreck hull (+1.06 m)" }
        };

        [TestMethod]
        public void EveryCampPlacementStandsOnTheWalkableSurface()
        {
            var root = RepositoryRoot();
            var navMesh = NavMeshFile.Read(Path.Combine(root, "navmesh", CampNavMesh));
            var query = new NavMeshQuery(navMesh);

            using var connection = new SqliteConnection($"Data Source={Path.Combine(root, "rasaworld.db")}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT id, pos_x, pos_y, pos_z, comment FROM content_placement WHERE map_context_id = @context ORDER BY id";
            command.Parameters.AddWithValue("@context", CampContext);
            using var reader = command.ExecuteReader();

            var offSurface = new List<string>();
            var checkedPlacements = 0;

            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var y = reader.GetDouble(2);
                var position = new Vector3((float)reader.GetDouble(1), (float)y, (float)reader.GetDouble(3));
                var ground = query.GroundHeight(position);

                checkedPlacements++;

                if (ground == null)
                {
                    if (!OffGround.ContainsKey(id))
                        offSurface.Add($"{id} has no walkable surface within reach (y {y:0.##}) {reader.GetString(4)}");
                    continue;
                }

                var delta = y - ground.Value;
                if (Math.Abs(delta) > Tolerance && !OffGround.ContainsKey(id))
                    offSurface.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0} stands {1:0.##} m off the surface (y {2:0.##}, ground {3:0.##}) {4}",
                        id, delta, y, ground.Value, reader.GetString(4)));
            }

            Assert.IsTrue(checkedPlacements > 25, $"the camp should have its placements, found {checkedPlacements}");
            Assert.AreEqual(0, offSurface.Count,
                "placements that do not stand where a body can walk:\n" + string.Join("\n", offSurface));
        }

        /// <summary>
        /// The first-login start (content_location 19851 / spawn.first_login) and McAllister's scripted-walk
        /// destination (19853) have to stand on the camp navmesh within the same 1.5 m height tolerance as
        /// placements, or be listed as labelled off-ground exceptions.
        /// </summary>
        [TestMethod]
        public void FirstLoginStartAndMcAllisterWalkStandOnTheCampNavmesh()
        {
            var root = RepositoryRoot();
            var navMesh = NavMeshFile.Read(Path.Combine(root, "navmesh", CampNavMesh));
            var query = new NavMeshQuery(navMesh);

            using var connection = new SqliteConnection($"Data Source={Path.Combine(root, "rasaworld.db")}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT id, pos_x, pos_y, pos_z, comment FROM content_location WHERE id IN (19851, 19853) ORDER BY id";
            using var reader = command.ExecuteReader();

            var offSurface = new List<string>();
            var checkedRows = 0;
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                var y = reader.GetDouble(2);
                var position = new Vector3((float)reader.GetDouble(1), (float)y, (float)reader.GetDouble(3));
                var ground = query.GroundHeight(position);
                checkedRows++;

                if (ground == null)
                {
                    offSurface.Add($"{id} has no walkable surface within reach (y {y:0.##}) {reader.GetString(4)}");
                    continue;
                }

                var delta = y - ground.Value;
                if (Math.Abs(delta) > Tolerance)
                    offSurface.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0} stands {1:0.##} m off the surface (y {2:0.##}, ground {3:0.##}) {4}",
                        id, delta, y, ground.Value, reader.GetString(4)));
            }

            Assert.AreEqual(2, checkedRows, "the first-login start and McAllister destination must both be present");
            Assert.AreEqual(0, offSurface.Count,
                "opening locations that do not stand where a body can walk:\n" + string.Join("\n", offSurface));
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
