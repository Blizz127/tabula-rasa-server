using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Managers;
using Rasa.Migrations.WildernessData;
using Rasa.Navigation;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// InstanceTravelLinks (2026-09-27): the doors of Warnet Caverns, Ustor Yard, Sanctus Grotto and The Refuge, the Last
    /// Stand's exits and the CELLAR's north end to Edmund Range. These tests check the rows against the client values and
    /// the evidence record they were built from, the preloader's conventions they follow, the ground they put a player
    /// on, and that MapLinkManager fires them. Fidelity itself rests on the cited client data (docs/evidence).
    /// </summary>
    [TestClass]
    public class InstanceTravelLinksTests
    {
        /// <summary>The client's own link markers (uimapmarker.maplinkmarkers), full precision: context, x, y, z.</summary>
        private static readonly Dictionary<uint, (uint Map, double X, double Y, double Z)> ClientTriggers = new Dictionary<uint, (uint, double, double, double)>
        {
            [199250] = (1244, 769.5072021484375, 154.9575958251953, 797.6297607421875),       // [1402] 133182640964138 -> template 1541
            [199252] = (1497, 361.1624450683594, 433.7641296386719, -556.0028686523438),      // [1654] 134269267734139 -> template 1659
            [199254] = (2047, -570.3524780273438, 629.9303588867188, 718.397705078125),       // [2047] 134419591472545 -> template 1823
            [199256] = (2047, 251.489501953125, 146.2857208251953, 224.7547607421875),        // [2047] 134419591466163 -> template 2156
            [199258] = (2375, -285.78839111328125, 208.9081268310547, -706.0601806640625),    // [2378] 134419591466020 -> template 2232
            [199259] = (2375, -1.9696722030639648, 193.91427612304688, 486.6047668457031),    // [2378] 134419591466021 -> template 2232
            [199260] = (20000009, 9.268339157104492, 40.500431060791016, 136.0600128173828),  // [2232] 134419591463502 -> template 2365 (D15 map)
        };

        /// <summary>gamecontextuimapinfo [size, cx, cz] centres of the destinations, for the preloader's yaw rule.</summary>
        private static readonly Dictionary<uint, (double X, double Z)> MapCentres = new Dictionary<uint, (double, double)>
        {
            [1244] = (-2, -84), [1384] = (148, -149), [1497] = (0, -15), [1502] = (113, 12), [2047] = (0, 0), [1823] = (158, 97),
            [2156] = (-156, 11), [20000009] = (0, -60), [2374] = (-74, 23), [1347] = (130, -35)
        };

        private static IEnumerable<InstanceTravelLinksRows.Row> Rows => InstanceTravelLinksRows.Rows;

        [TestMethod]
        public void IdsStayInTheBatchBlockAndAreUnique()
        {
            Assert.AreEqual(12, Rows.Count());
            Assert.AreEqual(Rows.Count(), Rows.Select(row => row.Id).Distinct().Count());
            Assert.IsTrue(Rows.All(row => row.Id >= 199250 && row.Id <= 199299));
            Assert.IsTrue(Rows.All(row => row.Comment.Length <= 64));
            // Nothing leads to a retired or unshipped map (2361 Edmund Range OLD, the removed test maps).
            Assert.IsFalse(Rows.Any(row => new uint[] { 2361, 1991, 2233, 1737 }.Contains(row.DestMapContextId) || new uint[] { 2361, 1991, 2233, 1737 }.Contains(row.MapContextId)));
        }

        [TestMethod]
        public void EveryTriggerTheClientDrawsIsTheClientsMarker()
        {
            foreach (var (id, marker) in ClientTriggers)
            {
                var row = Rows.Single(r => r.Id == id);
                Assert.AreEqual(marker.Map, row.MapContextId, id.ToString());
                Assert.AreEqual(Math.Round(marker.X, 4), row.X, 1e-9, $"{id} x");
                Assert.AreEqual(Math.Round(marker.Y, 4), row.Y, 1e-9, $"{id} y");
                Assert.AreEqual(Math.Round(marker.Z, 4), row.Z, 1e-9, $"{id} z");
            }

            // The CELLAR's north end goes to the final Edmund Range, not the D15 map the client still names.
            Assert.AreEqual(2374u, Rows.Single(r => r.Id == 199260).DestMapContextId);
        }

        [TestMethod]
        public void EachDoorArrivesWhereItsWayBackStandsAndReturnsOntoItsEntrance()
        {
            foreach (var (entry, exit) in new[] { (199250u, 199251u), (199252u, 199253u), (199254u, 199255u), (199256u, 199257u), (199260u, 199261u) })
            {
                var inbound = Rows.Single(r => r.Id == entry);
                var outbound = Rows.Single(r => r.Id == exit);
                Assert.AreEqual(inbound.DestMapContextId, outbound.MapContextId);
                Assert.AreEqual(inbound.MapContextId, outbound.DestMapContextId);
                Assert.AreEqual((inbound.DestX, inbound.DestY, inbound.DestZ), (outbound.X, outbound.Y, outbound.Z), $"{exit} stands on {entry}'s arrival");
                Assert.AreEqual((inbound.X, inbound.Y, inbound.Z), (outbound.DestX, outbound.DestY, outbound.DestZ), $"{exit} returns onto {entry}'s trigger");
                Assert.AreEqual(inbound.Radius, outbound.Radius);
                Assert.AreEqual(inbound.Kind, outbound.Kind);
            }
        }

        [TestMethod]
        public void KindsRadiiAndYawFollowTheMapLinkPreloaderConventions()
        {
            var instances = new uint[] { 1384, 1502, 1823, 2156 };
            foreach (var row in Rows)
            {
                var instanceDoor = instances.Contains(row.MapContextId) || instances.Contains(row.DestMapContextId);
                Assert.AreEqual(instanceDoor ? InstanceTravelLinksRows.Instance : InstanceTravelLinksRows.Border, row.Kind, row.Id.ToString());
                var cellarPad = row.MapContextId == 20000009 || row.DestMapContextId == 2374 || row.MapContextId == 2374;
                Assert.AreEqual(cellarPad ? 4.0 : 6.0, row.Radius, row.Id.ToString());

                var (cx, cz) = MapCentres[row.DestMapContextId];
                Assert.AreEqual(Math.Round(Math.Atan2(row.DestX - cx, row.DestZ - cz), 4), row.DestRotation, 1e-3, $"{row.Id} yaw");
            }

            // The rule reproduces the preloader's own rows (5: divide -> minos caverns, arrival yaw -0.7466).
            Assert.AreEqual(-0.7466, Math.Round(Math.Atan2(-15.822 - 130, 122.6093 - -35), 4), 1e-4);
        }

        [TestMethod]
        public void RowsMatchTheEvidenceRecordAndTheManifest()
        {
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("instance-travel-20260927.json")));
            var links = evidence.RootElement.GetProperty("links").EnumerateArray().ToList();
            Assert.AreEqual(Rows.Count(), links.Count);
            foreach (var link in links)
            {
                var row = Rows.Single(r => r.Id == link.GetProperty("id").GetUInt32());
                Assert.AreEqual(row.MapContextId, link.GetProperty("map").GetUInt32());
                Assert.AreEqual(row.DestMapContextId, link.GetProperty("dest_map").GetUInt32());
                CollectionAssert.AreEqual(new[] { row.X, row.Y, row.Z }, link.GetProperty("trigger").EnumerateArray().Select(v => v.GetDouble()).ToArray());
                CollectionAssert.AreEqual(new[] { row.DestX, row.DestY, row.DestZ }, link.GetProperty("arrival").EnumerateArray().Select(v => v.GetDouble()).ToArray());
                Assert.AreEqual(row.DestRotation, link.GetProperty("dest_rotation").GetDouble());
            }

            using var manifest = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var records = manifest.RootElement.GetProperty("rows").EnumerateArray()
                .Where(r => r.GetProperty("table").GetString() == "map_link").ToList();
            Assert.AreEqual(Rows.Count(), records.Count);
            foreach (var record in records)
            {
                var row = Rows.Single(r => r.Id == record.GetProperty("key").GetProperty("id").GetUInt32());
                var fields = record.GetProperty("fields");
                double Value(string name) => fields.GetProperty(name).GetProperty("value").GetDouble();
                Assert.AreEqual(InstanceTravelLinksRows.Migration, record.GetProperty("migration").GetString());
                Assert.AreEqual(row.MapContextId, (uint)Value("map_context_id"));
                Assert.AreEqual((row.X, row.Y, row.Z), (Value("pos_x"), Value("pos_y"), Value("pos_z")));
                Assert.AreEqual(row.Radius, Value("radius"));
                Assert.AreEqual(row.DestMapContextId, (uint)Value("dest_map_context_id"));
                Assert.AreEqual((row.DestX, row.DestY, row.DestZ), (Value("dest_pos_x"), Value("dest_pos_y"), Value("dest_pos_z")));
                Assert.AreEqual(row.DestRotation, Value("dest_rotation"));
                Assert.AreEqual(row.Kind, (byte)Value("kind"));
                Assert.AreEqual(1d, Value("enabled"));
                Assert.AreEqual(row.Comment, record.GetProperty("storage_fields").GetProperty("comment").GetProperty("value").GetString());
                // The only analogue is Edmund Range's way back (OD-131).
                var analogue = fields.EnumerateObject().Any(f => f.Value.GetProperty("tier").GetString() == "analogue");
                Assert.AreEqual(row.Id == 199261, analogue, row.Id.ToString());
            }
        }

        [TestMethod]
        public void MapLinkManagerFiresOnTheTriggerAndNotBeyondIt()
        {
            foreach (var row in Rows)
            {
                var link = MapLink.FromEntry(new MapLinkEntry
                {
                    Id = row.Id, MapContextId = row.MapContextId, PosX = row.X, PosY = row.Y, PosZ = row.Z, Radius = row.Radius,
                    DestMapContextId = row.DestMapContextId, DestPosX = row.DestX, DestPosY = row.DestY, DestPosZ = row.DestZ,
                    DestRotation = row.DestRotation, Kind = row.Kind, Enabled = 1, Comment = row.Comment
                });
                Assert.IsTrue(link.Enabled);
                Assert.IsTrue(MapLinkManager.Contains(link, link.Position + new Vector3(link.Radius - 0.5f, 1f, 0f)), row.Id.ToString());
                Assert.IsFalse(MapLinkManager.Contains(link, link.Position + new Vector3(0f, 0f, link.Radius + 0.5f)), row.Id.ToString());
                Assert.IsFalse(MapLinkManager.Contains(link, link.Position + new Vector3(0f, MapLinkManager.VerticalTolerance + 1f, 0f)), row.Id.ToString());
            }
        }

        /// <summary>
        /// MapLinkManager drops a link whose map is not loaded and refuses to fire one whose destination is not: every map
        /// these rows touch is in the world's map_info, and every arrival has walkable ground a player lands on (the rule
        /// for original positions: at most 5 m above the navmesh surface, never more than 1 m under it).
        /// </summary>
        [TestMethod]
        public void EveryLinkIsOnALoadedMapAndEveryArrivalHasGround()
        {
            var root = RepositoryRoot();
            using var connection = OpenWorld(root);
            var mapNames = new Dictionary<long, string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT map_context_id, map_name FROM map_info";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    mapNames[reader.GetInt64(0)] = reader.GetString(1);
            }

            var problems = new List<string>();
            var meshes = new Dictionary<string, NavMeshQuery>();
            foreach (var row in Rows)
            {
                if (!mapNames.ContainsKey(row.MapContextId))
                    problems.Add($"{row.Id}: map {row.MapContextId} is not loaded");
                if (!mapNames.TryGetValue(row.DestMapContextId, out var mapName))
                {
                    problems.Add($"{row.Id}: destination {row.DestMapContextId} is not loaded");
                    continue;
                }

                if (!meshes.TryGetValue(mapName, out var query))
                {
                    var path = Path.Combine(root, "navmesh", mapName.ToLowerInvariant() + ".nav");
                    Assert.IsTrue(File.Exists(path), path);
                    meshes[mapName] = query = new NavMeshQuery(NavMeshFile.Read(path));
                }

                var arrival = new Vector3((float)row.DestX, (float)row.DestY, (float)row.DestZ);
                var ground = query.GroundHeight(arrival);
                if (ground == null)
                    problems.Add($"{row.Id}: no ground at the arrival on {mapName}");
                else if (arrival.Y - ground.Value < -1.0 || arrival.Y - ground.Value > 5.0)
                    problems.Add(string.Format(CultureInfo.InvariantCulture, "{0}: arrival y {1:0.##} against surface {2:0.##} on {3}", row.Id, arrival.Y, ground.Value, mapName));
            }

            Assert.AreEqual(0, problems.Count, string.Join("\n", problems));
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
