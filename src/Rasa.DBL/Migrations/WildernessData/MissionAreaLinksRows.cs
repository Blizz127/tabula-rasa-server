using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 16: every seeded mission handed out and turned in where the original put it.
    ///
    /// The mission-area audit of 2026-09-17 walked all 77 seeded missions from the boot camp to the Incline and found
    /// three kinds of break between a mission and its area:
    ///
    /// * <b>Givers that never spawn.</b> The world seed's spawnpools 64, 100 and 101 carry Council Elder Moawi (38),
    ///   Outpost Commander Rogers (100) and Field Sgt. Witherspoon (101) with min/max counts of 0/0, and
    ///   <c>SpawnPoolManager.CreateListOfCreatures</c> draws <c>Random.Next(0, 1)</c> = 0 of them, so 1407, 422, 429,
    ///   431, 1392 and 1393 had no NPC in the world. Rogers already exists as the reserved row 198514 at his measured
    ///   command-tent position (BootcampFixRogersTurnIn), with the same dialogue package 116 the client binds those
    ///   missions to, so the four missions move to him. Witherspoon gets a placement at TaRapedia's /loc for him (Lower
    ///   Eloh Creek, by the teleporter); Moawi gets one at the world seed's own spawnpool 64 position, which is where
    ///   TaRapedia's description puts him ("a hut along the back of Alia Das", beside Solis' pool 184). The seed rows
    ///   themselves are left untouched, as the Rogers correction left creature 100.
    ///
    /// * <b>Two NPCs on the wrong map.</b> Field Dr. Dawson (199002) and Receptive Liaison Brice (199003) were placed on
    ///   the Wilderness because TaRapedia's infobox says Zone=Wilderness, but the same pages' Location tables put them in
    ///   the Divide (Foreas Base hospital; Nidu Dav, by the teleporter pad) and their /loc readings are Divide
    ///   coordinates: on the Divide navmesh the ground under them is 117.00 and 179.42 (readings 116.9 and 179.3), on
    ///   the Wilderness there is no walkable surface within reach at either point - which is exactly what the world
    ///   position audit had been excusing since 2026-09-16. Brice also carried Noonan's dialogue package (2051, the one
    ///   1743's objective completes through); his own is 2050 (1742 "Report to Liaison Brice").
    ///
    /// * <b>Cross-zone hand-offs collapsed onto their receivers.</b> The batches that seeded 1743, 1746, 1747, 1748, 1068,
    ///   1541, 796 and 1826 had no giver NPC for them and set giver = receiver, so "Report to Liaison Ridout" was handed
    ///   out by Ridout and "South Of The Border" by the man the supplies go to. TaRapedia's mission pages name the
    ///   givers; two exist already (1747 is given by Sage, 1748 by Maddox) and six are created here from the same
    ///   evidence the earlier batches used: the client's creaturenamelanguage for the name id, TaRapedia's NPC page for
    ///   level and /loc, the OD-45 analogue for the appearance. Noonan has no /loc anywhere, so he stands at the client
    ///   map's Foreas Base region label with the navmesh's ground height, recorded as inferred (GAP-W3-NOONAN-POSITION).
    ///   Dawson keeps Agent Franz's package 732 because nothing names his own (GAP-W3-DAWSON-PACKAGE).
    ///
    /// Values and tiers are in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows and changes with
    /// migration "MissionAreaLinks"); the audit itself is <c>docs/mission-area-audit.md</c>.
    /// </summary>
    public static class MissionAreaLinksRows
    {
        public const string Migration = "MissionAreaLinks";

        // The reserved Rogers row (BootcampFixRogersTurnIn), placement 198684 in context 1220.
        public const uint Rogers = 198514u;

        // New NPCs, continuing each zone batch's reserved keys.
        public const uint Noonan = 199004u;
        public const uint Franz = 199005u;
        public const uint Bosley = 199206u;
        public const uint Repp = 199505u;
        public const uint Parsons = 199602u;
        public const uint Gerry = 199603u;

        // Placements for two world-seed creatures that no spawnpool ever spawns.
        public const uint WitherspoonPlacement = 198686u;
        public const uint MoawiPlacement = 198687u;

        public const uint Dawson = 199002u;
        public const uint Brice = 199003u;

        private const uint Divide = 1148u;
        private const uint Wilderness = 1220u;
        private const uint Plateau = 1497u;
        private const uint Incline = 1761u;
        private const uint Plains = 1764u;

        private const uint AnalogueClass = 3846u;
        private const uint AnalogueHitPoints = 555u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the NPCs the hand-off missions are given by ──
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { Noonan, "Receptive Liaison Noonan", AnalogueClass, 1u, 10u, AnalogueHitPoints, 10013u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Franz, "Agent Franz", AnalogueClass, 1u, 15u, AnalogueHitPoints, 6945u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Bosley, "Colonel Bosley", AnalogueClass, 1u, 34u, AnalogueHitPoints, 8618u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Repp, "Receptive Liaison Repp", AnalogueClass, 1u, 30u, AnalogueHitPoints, 9940u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Parsons, "Private Parsons", AnalogueClass, 1u, 25u, AnalogueHitPoints, 10270u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { Gerry, "Lt. Gerry", AnalogueClass, 1u, 25u, AnalogueHitPoints, 10271u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment", "escort_mission_id" },
                values: new object[,]
                {
                    { Noonan, Divide, (byte)1, Noonan, 2051u, 0u, (byte)0, -42.0, 116.5, 479.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Noonan (Foreas Base label, position inferred)", 0u },
                    { Franz, Divide, (byte)1, Franz, 732u, 0u, (byte)0, 107.7, 59.0, 442.1, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Agent Franz (TaRapedia /loc)", 0u },
                    { Bosley, Plateau, (byte)1, Bosley, 1001u, 0u, (byte)0, -621.0, 442.0, -212.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Colonel Bosley (TaRapedia /loc)", 0u },
                    { Repp, Plains, (byte)1, Repp, 2026u, 0u, (byte)0, 336.0, 430.0, -187.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Repp (TaRapedia /loc)", 0u },
                    { Parsons, Incline, (byte)1, Parsons, 2327u, 0u, (byte)0, 164.0, 232.0, -278.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Private Parsons (TaRapedia /loc)", 0u },
                    { Gerry, Incline, (byte)1, Gerry, 2328u, 0u, (byte)0, -336.0, 222.0, -400.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Lt. Gerry (TaRapedia /loc)", 0u },
                    // Two world-seed NPCs whose only spawnpool has counts 0/0 and so never spawns them.
                    { WitherspoonPlacement, Wilderness, (byte)1, 101u, 208u, 0u, (byte)0, 501.5, 238.3, 214.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Sgt. Witherspoon (TaRapedia /loc)", 0u },
                    { MoawiPlacement, Wilderness, (byte)1, 38u, 113u, 0u, (byte)0, 812.0, 302.09375, 505.1953, 6.23, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Council Elder Moawi (world-seed spawnpool 64 position)", 0u }
                });

            // ── the dialogue packages the client binds their missions to ──
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { Noonan, 2051u, "Receptive Liaison Noonan (client conversation package)" },
                    { Franz, 732u, "Agent Franz (client conversation package)" },
                    { Bosley, 1001u, "Colonel Bosley (client conversation package)" },
                    { Repp, 2026u, "Receptive Liaison Repp (client conversation package)" },
                    { Parsons, 2327u, "Private Parsons (client conversation package)" },
                    { Gerry, 2328u, "Lt. Gerry (client conversation package)" }
                });

            // ── Dawson and Brice stand in the Divide, where their /loc readings are; Brice speaks with his own package ──
            SetPlacementMap(migrationBuilder, Dawson, Divide);
            SetPlacementMap(migrationBuilder, Brice, Divide);
            SetBricePackage(migrationBuilder, 2050u);

            // ── each mission's giver and receiver, as the original had them ──
            foreach (var (missionId, giverId, receiverId) in Corrected)
            {
                SetGiver(migrationBuilder, missionId, giverId);
                SetReceiver(migrationBuilder, missionId, receiverId);
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (missionId, giverId, receiverId) in Previous)
            {
                SetGiver(migrationBuilder, missionId, giverId);
                SetReceiver(migrationBuilder, missionId, receiverId);
            }

            SetBricePackage(migrationBuilder, 2051u);
            SetPlacementMap(migrationBuilder, Brice, Wilderness);
            SetPlacementMap(migrationBuilder, Dawson, Wilderness);

            var created = new object[] { Noonan, Franz, Bosley, Repp, Parsons, Gerry };
            foreach (var id in created)
                migrationBuilder.DeleteData(table: "npc_package", keyColumn: "id", keyValue: id);
            foreach (var id in new object[] { Noonan, Franz, Bosley, Repp, Parsons, Gerry, WitherspoonPlacement, MoawiPlacement })
                migrationBuilder.DeleteData(table: "content_placement", keyColumn: "id", keyValue: id);
            foreach (var id in created)
                migrationBuilder.DeleteData(table: "creature", keyColumn: "id", keyValue: id);
        }

        /// <summary>(mission, giver, receiver) after this migration.</summary>
        public static readonly (uint MissionId, uint GiverId, uint ReceiverId)[] Corrected =
        {
            // Wilderness: the never-spawning Rogers row 100 gives way to the reserved Rogers 198514.
            (422u, Rogers, Rogers),
            (429u, Rogers, 101u),
            (1392u, Rogers, Rogers),
            (1393u, Rogers, Rogers),
            // Hand-offs: given by the liaison or officer TaRapedia names, turned in to the one already seeded.
            (1743u, Brice, Noonan),
            (1746u, Repp, 199504u),
            (1747u, 199504u, 199600u),
            (1748u, 199600u, 199405u),
            (1068u, Bosley, 199203u),
            (1541u, Bosley, 199204u),
            (796u, Dawson, Franz),
            (1826u, Parsons, 199601u)
        };

        /// <summary>(mission, giver, receiver) before this migration, for the rollback.</summary>
        public static readonly (uint MissionId, uint GiverId, uint ReceiverId)[] Previous =
        {
            (422u, 100u, 100u),
            (429u, 100u, 101u),
            (1392u, 100u, 100u),
            (1393u, 100u, 100u),
            (1743u, Brice, Brice),
            (1746u, 199504u, 199504u),
            (1747u, 199600u, 199600u),
            (1748u, 199405u, 199405u),
            (1068u, 199203u, 199203u),
            (1541u, 199204u, 199204u),
            (796u, Dawson, Dawson),
            (1826u, 199601u, 199601u)
        };

        private static void SetGiver(MigrationBuilder migrationBuilder, uint missionId, uint giverId)
            => migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: missionId, column: "giver_id", value: giverId);

        private static void SetReceiver(MigrationBuilder migrationBuilder, uint missionId, uint receiverId)
            => migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: missionId, column: "reciver_id", value: receiverId);

        private static void SetPlacementMap(MigrationBuilder migrationBuilder, uint placementId, uint mapContextId)
            => migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placementId, column: "map_context_id", value: mapContextId);

        /// <summary>The package lives on the npc_package row and, because CreatureManager prefers it, on the placement.</summary>
        private static void SetBricePackage(MigrationBuilder migrationBuilder, uint packageId)
        {
            migrationBuilder.UpdateData(table: "npc_package", keyColumn: "id", keyValue: Brice, column: "package_id", value: packageId);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: Brice, column: "npc_package_id", value: packageId);
        }
    }
}
