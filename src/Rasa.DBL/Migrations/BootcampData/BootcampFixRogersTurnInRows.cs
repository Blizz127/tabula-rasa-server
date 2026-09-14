using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the boot-camp correction migration (<c>BootcampFixRogersTurnIn</c>), inserted and deleted by
    /// reserved key only, plus the recorded change of the 1995/2005 receiver. Both provider migrations call this class,
    /// so <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release; corrections go in a
    /// <c>BootcampFix_*</c> migration with a manifest <c>changes</c> entry.
    ///
    /// Closes GAP-ROGERS (manifest OD-35, decided by the agent on the owner's behalf on 2026-09-14, pending owner
    /// review). Van Valkenberg sends the recruit to "Commander Rogers" at Alia Das (texts 21864/21861), but the S5
    /// receiver, emulator creature 100, is a level-5 row whose only spawn (spawnpool 100, counts 0/0) stands where
    /// Langerman does; its values are pre-D11 and it lies outside the reserved keys, so it is left untouched. This seeds
    /// a reserved Rogers row (level 20 observed in B3-020; class 3846 and 1000 hp analogues under OD-11), places him at
    /// his measured command-tent position in the shared Wilderness context 1220 (always present, stationary, package
    /// 116), and changes <c>npc_mission.reciver_id</c> of 1995 and 2005 from 100 to him (manifest <c>changes</c>).
    /// Values and tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "BootcampFixRogersTurnIn"); the position in <c>bootcamp-d11-positions.json</c> (npc.outpost_commander_rogers).
    /// </summary>
    public static class BootcampFixRogersTurnInRows
    {
        public const string Migration = "BootcampFixRogersTurnIn";

        public const uint Rogers = 198514u;
        private const uint EmulatorRogers = 100u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── creature ──
            // {"id": 198514} Outpost Commander Rogers: analogue: class_id OD-11, max_hp OD-11; inferred: faction, run_speed, walk_speed, action1; observed: level; original: name_id
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { 198514u, "Outpost Commander Rogers", 3846u, 1u, 20u, 1000u, 2973u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            // ── content_placement ──
            // {"id": 198684} Outpost Commander Rogers (command tent): measured: pos_x ±1.5/0.3 m, pos_y ±1.5/0.3 m, pos_z ±1.5/0.3 m; inferred: rotation, behavior; original: npc_package_id
            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                values: new object[] { 198684u, 1220u, (byte)1, 198514u, 116u, 0u, (byte)0, 855.84, 294.14, 387.4, 4.3633, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Outpost Commander Rogers (command tent)" });

            // ── npc_mission (manifest changes: reciver_id 100 -> 198514) ──
            UpdateReceivers(migrationBuilder, Rogers);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            UpdateReceivers(migrationBuilder, EmulatorRogers);

            migrationBuilder.DeleteData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 198684u });

            migrationBuilder.DeleteData(
                table: "creature",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 198514u });
        }

        private static void UpdateReceivers(MigrationBuilder migrationBuilder, uint receiver)
        {
            foreach (var missionId in new[] { 1995u, 2005u })
                migrationBuilder.UpdateData(
                    table: "npc_mission",
                    keyColumn: "id",
                    keyValue: missionId,
                    column: "reciver_id",
                    value: receiver);
        }
    }
}
