using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Completes the three named level-3 Forean Initiates seen together at the camp pylon.
    /// Individual positions and combat numbers are estimates documented field by field in
    /// docs/evidence/bootcamp-camp-archer-shaman-companions.json.
    /// </summary>
    public static class BootcampCampArcherShamanCompanionsRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // The native client staff 10533 defines weapon attack (1,146). Range, cooldown
            // and damage borrow the closest same-rank Forean companion action, not retail data.
            migrationBuilder.InsertData("creature_action",
                new[] { "id", "description", "action_id", "action_arg_id", "range_min", "range_max", "cooldown", "windup", "min_damage", "max_damage" },
                new object[] { 46u, "Forean Shaman Initiate staff (estimated)", 1u, 146u, 1.0, 20.0, 2500u, 0u, 15u, 28u });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "TEXT", "INTEGER", "INTEGER", "DOUBLE", "DOUBLE", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("creature",
                new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                new object[,]
                {
                    { 198517u, "Forean Archer Initiate L3 (camp companion)", 7036u, 1u, 3u, 555u, 7986u, 9.0, 5.0,
                        28u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198518u, "Forean Shaman Initiate L3 (camp companion)", 7035u, 1u, 3u, 555u, 7890u, 9.0, 5.0,
                        46u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "TEXT", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "DOUBLE", "DOUBLE",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("creature_appearance",
                new[] { "id", "slot_id", "Class_id", "color" },
                new object[,]
                {
                    { 198517u, 13u, 10529u, 1u }, // original-client NPC Forean Bow; analogue equipment assignment
                    { 198518u, 13u, 10533u, 1u }  // original-client NPC Forean Staff; analogue equipment assignment
                });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER" };

            migrationBuilder.InsertData("content_placement",
                new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "escort_mission_id", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms",
                    "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                new object[,]
                {
                    { 198689u, 1985u, (byte)1, 198517u, 0u, 0u, (byte)0, 382.5, 119.40, 153.2, 0.0,
                        (byte)4, 1994u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198914u, 0u, "Forean Archer Initiate camp companion" },
                    { 198690u, 1985u, (byte)1, 198518u, 0u, 0u, (byte)0, 386.9, 119.48, 149.7, 0.0,
                        (byte)4, 1994u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198914u, 0u, "Forean Shaman Initiate camp companion" }
                });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "DOUBLE", "DOUBLE", "DOUBLE", "DOUBLE", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
                    "INTEGER", "INTEGER", "TEXT" };
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("content_placement", "id", new object[] { 198689u, 198690u });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
            migrationBuilder.DeleteData("creature_appearance", new[] { "id", "slot_id" },
                new object[,] { { 198517u, 13u }, { 198518u, 13u } });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes =
                new[] { "INTEGER", "INTEGER" };
            migrationBuilder.DeleteData("creature", "id", new object[] { 198517u, 198518u });
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
            migrationBuilder.DeleteData("creature_action", "id", 46u);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
        }
    }
}
