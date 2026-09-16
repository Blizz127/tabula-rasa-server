using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The first seeded escort: mission 1390 Conscientious Objector's "Take Milpas to Apirka" (objective 4) and
    /// "Escort Milpas to Divide entrance" (objective 8).
    ///
    /// Ranger Milpas is one of the two things the original's Ethical Parable turns on, and he exists in the client's
    /// own creature name table (id 9519 "Ranger Milpas") although nothing places him. He is created here as the OD-45
    /// pipeline does (class the world's AFS humans use, level of the zone's first missions) and given the **Escort**
    /// behavior with this mission, so he walks with the player whose mission it is. His start is the Alia Das arrival
    /// point, where the mission is picked up.
    ///
    /// The two destinations are original: Warrior Apirka's own position from the sources that record it, and the
    /// Wilderness -> Divide map link (id 11), whose position and radius are in the world's own map_link table.
    /// </summary>
    public static class WildernessEscortMilpasRows
    {
        public const string Migration = "WildernessEscortMilpas";

        private const uint Wilderness = 1220u;

        private const uint Milpas = 199803u;
        private const uint MilpasPlacement = 199803u;
        private const uint MilpasNameId = 9519u;      // client creature name: "Ranger Milpas"
        private const uint AfsHuman = 3846u;

        private const uint ConscientiousObjector = 1390u;
        private const uint TakeMilpasToApirka = 4u;
        private const uint EscortToDivide = 8u;

        private const uint ApirkaArea = 198605u;
        private const uint DivideEntranceArea = 198606u;

        // Warrior Apirka: the sources that record him (TaRapedia /loc). The Divide entrance: map_link 11,
        // (888.7216, 267.0309, 31.6255) radius 8, so the area is that circle with a little room to arrive in.
        private const double ApirkaX = 825.0, ApirkaY = 301.0, ApirkaZ = 499.5, ApirkaRadius = 12.0;
        private const double DivideX = 888.7216, DivideY = 267.0309, DivideZ = 31.6255, DivideRadius = 10.0;

        // The Alia Das arrival the boot camp exits to.
        // Beside the Alia Das arrival the boot camp exits to, on the same walkable ground
        // (content_location 19852, which the world position audit checks).
        private const double MilpasX = 886.11, MilpasY = 305.8, MilpasZ = 347.81;

        private const byte Creature = 1;
        private const byte EscortBehavior = 3;
        private const byte Sphere = 1;
        private const byte AreaEnteredBinding = 1;

        /// <summary>An objective that counts nothing rather than a counter.</summary>
        private const byte NoCounter = 255;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                    "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { Milpas, "Ranger Milpas (client name 9519)", AfsHuman, 1u, 10u, 750u, MilpasNameId, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[]
                {
                    "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                    "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id",
                    "comment", "escort_mission_id"
                },
                values: new object[,]
                {
                    { MilpasPlacement, Wilderness, Creature, Milpas, 0u, 0u, (byte)0,
                      MilpasX, MilpasY, MilpasZ, 0.0, EscortBehavior, (byte)0, (byte)0,
                      0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u,
                      "Ranger Milpas (OD-45 creation, OD-50 escort)", ConscientiousObjector }
                });

            migrationBuilder.InsertData(
                table: "content_area",
                columns: new[] { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" },
                values: new object[,]
                {
                    { ApirkaArea, Wilderness, Sphere, ApirkaX, ApirkaY, ApirkaZ, ApirkaRadius, 0.0, "Warrior Apirka (TaRapedia /loc)" },
                    { DivideEntranceArea, Wilderness, Sphere, DivideX, DivideY, DivideZ, DivideRadius, 0.0, "the Divide entrance (map_link 11)" }
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
                    { ConscientiousObjector, TakeMilpasToApirka, 0u, AreaEnteredBinding, ApirkaArea, 0u, 0u, 0u, false, false, 0u, 0u, 0u, NoCounter, "1390/4 arrive at Warrior Apirka with Milpas" },
                    { ConscientiousObjector, EscortToDivide, 0u, AreaEnteredBinding, DivideEntranceArea, 0u, 0u, 0u, false, false, 0u, 0u, 0u, NoCounter, "1390/8 arrive at the Divide entrance with Milpas" }
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,] { { ConscientiousObjector, TakeMilpasToApirka, 0u }, { ConscientiousObjector, EscortToDivide, 0u } });

            migrationBuilder.DeleteData(table: "content_area",
                keyColumns: new[] { "id" }, keyValues: new object[,] { { ApirkaArea }, { DivideEntranceArea } });

            migrationBuilder.DeleteData(table: "content_placement",
                keyColumns: new[] { "id" }, keyValues: new object[,] { { MilpasPlacement } });

            migrationBuilder.DeleteData(table: "creature",
                keyColumns: new[] { "id" }, keyValues: new object[,] { { Milpas } });
        }
    }
}
