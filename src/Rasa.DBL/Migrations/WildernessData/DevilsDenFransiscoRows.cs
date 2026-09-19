using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: Captain Fransisco in the Devil's Den, and Field Lt. Brody given his own dialogue back
    /// (GAP-W3-MISASSIGNED-PACKAGE, the Brody half).
    ///
    /// Mission 670 "Missing Strike Team" completes objective 1 through package 550 and objective 2 through 145
    /// (the client's objectiveconversation table). 550's line is the found commander's: "Man, am I glad you showed
    /// up ... get back to Brody" - said to the player about Brody, so it is not his. Brody carried it anyway, which
    /// made objective 1 complete at the man who sent the player, and left 145 - "return to Field Lieutenant Brody",
    /// and 361/1 "Report to Field Lieutenant Brody" - carried by no one, so 670 could not be finished.
    ///
    /// The commander is Captain Fransisco, client creature name 6587. TaRapedia's page (revision 2007-11-25) puts him
    /// in the Devil's Den instance of the Palisades, level 17, AFS, and its Devil's Den page gives his coordinates as
    /// -59, 96, 50. The navmesh floor under that point is 95.82, 0.18 m from the reading, so Y is that floor less
    /// the world's spawn offset. His class and hit points are OD-45's AFS analogue (3846, 555 hp), as for Brody's
    /// other W3 neighbours. He is placed standing where he is found; TaRapedia says he then follows the player
    /// through the instance, which is not built (GAP-ESCORT).
    /// </summary>
    public static class DevilsDenFransiscoRows
    {
        public const string Migration = "DevilsDenFransisco";

        /// <summary>Captain Fransisco, in the Devil's Den (client name 6587).</summary>
        public const uint Fransisco = 199108u;

        /// <summary>Field Lt. Brody, from PalisadesConversationNpc.</summary>
        public const uint Brody = 199105u;

        /// <summary>670 objective 1 - the found commander's own dialogue.</summary>
        public const uint FransiscoPackage = 550u;

        /// <summary>670 objective 2 and 361 objective 1 - Brody's own dialogue.</summary>
        public const uint BrodyPackage = 145u;

        /// <summary>Palisades: Devil's Den.</summary>
        public const uint DevilsDen = 1394u;

        /// <summary>The navmesh floor under TaRapedia's /loc (95.82) plus WorldPlacementFloorSnapRows.OriginalSpawnOffset.</summary>
        public const double FransiscoY = 95.82 + WorldPlacementFloorSnapRows.OriginalSpawnOffset;

        private const uint Stationary = 1u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { Fransisco, "Captain Fransisco (Devil's Den)", 3846u, 1u, 17u, 555u, 6587u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[] { Fransisco, FransiscoPackage, "Captain Fransisco (client conversation package 550)" });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[] { Fransisco, DevilsDen, (byte)1, Fransisco, FransiscoPackage, 0u, (byte)0, -59.0, FransiscoY, 50.0, 0.0,
                    (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Captain Fransisco (TaRapedia /loc -59, 96, 50, navmesh floor)" });

            migrationBuilder.UpdateData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Brody },
                column: "package_id", value: BrodyPackage);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Brody },
                column: "package_id", value: FransiscoPackage);
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { Fransisco });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Fransisco });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { Fransisco });
        }
    }
}
