using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Server rows for maps that were not in the final live game (research/20260926-instances, retired_contexts.json
    /// and the verdicts of instances.json).
    ///
    /// <b>Three test maps are no longer loaded.</b> map_info 1991 "test_lridout_outpostcombat", 2233
    /// "test_pvpcontrolpoint" and 1737 "test_nexus2" came with the world seed (MapInfoPreloader). The final client has no
    /// gamecontext row for 1991 at all (gamecontextlanguage names it "CP Wargame", verdict retired_unshipped); 2233 and
    /// 1737 are the client's own "Default for map [test_...]" rows, test maps with no location, no level band and no link
    /// marker anywhere. No map_link leads to any of them, so a channel was loaded for each that only a GM teleport or a
    /// stale character position could reach. Removing the rows makes them unreachable (the character keeps its saved
    /// position; there is no gameplay route there to preserve).
    ///
    /// <b>Edmund Range OLD's six spawn pools are removed.</b> 2361 "adv_wargame_provinggroundsv002" is the D15 Edmund Range
    /// map; D16.3 (live with D16.4, 2009-02-09) speaks of "the D15 Edmund Range map" and "the new version" (2374), and
    /// fixes characters "blackholed" logging into the new one - the old map was gone at shutdown. Add_service_npcs
    /// (ServiceNpcSpawnpoolPreloader) stood four hospital medics, a class trainer and a vendor on its markers
    /// (500320-500325); the creature and vendor rows stay, unused. Its map_info row stays so a character saved there can
    /// still load (GAP-EDMUND-OLD-RELOCATION: the D16.3 move to the new map is not implemented).
    ///
    /// Not changed: map_marker rows of the unshipped wargame copies 2265 and 2373. They are never served:
    /// MapMarkerManager sends a map's markers only to a player standing on that map, and neither context has a map_info
    /// row, so no channel exists for them.
    /// Both provider migrations call this class. Never edit after release.
    /// </summary>
    public static class InstanceTravelRetiredRows
    {
        public const string Migration = "InstanceTravelRetiredRows";

        public static readonly string[] MapInfoColumns = { "map_context_id", "map_name", "map_version", "base_region" };

        /// <summary>MapInfoPreloader's rows, exactly as the world seed inserted them.</summary>
        public static readonly object[][] MapInfo =
        {
            new object[] { 1737u, "test_nexus2", 147u, 0u },
            new object[] { 1991u, "test_lridout_outpostcombat", 114u, 0u },
            new object[] { 2233u, "test_pvpcontrolpoint", 140u, 0u },
        };

        public static readonly string[] SpawnPoolColumns = WorldDefectsFixRows.SpawnPoolColumns;

        /// <summary>
        /// ServiceNpcSpawnpoolPreloader's rows for 2361 as the deployed world holds them: 500320-500324 as inserted
        /// (single-precision positions), 500325 as WorldSweepCorrections left it.
        /// </summary>
        public static readonly object[][] EdmundOldPools =
        {
            new object[] { 500320u, (byte)0, (byte)0, 20u, (double)-402.0f, (double)420.5f, (double)174.0f, 0d, 2361u, 500320u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },   // Hospital: Blue Team
            new object[] { 500321u, (byte)0, (byte)0, 20u, (double)192.0f, (double)421.2f, (double)205.0f, 0d, 2361u, 500321u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },    // Hospital: Echo
            new object[] { 500322u, (byte)0, (byte)0, 20u, (double)282.0f, (double)420.5f, (double)162.0f, 0d, 2361u, 500322u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },    // Hospital: Red Team
            new object[] { 500323u, (byte)0, (byte)0, 20u, (double)-311.0f, (double)421.2f, (double)132.0f, 0d, 2361u, 500323u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },   // Hospital: Whiskey
            new object[] { 500324u, (byte)0, (byte)0, 20u, (double)17.2f, (double)363.5f, (double)-43.7f, 0d, 2361u, 500324u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },     // Class Trainer
            new object[] { 500325u, (byte)0, (byte)0, 20u, 70.151, 363.433, -51.648, 0d, 2361u, 500325u, (byte)1, (byte)1, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0, 0u, (byte)0, (byte)0 },                     // Vendors
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in EdmundOldPools)
                migrationBuilder.DeleteData(table: "spawnpool", keyColumn: "id", keyValue: row[0]);

            foreach (var row in MapInfo)
                migrationBuilder.DeleteData(table: "map_info", keyColumn: "map_context_id", keyValue: row[0]);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            for (var i = MapInfo.Length - 1; i >= 0; i--)
                migrationBuilder.InsertData(table: "map_info", columns: MapInfoColumns, values: MapInfo[i]);

            for (var i = EdmundOldPools.Length - 1; i >= 0; i--)
                migrationBuilder.InsertData(table: "spawnpool", columns: SpawnPoolColumns, values: EdmundOldPools[i]);
        }
    }
}
