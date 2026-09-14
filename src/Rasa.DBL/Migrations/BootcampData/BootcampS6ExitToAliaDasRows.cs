using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the S6 boot-camp data migration (<c>BootcampS6ExitToAliaDas</c>), inserted and deleted by
    /// reserved key only. Both provider migrations call this class, so <c>SeedMigrationParityTests</c> sees
    /// identical operations. Never edit after release; corrections go in a <c>BootcampFix_*</c> migration with a
    /// manifest <c>changes</c> entry.
    ///
    /// The exit to Alia Das (S6 of the boot-camp build plan). Values and their provenance tiers live in
    /// <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "BootcampS6ExitToAliaDas"); positions in <c>bootcamp-d11-positions.json</c>. Literal types must match the
    /// mapped CLR types.
    ///
    /// Van Valkenberg sends the recruit to the pad ("Go stand on the dropship pad to transport out to Alia Das",
    /// texts 21864/21861). Entering the pad area (the damaged-pad map entity, OD-8) once (1995,4) or (2005,4) is
    /// completed transfers the recruit to the first observed Alia Das position (location 19852) and sets the
    /// account's skip-bootcamp flag (OD-9), in that order.
    ///
    /// Owner decisions applied here (2026-09-14, manifest OD-28 and OD-32; decided by the agent on the owner's
    /// behalf and pending owner review): exit radius 12 m (inferred); the inferred indicator id 438 for (1995,4)
    /// at its measured position with the observed 3D effect.
    /// </summary>
    public static class BootcampS6ExitToAliaDasRows
    {
        public const string Migration = "BootcampS6ExitToAliaDas";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── content_area ──
            // {"id": 198603} 1995/2005 exit dropship pad: inferred: shape, radius, half_height; original: pos_x, pos_y, pos_z
            migrationBuilder.InsertData(
                table: "content_area",
                columns: new[] { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" },
                values: new object[] { 198603u, 1985u, (byte)1, -225.35, 99.6, -70.52, 12.0, 0.0, "1995/2005 exit dropship pad" });

            // ── content_location ──
            // {"id": 19852} boot-camp exit: Alia Das arrival: inferred: purpose; original: map_context_id; measured: pos_x ±1.5/0.5 m, pos_y ±1.5/0.5 m, pos_z ±1.5/0.5 m, rotation ±30 deg
            migrationBuilder.InsertData(
                table: "content_location",
                columns: new[] { "id", "purpose", "map_context_id", "pos_x", "pos_y", "pos_z", "rotation", "comment" },
                values: new object[] { 19852u, (byte)2, 1220u, 884.11, 305.8, 347.81, 5.5501, "boot-camp exit: Alia Das arrival" });

            // ── content_condition ──
            // {"condition_id": 198909, "or_group": 0, "term_index": 0} : inferred: kind, objective_id; original: state
            // {"condition_id": 198909, "or_group": 1, "term_index": 0} : inferred: kind, mission_id, objective_id; original: state
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { 198909u, (byte)0, (byte)0, (byte)2, 1995u, 4u, 2u, "", 0, false },
                    { 198909u, (byte)1, (byte)0, (byte)2, 2005u, 4u, 2u, "", 0, false }
                });

            // ── content_rule ──
            // {"id": 1985010} exit pad -> Alia Das: inferred: event, area_id, condition_id
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[] { 1985010u, 1985u, (byte)10, 0u, 0u, 198603u, 0u, 0u, 198909u, "exit pad -> Alia Das" });

            // ── content_rule_action ──
            // {"rule_id": 1985010, "sequence": 0} transfer to Alia Das: inferred: action, location_id
            // {"rule_id": 1985010, "sequence": 1} set account can_skip_bootcamp: inferred: action
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[,]
                {
                    { 1985010u, (byte)0, (byte)11, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 19852u, 0u, "transfer to Alia Das" },
                    { 1985010u, (byte)1, (byte)12, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "set account can_skip_bootcamp" }
                });

            // ── npc_mission_objective_indicator ──
            // {"mission_id": 1995, "objective_id": 4, "indicator_index": 0} : inferred: indicator_id; measured: pos_x ±2.0/0.5 m, pos_y ±2.0/0.5 m, pos_z ±2.0/0.5 m; observed: show_3d
            migrationBuilder.InsertData(
                table: "npc_mission_objective_indicator",
                columns: new[] { "mission_id", "objective_id", "indicator_index", "indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d" },
                values: new object[] { 1995u, 4u, (byte)0, 438u, -225.53, 100.8, -69.81, 0.0, true });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "npc_mission_objective_indicator",
                keyColumns: new[] { "mission_id", "objective_id", "indicator_index" },
                keyValues: new object[] { 1995u, 4u, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[,]
                {
                    { 1985010u, (byte)0 },
                    { 1985010u, (byte)1 }
                });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 1985010u });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,]
                {
                    { 198909u, (byte)0, (byte)0 },
                    { 198909u, (byte)1, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "content_location",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 19852u });

            migrationBuilder.DeleteData(
                table: "content_area",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 198603u });
        }
    }
}
