using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Three missions whose giver was not the character the briefing names, and the one NPC that was missing
    /// because of it.
    ///
    /// A live report on 2026-09-20 - "commander rogers is giving me quests to go to outpost commander rogers" -
    /// sent every mission whose giver and receiver are the same creature back through the client's own mission
    /// text. Twenty-one missions have giver == receiver, and eighteen of them are meant to: a standing order
    /// taken and returned at the same desk. Three were not.
    ///
    /// <b>1392 and 1393, Conscientious Objector Part Two.</b> Both were given and received by Outpost Commander
    /// Rogers (198514), so the mission told the player to carry a report to the NPC who had just handed it over.
    /// The item the chain turns on is the client's entity class 25696, <i>Apirka's Report</i>, described as
    /// "Apirka's Report detailing your involvement in the case of the deserter, Ranger Milpas" - so the report is
    /// Warrior Apirka's to give, and Rogers is the C.O. it is carried to. The giver becomes creature 43,
    /// <i>Warrior Apirka</i> (client name 2969), who already stands in the Wilderness; the receiver stays Rogers.
    ///
    /// <b>976, Restraining Order.</b> Ours was given and received by Colonel Li Hua (199802). The client's mission
    /// text (ids 5334-5336) reads "Colonel Bruce wants you to help the ground forces out by taking out 3 Bane
    /// Stalkers. Return to him at Fort Haroun when you're done." Colonel Bruce is client name 8657 and had no
    /// creature in this world at all, which is how the mission came to be hung on the nearest colonel. He is
    /// created here as creature 199021 and takes both ends of the mission.
    ///
    /// Bruce's coordinates are inferred, not sourced: neither the client nor Ellatha gives a position for him.
    /// The mission text places him at Fort Haroun, so he stands beside Dr. Torpor (spawn pool 510079), the
    /// mission NPC already there, on the navmesh floor. The class, hit points and appearance are the OD-45
    /// analogue set every created NPC here uses.
    /// </summary>
    public static class QuestGiverBriefingFixRows
    {
        public const string Migration = "QuestGiverBriefingFix";

        private const uint AfsHuman = 3846u;

        public const uint ConscientiousObjectorTwo = 1392u, ConscientiousObjectorTwoBranch = 1393u;
        public const uint RestrainingOrder = 976u;
        public const uint Rogers = 198514u, Apirka = 43u, LiHua = 199802u, Bruce = 199021u;
        public const uint BruceName = 8657u;

        /// <summary>
        /// Beside Dr. Torpor at Fort Haroun, Torden Mires (1759). BruceFloor is the navmesh surface there less
        /// the original spawn offset, so it is the Y itself and takes no further correction.
        /// </summary>
        public const double BruceX = 618.0, BruceFloor = 223.884, BruceZ = 333.0;

        private static readonly uint[][] Officer =
        {
            new[] { 2u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 14404004u },
            new[] { 16u, 4022u, 933202u }, new[] { 15u, 4023u, 13933202u }, new[] { 17u, 24019u, 4286886614u }
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            Giver(migrationBuilder, ConscientiousObjectorTwo, Apirka);
            Giver(migrationBuilder, ConscientiousObjectorTwoBranch, Apirka);

            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { Bruce, "Colonel Bruce (Fort Haroun)", AfsHuman, 1u, 20u, 555u, BruceName, 9u, 5u,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[] { Bruce, 1759u, (byte)1, Bruce, 0u, 0u, (byte)0, BruceX, BruceFloor, BruceZ, 0.0,
                    (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u,
                    "Colonel Bruce (mission 976 text, Fort Haroun, navmesh floor)" });

            foreach (var piece in Officer)
                migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                    values: new object[] { Bruce, piece[0], piece[1], piece[2] });

            migrationBuilder.Sql($"update npc_mission set giver_id = {Bruce}, reciver_id = {Bruce} where id = {RestrainingOrder};");

            // The world seed spells her "Aprika"; the client's own name table reads "Warrior Apirka".
            migrationBuilder.Sql($"update creature set comment = 'Warrior Apirka' where id = {Apirka};");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update creature set comment = 'Warrior Aprika' where id = {Apirka};");
            migrationBuilder.Sql($"update npc_mission set giver_id = {LiHua}, reciver_id = {LiHua} where id = {RestrainingOrder};");

            foreach (var piece in Officer)
                migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                    keyValues: new object[] { Bruce, piece[0] });

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { Bruce });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { Bruce });

            Giver(migrationBuilder, ConscientiousObjectorTwo, Rogers);
            Giver(migrationBuilder, ConscientiousObjectorTwoBranch, Rogers);
        }

        private static void Giver(MigrationBuilder migrationBuilder, uint mission, uint creature)
            => migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: mission, column: "giver_id", value: creature);
    }
}
