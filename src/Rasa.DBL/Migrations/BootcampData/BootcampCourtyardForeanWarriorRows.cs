using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// A level-2 Forean Warrior is targeted among the courtyard defenders in final-week
    /// footage. The position, class choice and combat parameters are estimates recorded in
    /// docs/evidence/bootcamp-courtyard-forean-warrior.json.
    /// </summary>
    public static class BootcampCourtyardForeanWarriorRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("creature",
                new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                new object[] { 198519u, "Forean Warrior L2 (courtyard defender)", 6043u, 1u, 2u, 500u, 0u, 9.0, 5.0,
                    5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "TEXT", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "DOUBLE", "DOUBLE",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("creature_appearance",
                new[] { "id", "slot_id", "Class_id", "color" },
                new object[] { 198519u, 13u, 10532u, 1u });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("content_placement",
                new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "escort_mission_id", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms",
                    "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                new object[] { 198691u, 1985u, (byte)1, 198519u, 0u, 0u, (byte)0,
                    294.0, 120.5, 65.0, 0.0, (byte)2, 0u, 0u, 0u,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198914u, 0u, "Forean Warrior courtyard defender" });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "DOUBLE", "DOUBLE", "DOUBLE", "DOUBLE", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "TEXT" };
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("content_placement", "id", 198691u);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
            migrationBuilder.DeleteData("creature_appearance", new[] { "id", "slot_id" }, new object[] { 198519u, 13u });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes =
                new[] { "INTEGER", "INTEGER" };
            migrationBuilder.DeleteData("creature", "id", 198519u);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
        }
    }
}
