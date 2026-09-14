using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the S2 boot-camp data migration (<c>BootcampS2GearingUp</c>), inserted and
    /// deleted by reserved key only. Both provider migrations call this class, so
    /// <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release; corrections
    /// go in a <c>BootcampFix_*</c> migration with a manifest <c>changes</c> entry.
    ///
    /// Mission 1992 "Gearing Up for Battle" (S2 of the boot-camp build plan). Values and their
    /// provenance tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c>
    /// (rows with migration "BootcampS2GearingUp"). Literal types must match the mapped CLR types.
    ///
    /// Owner decisions applied here (docs/progression-preservation-plan.md#boot-camp-owner-decisions):
    /// OD-11 (analogue NPC classes from final-live data), OD-12 (crate class 26714
    /// UsableTreasureDispHumCrateV04, the first TreasureDispenser candidate), OD-13 (Target Dummy as a
    /// second class-29365 placement), OD-19 (crate item templates are the level-1 generic Motor Assist
    /// variants as analogues — the client holds no binding for the observed maker variants and the
    /// tooltip-stat match does not resolve unique template ids; approved 2026-09-13).
    /// </summary>
    public static class BootcampS2GearingUpRows
    {
        public const string Migration = "BootcampS2GearingUp";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // The MissionClientObjectiveSkeleton migration seeded (1992, 1..9) with NULL
            // server-authoritative flags; this migration owns the evidenced values, so the
            // skeleton rows are replaced (delete + insert, both data operations).
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1992u, 1u }, { 1992u, 2u }, { 1992u, 3u }, { 1992u, 4u },
                    { 1992u, 5u }, { 1992u, 6u }, { 1992u, 7u }, { 1992u, 8u }, { 1992u, 9u }
                });

            // ── npc_mission 1992 ──────────────────────────────────────────────
            // giver 198500 McAllister: observed (A2-050, the 1990 turn-in opens his 1992 offer in the
            // same frame). receiver DeSimone: inferred (log text "report to Corporal DeSimone",
            // conversation 21494) — his creature row is seeded below; the id is a reserved key.
            // level 1: inferred (the mission is accepted at level 1 in footage).
            // group_type 1 (solo), category 10000032 "Instance (Bootcamp)": inferred (1990 precedent).
            // shareable/radio false: inferred (an instanced tutorial mission).
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[] { 1992u, 198500u, 198504u, 1u, (byte)1, 10000032u, false, false, "Gearing Up for Battle" });

            // Rewards: 200 credits observed (A2-050 offer window "reward 200"); 1250 XP observed
            // (A3-066 "You gained 1250 experience points." on completion).
            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { 1992u, (byte)1, 200, 0u, 0u },
                    { 1992u, (byte)3, 1250, 0u, 0u }
                });

            // Prerequisite: 1990 Completed before 1992 is offered (inferred: the offer only
            // appears after the 1990 turn-in, A2-050).
            migrationBuilder.InsertData(
                table: "npc_mission_prerequisite",
                columns: new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                values: new object[] { 1992u, (byte)0, 1990u, (byte)4, "1992 requires 1990 completed" });

            // ── objectives: ordinals/flags/transition chain ────────────────────
            // 4 revealed on accept: observed (A2-050 offer lists "Speak to Captain Delessio").
            // 4→1: inferred (inside the 304.733 cut; the crate objective follows the talk).
            // 1→2: observed (A3-027 tracker switches to equip after the loot completes).
            // 2→5: observed (A3-038 tracker switches to Delessio after equipping).
            // 5→6, 6→3, 3→9, 9→8, 8→7: inferred (conversation texts 21487/21491/21665/21494;
            // A3-060 "Speak to Corporal Hartmann" ordering).
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1992u, 1u, 2u, true, false, "Get your gear from the nearby crate" },
                    { 1992u, 2u, 3u, true, false, "Equip the gear by right-clicking it in your backpack [B]" },
                    { 1992u, 3u, 4u, true, false, "Shoot the Practice Dummy" },
                    { 1992u, 4u, 1u, true, true, "Speak to Captain Delessio" },
                    { 1992u, 5u, 5u, true, false, "Speak to Captain Delessio" },
                    { 1992u, 6u, 6u, true, false, "Speak to Corporal Hartmann by the Firing Range" },
                    { 1992u, 7u, 9u, true, false, "Speak to Corporal Hartmann" },
                    { 1992u, 8u, 8u, true, false, "Use your Lightning power on the Target Dummy" },
                    { 1992u, 9u, 7u, true, false, "Speak to Corporal Hartmann" }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[,]
                {
                    { 1992u, 4u, 1u },
                    { 1992u, 1u, 2u },
                    { 1992u, 2u, 5u },
                    { 1992u, 5u, 6u },
                    { 1992u, 6u, 3u },
                    { 1992u, 3u, 9u },
                    { 1992u, 9u, 8u },
                    { 1992u, 8u, 7u }
                });

            // ── bindings ──────────────────────────────────────────────────────
            // (1992,1) loot_all on the crate: inferred (items then completion 0.33 s later, A3-023/024).
            // (1992,2) equip any: inferred (first item completes, A3-036/037).
            // (1992,3) hit weapon destroying on the Practice Dummy: observed (vanish 347.133,
            //     completion 347.333).
            // (1992,8) hit action 194 (Recruit Lightning) on the Target Dummy: inferred (objective text).
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
                    { 1992u, 1u, (byte)0, (byte)3, 0u, 198651u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1992/1 crate loot all" },
                    { 1992u, 2u, (byte)0, (byte)4, 0u, 0u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1992/2 equip any" },
                    { 1992u, 3u, (byte)0, (byte)5, 0u, 198652u, 0u, 0u, true, (byte)0, 0u, 0u, 0u, (byte)255, "1992/3 destroying hit practice dummy" },
                    { 1992u, 8u, (byte)0, (byte)5, 0u, 198653u, 0u, 194u, true, (byte)0, 0u, 0u, 0u, (byte)255, "1992/8 lightning hit target dummy" }
                });

            // ── content_item_set: the crate contents ──────────────────────────
            // Names and quantity 1 each: observed (A3-017/022/023). Template ids: analogue per OD-19 —
            // the client holds no binding for the observed maker variants (Astra/Teleract/Hellstrom/
            // Shinobi are runtime-composed module names); these are the level-1 generic Motor Assist
            // variants and the level-1 rifle from the client-verified world data (the closest original
            // counterparts). 13066 boots / 13096 gloves / 13156 legs / 13186 vest (quality 2, Motor
            // Assist Armor 1 requirement, verified in itemtemplate_itemclass) / 13713 rifle
            // (106 physical, Firearms 1, clip 20 — matches the observed 20/994 ammo state and tooltip).
            migrationBuilder.InsertData(
                table: "content_item_set",
                columns: new[] { "item_set_id", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { 19858u, 13066u, 1u },
                    { 19858u, 13096u, 1u },
                    { 19858u, 13156u, 1u },
                    { 19858u, 13186u, 1u },
                    { 19858u, 13713u, 1u }
                });

            // ── creatures: Delessio, Hartmann, DeSimone ───────────────────────
            // name_ids 10576/10578/10579: original (creaturenamelanguage). Classes 3846
            // (NPC_Human_Swapset_Male): analogue per OD-11 (the final-live generic human NPC class the
            // shipped world data uses for AFS human officers). Delessio level 10: observed (A3-028).
            // Hartmann/DeSimone level 10: analogue of Delessio's observed rank level per OD-11.
            // faction 1, hp 1000: analogue (the S1 McAllister precedent).
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[]
                {
                    "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
                },
                values: new object[,]
                {
                    { 198501u, "Captain Delessio", 3846u, 1u, 10u, 1000u, 10576u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198502u, "Corporal Hartmann", 3846u, 1u, 10u, 1000u, 10578u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198504u, "Corporal DeSimone", 3846u, 1u, 10u, 1000u, 10579u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            // ── placements ────────────────────────────────────────────────────
            // McAllister 198650 already exists from S1. New:
            // Delessio (398.9, 173.3): measured ±2.5 (npc_captain_delessio); Y 114.0 from the platform
            //     static ArchHumBaseGatesystemPlatform16Mx24M (405, 114, 166) the crate stands on.
            //     Package 2560: original (objectiveconversation (1992,4/5,2560,1,1)).
            // Hartmann (385.7, 166.7): measured ±2.0 (npc_corporal_hartmann, identity inferred from the
            //     active objective); Y 119.4 from the firing-range sandbag statics. Package 2563: original.
            // DeSimone: not observed anywhere; position stays unseeded (gap) — the receiver turns the
            //     mission in, so a placement is required for the turn-in; reserved id 198654 is seeded at
            //     the base-camp area per the manifest's inferred note and must be reconciled with footage
            //     when it surfaces. Package 2562: original (objectiveconversation (1994,4,2562,1,1) is
            //     DeSimone's promotion conversation).
            // Supply Crate (397.3, 173.7): measured ±2.0 (obj_supply_crate); Y 114.0 platform top.
            //     Class 26714 per OD-12; usable_kind 1 container; loot_item_set 198700.
            //     usable_condition_id 198800 = objective (1992,1) Incomplete (inferred: client
            //     missionActivated; crate and beam persist, A3-023).
            // Practice Dummy (384.7, 186.8): measured ±1.5 (obj_practice_dummy); Y 119.4 ground.
            //     Class 29365: original (entityclass UsableStatelessHumPracticeDummyV01) + inferred
            //     identity (nameplate "Practice Dummy", A3-056). restore_ms 930: measured ±70 (four
            //     cycles A3). hit_points 100: analogue placeholder — the summed damage between restores
            //     (E6) is not yet computed from the footage; 100 is a storage placeholder pending E6.
            // Target Dummy: per OD-13 (second class-29365 placement); position inferred (firing range,
            //     near the practice dummy).
            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[]
                {
                    "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                    "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                    "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment"
                },
                values: new object[,]
                {
                    { 198651u, 1985u, (byte)2, 0u, 0u, 26714u, (byte)1, 397.3, 114.0, 173.7, 0.0, (byte)1, 200u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 19858u, 0u, 0u, 198800u, "1992 supply crate" },
                    { 198652u, 1985u, (byte)2, 0u, 0u, 29365u, (byte)2, 384.7, 119.4, 186.8, 0.0, (byte)1, 110u, 0u, 0u, 0u, 0u, 100u, 930u, 0u, 0u, 0u, 0u, 0u, "1992 practice dummy" },
                    { 198653u, 1985u, (byte)2, 0u, 0u, 29365u, (byte)2, 387.0, 119.4, 188.5, 0.0, (byte)1, 110u, 0u, 0u, 0u, 0u, 100u, 930u, 0u, 0u, 0u, 0u, 0u, "1992 target dummy" },
                    { 198655u, 1985u, (byte)1, 198501u, 2560u, 0u, 0u, 398.9, 114.0, 173.3, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Delessio" },
                    { 198656u, 1985u, (byte)1, 198502u, 2563u, 0u, 0u, 385.7, 119.4, 166.7, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Hartmann" },
                    { 198657u, 1985u, (byte)1, 198504u, 2562u, 0u, 0u, 387.2, 125.57, 40.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "DeSimone (position inferred, not observed)" }
                });

            // ── usable_condition for the crate: objective (1992,1) Incomplete ──
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[] { 198800u, (byte)0, (byte)0, (byte)2, 1992u, 1u, 1u, "", 0, false });

            // ── rule: the 1990 turn-in opens McAllister's 1992 offer ──────────
            // observed (A2-050: the offer window opens in the same frame as the turn-in).
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[] { 1985004u, 1985u, (byte)6, 1990u, 0u, 0u, 0u, 0u, 0u, "1990 turned in -> offer 1992 at McAllister" });

            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[] { 1985004u, (byte)0, (byte)2, 1992u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 198650u, 0u, "", 0, 0u, 0u, "offer 1992 at McAllister placement" });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[] { 1985004u, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 1985004u });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[] { 198800u, (byte)0, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198651u }, { 198652u }, { 198653u }, { 198655u }, { 198656u }, { 198657u }
                });

            migrationBuilder.DeleteData(
                table: "creature",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198501u }, { 198502u }, { 198504u }
                });

            migrationBuilder.DeleteData(
                table: "content_item_set",
                keyColumns: new[] { "item_set_id", "item_template_id" },
                keyValues: new object[,]
                {
                    { 19858u, 13066u }, { 19858u, 13096u }, { 19858u, 13156u }, { 19858u, 13186u }, { 19858u, 13713u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,]
                {
                    { 1992u, 1u, (byte)0 }, { 1992u, 2u, (byte)0 }, { 1992u, 3u, (byte)0 }, { 1992u, 8u, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[,]
                {
                    { 1992u, 4u, 1u }, { 1992u, 1u, 2u }, { 1992u, 2u, 5u }, { 1992u, 5u, 6u },
                    { 1992u, 6u, 3u }, { 1992u, 3u, 9u }, { 1992u, 9u, 8u }, { 1992u, 8u, 7u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1992u, 1u }, { 1992u, 2u }, { 1992u, 3u }, { 1992u, 4u },
                    { 1992u, 5u }, { 1992u, 6u }, { 1992u, 7u }, { 1992u, 8u }, { 1992u, 9u }
                });

            // Restore the skeleton rows this migration replaced (NULL flags, original-tier
            // name comments), keeping the Down path consistent with the pre-S2 state.
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1992u, 1u, null, null, null, "Get your gear from the nearby crate" },
                    { 1992u, 2u, null, null, null, "Equip the gear by right-clicking it in your backpack [B]" },
                    { 1992u, 3u, null, null, null, "Shoot the Practice Dummy" },
                    { 1992u, 4u, null, null, null, "Speak to Captain Delessio" },
                    { 1992u, 5u, null, null, null, "Speak to Captain Delessio" },
                    { 1992u, 6u, null, null, null, "Speak to Corporal Hartmann by the Firing Range" },
                    { 1992u, 7u, null, null, null, "Speak to Corporal Hartmann" },
                    { 1992u, 8u, null, null, null, "Use your Lightning power on the Target Dummy" },
                    { 1992u, 9u, null, null, null, "Speak to Corporal Hartmann" }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_prerequisite",
                keyColumns: new[] { "mission_id", "or_group", "required_mission_id" },
                keyValues: new object[] { 1992u, (byte)0, 1990u });

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type" },
                keyValues: new object[,] { { 1992u, (byte)1 }, { 1992u, (byte)3 } });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 1992u });
        }
    }
}
