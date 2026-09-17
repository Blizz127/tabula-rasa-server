using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the two NPCs two Wilderness missions could not be completed without, and the objective flags one of
    /// them was never offered over.
    ///
    /// The client's own <c>objectiveconversation</c> table says which conversation package completes each
    /// objective, and the server only offers a conversation from a creature carrying that package. Two missions
    /// named packages no creature in the world carried:
    ///
    /// * <b>422 "Miner Difficulties"</b> completes objective 1 through package 213, whose dialogue (1276) is
    ///   "You're a sight for sore eyes, soldier! We've been waiting on these supplies for too damn long!" - the
    ///   mission's own text names that NPC: "deliver two crates of mining equipment and medical supplies to
    ///   <b>Mining Coordinator Richards</b> at the entrance to the Pinhole Falls Caverns". TaRapedia's page agrees
    ///   from the other side: it is the mission's <i>RewardGiver</i>, at Pinhole Falls Cavern (and the giver of its
    ///   follow-up "Mama Miasma", also from there). His client creature name is 3077, "Mining Coord. Richards".
    ///
    /// * <b>429 "River Recon"</b> completes objective 5 through package 726, whose dialogue (3886) is the dying
    ///   Forean Ranger's "Bane ambush... they killed... all of us... Please... must tell human commander... they are
    ///   moving... towards Alia Das... Tell Witherspoon..." - the patrol member the mission sends the player to find
    ///   at the top of Pinhole Falls. Its other completion packages are Rogers' 116 and Witherspoon's 208, which the
    ///   world already had.
    ///
    /// Both positions are the client's own map markers: <c>uimapmarker</c> text id 40 "Pinhole Falls Caverns" at
    /// (341.16, 228.52, 477.85) and text id 39 "Pinhole Falls" at (309.51, 271.08, 436.97). The marker's own Y agrees
    /// with the navmesh floor under it (228.70 and 271.38; the falls has two floors, 226.32 at the river and 271.38
    /// at the top) and the mission says "near the top", so each Y here is the navmesh floor at the marker's X,Z.
    ///
    /// The rest is the OD-45 pipeline: the client's creature name table gives the name ids, and the appearance is an
    /// analogue of an existing world NPC of the same race - the package 213 greeting is the Forean one ("our kind
    /// were not born of this world..."), so Richards is a Forean civilian (NPC_Forean_Unarmed, as Council Elder Moawi
    /// is a Redshirt_Forean_Elder) and the ranger a Forean archer (NPC_Forean_Archer). Experience, faction and the
    /// health/speed figures are the world seed's own for a quest NPC (555 hp, run 9, walk 5).
    ///
    /// The batch also completes the offering of 429, which the loader refused because its objectives carried no
    /// ordinal, required or revealed flags (the client's objective skeleton exports none). Their order is the one the
    /// mission text states: "search for signs of a lost patrol of Forean Rangers near the top of Pinhole Falls, *then*
    /// report your findings to Field Sgt. Witherspoon" - so the recon (objective 5) is first and revealed on
    /// acceptance, the report (objective 4) second.
    /// </summary>
    public static class WildernessPinholeNpcRows
    {
        public const string Migration = "WildernessPinholeNpc";

        /// <summary>Mining Coord. Richards, at the entrance to the Pinhole Falls Caverns (client name 3077).</summary>
        public const uint Richards = 199910u;

        /// <summary>The wounded Forean Ranger of River Recon's lost patrol, at the top of Pinhole Falls.</summary>
        public const uint WoundedRanger = 199911u;

        /// <summary>The client's completion package for 422 objective 1 - Richards' own dialogue.</summary>
        public const uint RichardsPackage = 213u;

        /// <summary>The client's completion package for 429 objective 5 - the dying ranger's own dialogue.</summary>
        public const uint RangerPackage = 726u;

        public const uint MinerDifficulties = 422u;
        public const uint RiverRecon = 429u;

        /// <summary>422's receiver before this batch: the giver, from the WildernessGiverFix pass.</summary>
        public const uint Rogers = 198514u;

        /// <summary>The Wilderness.</summary>
        private const uint Wilderness = 1220u;

        private const uint Stationary = 1u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the two NPCs ──
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { Richards, "Mining Coord. Richards (Pinhole Falls Caverns)", 26833u, 1u, 25u, 555u, 3077u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { WoundedRanger, "Wounded Forean Ranger (lost patrol, top of Pinhole Falls)", 7036u, 1u, 3u, 555u, 3013u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            // Their conversation packages: 213 is what 422 objective 1 completes through, 726 what 429 objective 5 does.
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { Richards, RichardsPackage, "Mining Coord. Richards (client conversation package 213)" },
                    { WoundedRanger, RangerPackage, "Wounded Forean Ranger (client conversation package 726)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { Richards, Wilderness, (byte)1, Richards, RichardsPackage, 0u, (byte)0, 341.16, 228.70, 477.85, 0.0, (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Mining Coord. Richards (client uimapmarker 40 'Pinhole Falls Caverns', navmesh floor)" },
                    { WoundedRanger, Wilderness, (byte)1, WoundedRanger, RangerPackage, 0u, (byte)0, 309.51, 271.38, 436.97, 0.0, (byte)Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Wounded Forean Ranger (client uimapmarker 39 'Pinhole Falls' top floor, navmesh)" }
                });

            // ── 422 turns in at Richards, which is what TaRapedia records him as ──
            migrationBuilder.UpdateData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { MinerDifficulties },
                column: "reciver_id",
                value: Richards);

            // ── 429: the recon first, revealed on acceptance, then the report ──
            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 5u },
                column: "ordinal",
                value: 1u);

            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 5u },
                column: "is_required",
                value: true);

            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 5u },
                column: "revealed_on_accept",
                value: true);

            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 4u },
                column: "ordinal",
                value: 2u);

            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 4u },
                column: "is_required",
                value: true);

            migrationBuilder.UpdateData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { RiverRecon, 4u },
                column: "revealed_on_accept",
                value: false);

            // The recon reveals the report, as the mission text's "then report your findings to Field Sgt.
            // Witherspoon" says. Without it the loader refuses the mission outright - "required objective 4 is never
            // revealed" - which is what it reported on the first deploy of this batch.
            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[] { RiverRecon, 5u, 4u });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            // 429 goes back to carrying no flags at all, which is how the client skeleton left it.
            foreach (var objectiveId in new uint[] { 4u, 5u })
            {
                migrationBuilder.UpdateData(table: "npc_mission_objective", keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { RiverRecon, objectiveId }, column: "ordinal", value: null);
                migrationBuilder.UpdateData(table: "npc_mission_objective", keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { RiverRecon, objectiveId }, column: "is_required", value: null);
                migrationBuilder.UpdateData(table: "npc_mission_objective", keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { RiverRecon, objectiveId }, column: "revealed_on_accept", value: null);
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[] { RiverRecon, 5u, 4u });

            // 422 goes back to being turned in at its giver.
            migrationBuilder.UpdateData(table: "npc_mission", keyColumns: new[] { "id" },
                keyValues: new object[] { MinerDifficulties }, column: "reciver_id", value: Rogers);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { Richards });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { WoundedRanger });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Richards });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { WoundedRanger });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { Richards });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { WoundedRanger });
        }
    }
}
