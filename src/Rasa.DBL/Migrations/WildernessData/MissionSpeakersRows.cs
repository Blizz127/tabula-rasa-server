using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Three NPCs a mission tells the player to go and speak to, who were not in this world - so the
    /// conversation that completes each objective had been hung on the mission's own giver instead.
    ///
    /// The same defect as the live report of 2026-09-20, in three more places. A conversation objective is
    /// completed by talking to whoever carries its NPC package, and when the intended speaker has no creature
    /// row the package ends up on the giver, who then both sends the player out and receives them back:
    ///
    ///   * <b>332</b> (Divide): package 74 sat on Lt. Sebastian. The client sends the player to <b>Field Sgt.
    ///     Hayes</b> (name id 169), at the Crossroads Waypoint.
    ///   * <b>969</b> (Mires): package 1070 sat on Chakel. The speaker is <b>Ashwon</b> (name id 8692), at
    ///     Quicksilver Post.
    ///   * <b>977</b> (Mires): package 1082 sat on Corporal Cooper, who is the giver. The client sends the player
    ///     to <b>Sirth</b> (name id 8704) about the samples, at Outpost Condor.
    ///
    /// Positions are the client's own UI map markers for those places, and each lands on the navmesh floor within
    /// 0.4 m of the marker's height: Hayes 116.12 against a marker 116.02, Ashwon 235.40 against 235.81, Sirth
    /// 230.68 against 230.88. The Fort Condor marker itself is already occupied by Rohish, so Sirth takes the
    /// medical tent marker beside it.
    ///
    /// Their names are the client's. No source anywhere records their species, class or level: the class is the
    /// AFS-human analogue every created NPC in this world uses (OD-45), the body a shipped NPC's set (OD-11), and
    /// the level the level of the giver whose mission they answer. Those three are labelled analogues, not
    /// findings.
    /// </summary>
    public static class MissionSpeakersRows
    {
        public const string Migration = "MissionSpeakers";

        private const uint AfsHuman = 3846u;
        private const byte Stationary = 1;

        /// <summary>The officer set, as ContentNpcAppearance and the TaRapedia batches use it.</summary>
        private static readonly uint[][] Officer =
        {
            new[] { 2u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 1u },
            new[] { 16u, 4022u, 13933202u }, new[] { 15u, 4023u, 13933202u }, new[] { 17u, 20824u, 4286886614u }
        };

        // id, comment, name id, level, map, x, floor, z, package, the placement the package leaves, provenance
        private static readonly object[][] Speakers =
        {
            new object[] { 199082u, "Field Sgt. Hayes (Crossroads Waypoint)", 169u, 10u, 1148u, 393.92, 116.124, 285.59,
                74u, 199000u, "mission 332; client uimapmarker, Crossroads Waypoint; the package was on Lt. Sebastian" },
            new object[] { 199083u, "Ashwon (Quicksilver Post)", 8692u, 30u, 1759u, -571.12, 235.399, -223.98,
                1070u, 199401u, "mission 969; client uimapmarker, Quicksilver Post; the package was on Chakel" },
            new object[] { 199084u, "Sirth (Outpost Condor)", 8704u, 30u, 1759u, 199.12, 230.684, 714.67,
                1082u, 199402u, "mission 977; client uimapmarker, Outpost Condor medical tent; the package was on Corporal Cooper, the giver" }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Speakers)
            {
                var id = (uint)npc[0];
                var package = (uint)npc[8];
                var previous = (uint)npc[9];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                        "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], AfsHuman, 1u, npc[3], 555u, npc[2], 9u, 5u,
                        0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id",
                        "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                        "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                        "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[4], (byte)1, id, package, 0u, (byte)0, npc[5], npc[6], npc[7], 0.0,
                        Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[10]})" });

                foreach (var piece in Officer)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });

                // The package comes off whoever was standing in for them, on both tables: a placement's
                // npc_package_id wins over an npc_package row, so leaving either behind leaves the defect.
                migrationBuilder.Sql($"update content_placement set npc_package_id = 0 where id = {previous} and npc_package_id = {package};");
                migrationBuilder.Sql($"delete from npc_package where id = {previous} and package_id = {package};");
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Speakers)
            {
                var id = (uint)npc[0];
                var package = (uint)npc[8];
                var previous = (uint)npc[9];

                migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                    values: new object[] { previous, package, "restored by rollback" });
                migrationBuilder.Sql($"update content_placement set npc_package_id = {package} where id = {previous} and npc_package_id = 0;");

                foreach (var piece in Officer)
                    migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                        keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }
    }
}
