using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the S4 boot-camp data migration (<c>BootcampS4CaptureTheFlag</c>), inserted and
    /// deleted by reserved key only. Both provider migrations call this class, so
    /// <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release; corrections
    /// go in a <c>BootcampFix_*</c> migration with a manifest <c>changes</c> entry.
    ///
    /// Mission 1994 "Capture the Flag" (S4 of the boot-camp build plan). Values and their provenance
    /// tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "BootcampS4CaptureTheFlag"); positions in <c>bootcamp-d11-positions.json</c>. Literal types must
    /// match the mapped CLR types.
    ///
    /// Owner decisions applied here (2026-09-14, manifest OD-20..OD-24): Youngblood is present once the
    /// boss objective is completed or the mission is completed (he stays as the 2005 retry giver, OD-7);
    /// the ambient Thrax are always present (optional default) and level 2 only; indicators 437/439 at
    /// measured positions with neutral radius/show_3d; the Thrax placements guard their spot (behavior 2)
    /// and Youngblood stands still; <c>creature.action1</c> is a required column and the two hostile rows
    /// use emulator creature_action 33 as a labelled analogue (OD-11 for the Initiate, OD-17 for the boss);
    /// Tizzik Gi level 10 (inferred from the measured 50-credit kill rule) with 1000 hp (analogue, OD-17).
    /// </summary>
    public static class BootcampS4CaptureTheFlagRows
    {
        public const string Migration = "BootcampS4CaptureTheFlag";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // The MissionClientObjectiveSkeleton migration seeded (1994, 1..4) with NULL
            // server-authoritative flags; this migration owns the evidenced values, so the
            // skeleton rows are replaced (delete + insert, both data operations), as S2 did.
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1994u, 1u }, { 1994u, 2u }, { 1994u, 3u }, { 1994u, 4u }
                });

            // ── npc_mission 1994 ──────────────────────────────────────────────
            // giver 198504 DeSimone: inferred (A3-066 contiguous chat after the 1992 turn-in; text 21494).
            // receiver 198505 Youngblood: inferred (summary text 21236; B1-008 turn-in beside him).
            // level 1, solo, category 10000032, not shareable, not radio-completeable: inferred (S1/S2 precedent).
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[] { 1994u, 198504u, 198505u, 1u, (byte)1, 10000032u, false, false, "Capture the Flag" });

            // 5000 XP: observed (B1-008). Credits and the item reward are unobserved and omitted.
            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[] { 1994u, (byte)3, 5000, 0u, 0u });

            // 1992 Completed before 1994 is offered: inferred (A3-066 order).
            migrationBuilder.InsertData(
                table: "npc_mission_prerequisite",
                columns: new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                values: new object[] { 1994u, (byte)0, 1992u, (byte)4, "1994 requires 1992 completed" });

            // ── objectives: display order 4 → 2 → 1 → 3, only 4 revealed on accept ──
            // 2→1 observed (A4-20/A4-24); 4→2 and 1→3 inferred (inside the 362.133 and 551.467 cuts).
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1994u, 1u, 3u, true, false, "Eliminate the Tizzik G, the Bane Boss, in the AFS Base Camp" },
                    { 1994u, 2u, 2u, true, false, "Find a way out of the cave" },
                    { 1994u, 3u, 4u, true, false, "Report in to Captain Youngblood when he arrives in the camp." },
                    { 1994u, 4u, 1u, true, true, "Speak to Corporal Desimone to earn a promotion" }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[,]
                {
                    { 1994u, 4u, 2u },
                    { 1994u, 2u, 1u },
                    { 1994u, 1u, 3u }
                });

            // "Boss Eliminated: 0 / 1": observed (A4-21); counter slot 0 original (missionobjective @107252).
            migrationBuilder.InsertData(
                table: "npc_mission_objective_counter",
                columns: new[] { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" },
                values: new object[] { 1994u, 1u, (byte)0, 0, 1 });

            // ── bindings ──────────────────────────────────────────────────────
            // (1994,1) kill of the Tizzik Gi placement, counter 0: inferred (objective text 21313/21315).
            //     The placement names the creature, so creature_id stays 0 (the validator rejects both).
            // (1994,2) area_entered at the cave-in: inferred (completion inside the 430.533 cut).
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
                    { 1994u, 1u, (byte)0, (byte)6, 0u, 198659u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)0, "1994/1 kill Tizzik Gi" },
                    { 1994u, 2u, (byte)0, (byte)1, 198602u, 0u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1994/2 cave-in area" }
                });

            // Cave-in area: centre measured ±2.0 m / ±0.5 m (area.1994.2, the radar objective icon); sphere
            // radius 5 m inferred (below the ~19 m the 45 s fight never triggered, ≥ 2× the uncertainty).
            migrationBuilder.InsertData(
                table: "content_area",
                columns: new[] { "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment" },
                values: new object[] { 198602u, 1985u, (byte)1, 279.05, 120.5, 66.07, 5.0, 0.0, "1994 obj2 cave-in" });

            // Indicators (optional presentation, OD-21): 437 "Base Center" observed id (A5-26) at the
            // measured map readout (indicator.1994.1.437); 439 "Cave-in Location" inferred id at the measured
            // objective icon (indicator.1994.2.439). radius 0 / show_3d false are the neutral defaults,
            // omitted in the manifest (unobserved).
            migrationBuilder.InsertData(
                table: "npc_mission_objective_indicator",
                columns: new[] { "mission_id", "objective_id", "indicator_index", "indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d" },
                values: new object[,]
                {
                    { 1994u, 1u, (byte)0, 437u, 95.1, 109.25, 150.8, 0.0, false },
                    { 1994u, 2u, (byte)0, 439u, 279.05, 120.5, 66.07, 0.0, false }
                });

            // ── creatures: Youngblood, Tizzik Gi, Thrax Infantry Initiate ─────
            // name_ids 10575/6730/7674: original (creaturenamelanguage).
            // Youngblood: class 3846 analogue (OD-11), level 10 analogue (Delessio's observed rank level),
            //     1000 hp analogue, faction 1 inferred, stationary (speeds 0), no attack.
            // Tizzik Gi: class 10503 Bane_Thrax_Soldier_Pistol_Boss analogue (OD-17), level 10 inferred
            //     (50-credit kill line and the measured 5 × level credit rule; 143 XP conflict recorded),
            //     1000 hp and run 9 / walk 0 analogue (OD-17), faction 0 inferred.
            // Initiate: class 29769 Bane_Thrax_Soldier_Grunt_Pistol analogue (OD-11), level 2 observed,
            //     555 hp and run 9 / walk 5 analogue (OD-11), faction 0 inferred.
            // action1 33 (both hostile rows): analogue (OD-23) — emulator creature_action 33, whose attack
            //     pair (1, 1) equals the final client's Weapon_Creature_Bane_Pistol_Bootcamp (weaponclass
            //     29884); its range, cooldown and damage are emulator-authored.
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[]
                {
                    "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8"
                },
                values: new object[,]
                {
                    { 198505u, "Captain Youngblood", 3846u, 1u, 10u, 1000u, 10575u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198506u, "Tizzik Gi (boss)", 10503u, 0u, 10u, 1000u, 6730u, 9u, 0u, 33u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198507u, "Thrax Infantry Initiate L2", 29769u, 0u, 2u, 555u, 7674u, 9u, 5u, 33u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            // ── placements ────────────────────────────────────────────────────
            // Youngblood (93.2, 109.0, 137.5): measured ±2.0 / ±0.5 (npc.youngblood); stationary; package
            //     2561 original; present while (1994,1) Completed or 1994 Completed (condition 198902).
            // Tizzik Gi (95.1, 109.25, 150.8): inferred at indicator 437 (never on screen); guards its spot;
            //     present while (1994,1) Incomplete (condition 198901).
            // Initiates: measured standing positions while engaged (spawn.cave_assault.*, nameplate range
            //     readout + radar heading), guard their spot, always present, respawn only on rebuild.
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
                    { 198658u, 1985u, (byte)1, 198505u, 2561u, 0u, (byte)0, 93.2, 109.0, 137.5, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198902u, 0u, "Youngblood" },
                    { 198659u, 1985u, (byte)1, 198506u, 0u, 0u, (byte)0, 95.1, 109.25, 150.8, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198901u, 0u, "Tizzik Gi" },
                    { 198660u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 286.23, 120.5, 65.35, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.1" },
                    { 198661u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 289.35, 120.5, 61.02, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.2" },
                    { 198662u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 291.26, 120.5, 61.71, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.3" },
                    { 198663u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 287.8, 120.5, 67.11, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.4" },
                    { 198664u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 286.48, 120.5, 62.48, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.5" },
                    { 198665u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 291.77, 120.5, 63.77, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate courtyard.6" },
                    { 198666u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 247.71, 116.5, 76.06, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate passage.1" },
                    { 198667u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 250.01, 117.8, 74.07, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate passage.2" },
                    { 198668u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 257.68, 124.6, 68.99, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate passage.3" },
                    { 198669u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 202.24, 107.2, 88.44, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate caldera_trench.1" },
                    { 198670u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 209.39, 105.0, 94.09, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate caldera_trench.2" },
                    { 198671u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 150.05, 112.4, 159.06, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate base_perimeter.1" },
                    { 198672u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 116.26, 109.0, 125.54, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate base_interior.1" },
                    { 198673u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 128.49, 109.2, 132.34, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate base_interior.2" },
                    { 198674u, 1985u, (byte)1, 198507u, 0u, 0u, (byte)0, 59.2, 109.5, 139.33, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate base_gate.1" }
                });

            // ── presence conditions ───────────────────────────────────────────
            // 198901 boss: (1994,1) Incomplete. 198902 Youngblood: (1994,1) Completed OR 1994 Completed.
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { 198901u, (byte)0, (byte)0, (byte)2, 1994u, 1u, 1u, "", 0, false },
                    { 198902u, (byte)0, (byte)0, (byte)2, 1994u, 1u, 2u, "", 0, false },
                    { 198902u, (byte)1, (byte)0, (byte)1, 1994u, 0u, 4u, "", 0, false }
                });

            // ── rule: the promotion objective (1994,4) grants 500 XP ──────────
            // amount observed (A3-066 / A4-01); attachment to objective 4 inferred (text 21694 "first promotion").
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[] { 1985005u, 1985u, (byte)3, 1994u, 4u, 0u, 0u, 0u, 0u, "1994 obj4 promotion" });

            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[] { 1985005u, (byte)0, (byte)6, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 500u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "promotion 500 XP" });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[] { 1985005u, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 1985005u });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,]
                {
                    { 198901u, (byte)0, (byte)0 }, { 198902u, (byte)0, (byte)0 }, { 198902u, (byte)1, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198658u }, { 198659u }, { 198660u }, { 198661u }, { 198662u }, { 198663u }, { 198664u }, { 198665u },
                    { 198666u }, { 198667u }, { 198668u }, { 198669u }, { 198670u }, { 198671u }, { 198672u }, { 198673u }, { 198674u }
                });

            migrationBuilder.DeleteData(
                table: "creature",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198505u }, { 198506u }, { 198507u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_indicator",
                keyColumns: new[] { "mission_id", "objective_id", "indicator_index" },
                keyValues: new object[,]
                {
                    { 1994u, 1u, (byte)0 }, { 1994u, 2u, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "content_area",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 198602u });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,]
                {
                    { 1994u, 1u, (byte)0 }, { 1994u, 2u, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_counter",
                keyColumns: new[] { "mission_id", "objective_id", "counter_id" },
                keyValues: new object[] { 1994u, 1u, (byte)0 });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[,]
                {
                    { 1994u, 4u, 2u }, { 1994u, 2u, 1u }, { 1994u, 1u, 3u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1994u, 1u }, { 1994u, 2u }, { 1994u, 3u }, { 1994u, 4u }
                });

            // Restore the skeleton rows this migration replaced (NULL flags, original-tier
            // name comments), keeping the Down path consistent with the pre-S4 state.
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1994u, 1u, null, null, null, "Eliminate the Tizzik G, the Bane Boss, in the AFS Base Camp" },
                    { 1994u, 2u, null, null, null, "Find a way out of the cave" },
                    { 1994u, 3u, null, null, null, "Report in to Captain Youngblood when he arrives in the camp." },
                    { 1994u, 4u, null, null, null, "Speak to Corporal Desimone to earn a promotion" }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_prerequisite",
                keyColumns: new[] { "mission_id", "or_group", "required_mission_id" },
                keyValues: new object[] { 1994u, (byte)0, 1992u });

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type" },
                keyValues: new object[] { 1994u, (byte)3 });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { 1994u });
        }
    }
}
