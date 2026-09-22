using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Seven NPCs Ellatha's database records that this world did not have, at the coordinates it gives them.
    ///
    /// Of its 71 NPCs, 53 were already here and 51 of those stand within ten metres of its reading
    /// (EllathaNpcPositions). Of the eighteen that were not:
    ///
    /// * four were here under another name, which their position gave away - its "Dying Forean" is 1.5 m from the
    ///   world seed's Dying Forean Prisoner, its "Information Spec. Savious" is the creature EllathaNpcPositions had
    ///   just moved to that very spot;
    /// * four are the Imperial Valley and Landing Zone assault and defence commanders, which the client's name table
    ///   has no entry for - battleground fixtures rather than named NPCs;
    /// * Commander Elvers' page says "Spawns in 3 different spots" instead of a coordinate;
    /// * Captain Velns and Daniel Corman stand in Crater Lake Research, a map this world does not carry;
    /// * and the Caves of Donn's dying Forean reads -245, 24, -283, where that map's navmesh has no walkable
    ///   ground within reach, so it is left out rather than placed somewhere it cannot be reached.
    ///
    /// The seven below all land on the floor: between 0.1 and 1.6 m of it. Names are the client's own; the classes
    /// and hit points are OD-45 analogues, as every created NPC's are, and the levels follow the NPCs already
    /// standing in each place. They carry no dialogue: Ellatha names missions for some of them, but those missions
    /// are not seeded here, and a package no mission completes through would say nothing.
    /// </summary>
    public static class EllathaWorldNpcsRows
    {
        public const string Migration = "EllathaWorldNpcs";

        private const double Offset = WorldPlacementFloorSnapRows.OriginalSpawnOffset;
        private const uint Stationary = 1u;
        private const uint AfsHuman = 3846u, ForeanArcher = 7036u, ForeanUnarmed = 26833u;

        // id, comment, name id, class, level, map, x, ground, z, where
        private static readonly object[][] Npcs =
        {
            new object[] { 199014u, "Captain Burba (boot camp)", 10002u, AfsHuman, 10u, 1985u, -245.0, 101.61, -67.0, "Ellatha -245, 100, -67" },
            new object[] { 199015u, "Evac Pilot Constant (boot camp)", 5262u, AfsHuman, 10u, 1985u, 93.0, 109.32, 127.0, "Ellatha 93, 109, 127" },
            new object[] { 199016u, "Information Spec. Johnson (Pravus Research)", 206u, AfsHuman, 13u, 1430u, -83.0, 33.47, -132.0, "Ellatha -83, 33, -132" },
            new object[] { 199017u, "Ranger Nylla (Pravus Research)", 4843u, ForeanArcher, 13u, 1430u, -185.0, 5.14, -123.0, "Ellatha -185, 5, -123" },
            new object[] { 199018u, "Lieutenant Stone (Caves of Donn)", 6696u, AfsHuman, 10u, 1506u, -275.0, 23.28, -301.0, "Ellatha -275, 23, -301" },
            new object[] { 199019u, "Lost Forean (Caves of Donn)", 6740u, ForeanUnarmed, 10u, 1506u, -203.0, 8.17, -109.0, "Ellatha -203, 7, -109" },
            new object[] { 199020u, "Sgt. Pierre (Wilderness)", 3097u, AfsHuman, 10u, 1220u, -281.0, 170.61, 86.0, "Ellatha -281, 170, 86" }
        };

        /// <summary>The officer set of ContentNpcAppearance, for the ones on the swapset class.</summary>
        private static readonly uint[][] Officer =
        {
            new[] { 2u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 14404004u },
            new[] { 16u, 4022u, 933202u }, new[] { 15u, 4023u, 13933202u }, new[] { 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                        "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[3], 1u, npc[4], 555u, npc[2], 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                        "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                        "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                        "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[5], (byte)1, id, 0u, 0u, (byte)0, npc[6], (double)npc[7] + Offset, npc[8], 0.0,
                        (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[9]}, navmesh floor)" });

                if ((uint)npc[3] != AfsHuman)
                    continue;

                foreach (var piece in Officer)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                if ((uint)npc[3] == AfsHuman)
                    foreach (var piece in Officer)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" }, keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
