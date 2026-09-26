using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// First observed Capture the Flag combat companion. Individual position, health and attack
    /// are estimates; docs/evidence/bootcamp-camp-gunner-companion.json records every field.
    /// </summary>
    public static class BootcampCampGunnerCompanionRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("creature",
                new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                new object[] { 198516u, "Forean Gunner Initiate L3 (camp companion)", 6239u, 1u, 3u, 555u, 7938u, 9u, 5u,
                    27u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "TEXT", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            // The closest surviving Forean Gunner equipment row is world creature 51's GooGun.
            migrationBuilder.InsertData("creature_appearance",
                new[] { "id", "slot_id", "Class_id", "color" },
                new object[] { 198516u, 13u, 6238u, 1u });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            // Visible from the DeSimone handoff through the cave and caldera fights.
            migrationBuilder.InsertData("content_condition",
                new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                new object[,]
                {
                    { 198914u, (byte)0, (byte)0, (byte)2, 1994u, 2u, 1u, "", 0, false },
                    { 198914u, (byte)1, (byte)0, (byte)2, 1994u, 1u, 1u, "", 0, false }
                });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("content_placement",
                new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "escort_mission_id", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                    "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                new object[] { 198688u, 1985u, (byte)1, 198516u, 0u, 0u, (byte)0,
                    385.2, 119.5, 152.3, 0.0, (byte)4, 1994u, 0u, 0u,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198914u, 0u, "Forean Gunner Initiate camp companion" });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "DOUBLE", "DOUBLE", "DOUBLE", "DOUBLE", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "TEXT" };
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("content_placement", "id", 198688u);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
            migrationBuilder.DeleteData("content_condition", new[] { "condition_id", "or_group", "term_index" },
                new object[,] { { 198914u, (byte)0, (byte)0 }, { 198914u, (byte)1, (byte)0 } });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER" };
            migrationBuilder.DeleteData("creature_appearance", new[] { "id", "slot_id" }, new object[] { 198516u, 13u });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes =
                new[] { "INTEGER", "INTEGER" };
            migrationBuilder.DeleteData("creature", "id", 198516u);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
        }
    }
}
