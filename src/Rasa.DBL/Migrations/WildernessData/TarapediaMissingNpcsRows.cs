using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The NPCs behind the last of TaRapedia's giver and reward-giver mismatches, and the dialogue no one carried
    /// (GAP-W3-TARAPEDIA-GIVERS, GAP-W3-UNBOUND-CONVERSATION-PACKAGE).
    ///
    /// Eighteen of them have a TaRapedia page with a /loc, and every one of those readings lands on the navmesh
    /// floor of the zone its page names - between 0.1 and 1.8 m of it - which is what identifies the map as much as
    /// the zone does: Retread Vincent's reading only matches the Marshes, and the Computer Access Terminal's x and z
    /// sit twelve metres from Surveyor Miras in the Kardash colony while its y matches no floor at all, so the
    /// colony's own floor is used.
    ///
    /// Six have no page and are placed where the mission's own words put them, on the client's own markers, and each
    /// says so in its comment: Field Lt. Perkins at the north-west fortification of the Pravus instance, Base Guard
    /// Kapler at the Hydro Plant control point, Lieutenant Seguine in Nidu Dav (the village that Receptive Liaison
    /// Brice's own /loc locates), Line Capt. Dobbs at the Thoria Das waypoint, Field Lt. McMurray at the Central
    /// Trench waypoint, and Rohish at Fort Condor. Those six are guesses at the owner's word ("do best guess if you
    /// cannot find", 2026-09-19) and no more than that.
    ///
    /// Two are machines rather than people: the Operations Mainframe at the Viro Relay Tower and the Computer Access
    /// Terminal at Research Outpost Alpha, which mission 1112 has been waiting for. Both use
    /// UsableNPCCormanComputerV01 (7123), a computer that carries the NPC augmentation and so can be spoken to, and
    /// the closest client names there are, because neither name is in the client's table.
    /// </summary>
    public static class TarapediaMissingNpcsRows
    {
        public const string Migration = "TarapediaMissingNpcs";

        private const uint Stationary = 1u;

        // id, comment, name id, class, level, faction, map, x, y (floor + spawn offset), z, package, provenance
        private static readonly object[][] Npcs =
        {
            new object[] { 199007u, "Cmd. Sgt. Price (Pravus Research Facility)", 205u, 3846u, 13u, 1u, 1430u, -268.0, 6.884, -127.0, 0u, "TaRapedia /loc, AFS Preparation Camp" },
            new object[] { 199008u, "Field Lt. Perkins (Pravus, north-west fortification)", 204u, 3846u, 5u, 1u, 1430u, -308.0, 16.264, -107.0, 105u, "GUESS: no /loc; the mission says the north-west fortification, and this is the raised walkable ground 40 m west and 20 m north of Price" },
            new object[] { 199109u, "Watchman Hillenmeyer (Lake Elinor)", 2937u, 3846u, 16u, 1u, 1244u, 323.0, 128.174, 604.0, 0u, "TaRapedia /loc" },
            new object[] { 199209u, "Transportation Officer Mamede (Fort Defiance)", 8554u, 3846u, 33u, 1u, 1497u, -266.0, 420.774, 857.0, 0u, "TaRapedia /loc" },
            new object[] { 199210u, "Outpost Cmdr. Russ (Wedge Rock Outpost)", 4677u, 3846u, 32u, 1u, 1497u, 449.0, 378.004, 367.0, 0u, "TaRapedia /loc" },
            new object[] { 199409u, "Lt. Cockerell (Mires)", 8674u, 3846u, 33u, 1u, 1759u, 615.0, 241.244, 550.0, 0u, "TaRapedia /loc" },
            new object[] { 199410u, "Captain Gaston (Baylor Base)", 8677u, 3846u, 30u, 1u, 1759u, -547.0, 262.284, -451.0, 0u, "TaRapedia /loc" },
            new object[] { 199411u, "Sgt. Van Winkle (Mires)", 8659u, 3846u, 28u, 1u, 1759u, 622.0, 240.814, 584.0, 0u, "TaRapedia /loc" },
            new object[] { 199412u, "Sgt. Ricardo (Fort Condor)", 8683u, 3846u, 29u, 1u, 1759u, 228.0, 229.344, 696.0, 0u, "TaRapedia /loc" },
            new object[] { 199413u, "Rohish (Fort Condor)", 8697u, 3846u, 30u, 1u, 1759u, 219.59, 230.684, 735.15, 1075u, "GUESS: no page; Find Me A Rock sends the player to Sirth at Fort Condor and 1075's speaker answers there, so the client's Fort Condor marker" },
            new object[] { 199508u, "Researcher Erodan (Irendas Penal Colony)", 9009u, 7253u, 25u, 1u, 1764u, 253.0, 433.114, -149.0, 0u, "TaRapedia /loc" },
            new object[] { 199509u, "Major Silvia (Irendas Penal Colony)", 8782u, 3846u, 20u, 1u, 1764u, 348.0, 429.924, -170.0, 0u, "TaRapedia /loc" },
            new object[] { 199510u, "Lieutenant Liu (Irendas Penal Colony)", 8856u, 3846u, 23u, 1u, 1764u, 700.0, 413.324, 230.0, 0u, "TaRapedia /loc" },
            new object[] { 199511u, "Operations Mainframe (Viro Relay Tower)", 8576u, 7123u, 35u, 1u, 1764u, 900.0, 427.924, -132.0, 0u, "TaRapedia /loc; the client has no name for it, so CID Mainframe Computer stands in" },
            new object[] { 199512u, "Computer Access Terminal (Research Outpost Alpha)", 10169u, 7123u, 35u, 1u, 1773u, 5.0, 272.354, -212.0, 1213u, "TaRapedia /loc x,z; its y of 233 matches no floor, so the colony floor under it" },
            new object[] { 199304u, "Shaman Masai (Plateau Point)", 8596u, 22636u, 36u, 1u, 1454u, -735.0, 217.674, 171.0, 0u, "TaRapedia /loc" },
            new object[] { 199305u, "Retread Vincent (Retread City)", 8552u, 3846u, 36u, 1u, 1454u, -268.0, 216.644, -812.0, 0u, "TaRapedia /loc; the reading matches the Marshes floor, which is where Retread City is" },
            new object[] { 199110u, "Captain Tayros (Fort Dew)", 10203u, 3846u, 22u, 1u, 1244u, -155.4, 172.244, -719.8, 0u, "TaRapedia /loc" },
            new object[] { 199111u, "Sgt. Longshanks (Fort Dew)", 10205u, 3846u, 22u, 1u, 1244u, -154.0, 172.374, -691.8, 0u, "TaRapedia /loc" },
            new object[] { 199009u, "Archaeologist Wynne Topper (Twin Pillars Outpost)", 10650u, 3846u, 50u, 1u, 1220u, -132.0, 231.964, -628.0, 0u, "TaRapedia /loc" },
            new object[] { 199010u, "Base Guard Kapler (Hydro Plant)", 162u, 3846u, 10u, 1u, 1148u, -243.5, 57.324, 43.5, 0u, "GUESS: no /loc; the mission puts the turn-in at the Hydro Plant, and this is the client's own control-point marker there" },
            new object[] { 199011u, "Lieutenant Seguine (Nidu Dav)", 7033u, 3846u, 5u, 1u, 1148u, -770.0, 179.374, 612.0, 802u, "GUESS: no page; the mission puts him in Nidu Dav, and Receptive Liaison Brice's own /loc places that village at -773.7, 179.3, 615.9" },
            new object[] { 199012u, "Line Capt. Dobbs (Thoria Das)", 2993u, 3846u, 10u, 1u, 1148u, 265.45, 167.724, 1110.51, 32u, "GUESS: no page; the mission delivers to him at Thoria Das, and this is the client's own waypoint marker there" },
            new object[] { 199013u, "Field Lt. McMurray (Foreas Base trench)", 193u, 3846u, 10u, 1u, 1148u, -121.87, 84.784, -355.63, 98u, "GUESS: no page; the mission delivers to him along the Foreas Base trench, and this is the client's Central Trench waypoint (a Western Trench marker is the other candidate)" },
        };

        // mission, column, creature
        private static readonly object[][] Missions =
        {
            new object[] { 321u, "giver_id", 199007u },
            new object[] { 321u, "reciver_id", 199008u },
            new object[] { 411u, "giver_id", 199109u },
            new object[] { 887u, "giver_id", 199209u },
            new object[] { 970u, "giver_id", 199210u },
            new object[] { 983u, "giver_id", 199409u },
            new object[] { 1041u, "giver_id", 199410u },
            new object[] { 1141u, "giver_id", 199411u },
            new object[] { 940u, "reciver_id", 199412u },
            new object[] { 1118u, "giver_id", 199508u },
            new object[] { 1310u, "giver_id", 199509u },
            new object[] { 1063u, "reciver_id", 199510u },
            new object[] { 640u, "reciver_id", 199511u },
            new object[] { 1112u, "reciver_id", 199512u },
            new object[] { 1868u, "giver_id", 199304u },
            new object[] { 1904u, "giver_id", 199304u },   // Shaman Masai gives both parts
            new object[] { 1673u, "giver_id", 199305u },
            new object[] { 1788u, "giver_id", 199110u },
            new object[] { 1789u, "giver_id", 199111u },
            new object[] { 2016u, "giver_id", 199009u },
            new object[] { 347u, "reciver_id", 199010u },
            new object[] { 836u, "reciver_id", 199011u },
            new object[] { 332u, "reciver_id", 199012u },
        };

        /// <summary>The officer set of ContentNpcAppearance, for the ones on the swapset class.</summary>
        private static readonly uint[][] Officer =
        {
            new[] { 3u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 14404004u },
            new[] { 15u, 4022u, 933202u }, new[] { 16u, 4023u, 13933202u }, new[] { 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];
                var package = (uint)npc[10];
                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                        "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[3], npc[5], npc[4], 555u, npc[2], 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                if (package != 0)
                    migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                        values: new object[] { id, package, $"{npc[1]} (client conversation package {package})" });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                        "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                        "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                        "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[6], (byte)1, id, package, 0u, (byte)0, npc[7], npc[8], npc[9], 0.0,
                        (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[11]})" });

                if ((uint)npc[3] != 3846u)
                    continue;

                foreach (var piece in Officer)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }

            foreach (var mission in Missions)
                migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: mission[0],
                    column: (string)mission[1], value: mission[2]);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];
                if ((uint)npc[3] == 3846u)
                    foreach (var piece in Officer)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" }, keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                if ((uint)npc[10] != 0)
                    migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
