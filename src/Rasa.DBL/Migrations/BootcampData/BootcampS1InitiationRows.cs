using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the S1 boot-camp data migration (<c>BootcampS1Initiation</c>), inserted and
    /// deleted by reserved key only. Both provider migrations call this class, so
    /// <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release; corrections
    /// go in a <c>BootcampFix_*</c> migration with a manifest <c>changes</c> entry.
    ///
    /// Values and their provenance tiers live in
    /// <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "BootcampS1Initiation"). Literal types must match the mapped CLR types.
    /// </summary>
    public static class BootcampS1InitiationRows
    {
        public const string Migration = "BootcampS1Initiation";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // New-character start in the rebuilt boot camp (position_key spawn.first_login).
            migrationBuilder.InsertData(
                table: "content_location",
                columns: new[] { "id", "purpose", "map_context_id", "pos_x", "pos_y", "pos_z", "rotation", "comment" },
                values: new object[] { 19851u, (byte)1, 1985u, 387.2, 136.75, -79.09, 6.02139, "bootcamp first login" });

            // Major McAllister: the 1990 receiver and 1992 giver.
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[]
                {
                    "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
                },
                values: new object[]
                {
                    198500u, "Major McAllister", 3846u, 1u, 10u, 1000u, 10566u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u
                });

            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[] { 1990u, 0u, 198500u, 1u, (byte)1, 10000032u, false, false, "Initiation" });

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1990u, 1u, 1u, true, true, "Approach the Eloh Hologram" },
                    { 1990u, 2u, 2u, true, false, "Approach the Eloh Hologram" }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[] { 1990u, 1u, 2u });

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { 1990u, (byte)1, 100, 0u, 0u },
                    { 1990u, (byte)3, 1250, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[]
                {
                    "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id",
                    "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id", "target_state",
                    "counter_id", "comment"
                },
                values: new object[,]
                {
                    { 1990u, 1u, (byte)0, (byte)1, 198600u, 0u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1990/1 causeway" },
                    { 1990u, 2u, (byte)0, (byte)1, 198601u, 0u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1990/2 terrace" }
                });

            migrationBuilder.InsertData(
                table: "content_area",
                columns: new[] { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" },
                values: new object[,]
                {
                    { 198600u, 1985u, (byte)1, 387.22, 118.75, -28.3, 4.0, 0.0, "1990 obj1 causeway" },
                    { 198601u, 1985u, (byte)1, 387.83, 112.75, 6.71, 4.0, 0.0, "1990 obj2 terrace" }
                });

            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[] { 198900u, (byte)0, (byte)0, (byte)3, 1990u, 0u, 0u, "", 0, false });

            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[,]
                {
                    { 1985000u, 1985u, (byte)1, 0u, 0u, 0u, 0u, 0u, 198900u, "1990 radio offer" },
                    { 1985001u, 1985u, (byte)2, 1990u, 0u, 0u, 0u, 0u, 0u, "1990 accepted tip" },
                    { 1985002u, 1985u, (byte)3, 1990u, 1u, 0u, 0u, 0u, 0u, "1990 obj1 hologram" },
                    { 1985003u, 1985u, (byte)3, 1990u, 2u, 0u, 0u, 0u, 0u, "1990 obj2 hologram" }
                });

            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[]
                {
                    "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id",
                    "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id",
                    "fact_key", "fact_value", "location_id", "audio_set_id", "comment"
                },
                values: new object[,]
                {
                    { 1985000u, (byte)0, (byte)1, 1990u, true, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "1990 radio offer" },
                    { 1985001u, (byte)0, (byte)4, 0u, false, 0u, 0u, 10000018u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "Radial Menu tip" },
                    { 1985002u, (byte)0, (byte)5, 0u, false, 0u, 0u, 0u, 23u, (byte)2, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "Lightning Logos" },
                    { 1985002u, (byte)1, (byte)3, 0u, false, 1634u, 10598u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "An Ancient Eloh" },
                    { 1985003u, (byte)0, (byte)3, 0u, false, 1635u, 10598u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "Eloh 1635" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[]
                {
                    "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms",
                    "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment"
                },
                values: new object[]
                {
                    198650u, 1985u, (byte)1, 198500u, 0u, 0u, (byte)0, 387.2, 125.57, 53.3, 0.0,
                    (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "McAllister"
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "content_rule_action", keyColumns: new[] { "rule_id", "sequence" }, keyValues: new object[,]
            {
                { 1985000u, (byte)0 }, { 1985001u, (byte)0 }, { 1985002u, (byte)0 }, { 1985002u, (byte)1 }, { 1985003u, (byte)0 }
            });
            migrationBuilder.DeleteData(table: "content_rule", keyColumn: "id", keyValues: new object[]
            {
                1985000u, 1985001u, 1985002u, 1985003u
            });
            migrationBuilder.DeleteData(table: "content_condition", keyColumns: new[] { "condition_id", "or_group", "term_index" }, keyValues: new object[,]
            {
                { 198900u, (byte)0, (byte)0 }
            });
            migrationBuilder.DeleteData(table: "content_area", keyColumn: "id", keyValues: new object[] { 198600u, 198601u });
            migrationBuilder.DeleteData(table: "npc_mission_objective_binding", keyColumns: new[] { "mission_id", "objective_id", "binding_id" }, keyValues: new object[,]
            {
                { 1990u, 1u, (byte)0 }, { 1990u, 2u, (byte)0 }
            });
            migrationBuilder.DeleteData(table: "npc_mission_reward", keyColumns: new[] { "id", "type" }, keyValues: new object[,]
            {
                { 1990u, (byte)1 }, { 1990u, (byte)3 }
            });
            migrationBuilder.DeleteData(table: "npc_mission_objective_transition", keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" }, keyValues: new object[,]
            {
                { 1990u, 1u, 2u }
            });
            migrationBuilder.DeleteData(table: "npc_mission_objective", keyColumns: new[] { "mission_id", "objective_id" }, keyValues: new object[,]
            {
                { 1990u, 1u }, { 1990u, 2u }
            });
            migrationBuilder.DeleteData(table: "npc_mission", keyColumn: "id", keyValues: new object[] { 1990u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumn: "id", keyValues: new object[] { 198650u });
            migrationBuilder.DeleteData(table: "creature", keyColumn: "id", keyValues: new object[] { 198500u });
            migrationBuilder.DeleteData(table: "content_location", keyColumn: "id", keyValues: new object[] { 19851u });
        }
    }
}
