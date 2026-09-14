using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen rows of the S5 boot-camp data migration (<c>BootcampS5Reinforcements</c>), inserted and
    /// deleted by reserved key only. Both provider migrations call this class, so
    /// <c>SeedMigrationParityTests</c> sees identical operations. Never edit after release; corrections
    /// go in a <c>BootcampFix_*</c> migration with a manifest <c>changes</c> entry.
    ///
    /// Missions 1995 "Calling for Reinforcements" and its retry 2005 (S5 of the boot-camp build plan). Values and
    /// their provenance tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with
    /// migration "BootcampS5Reinforcements"); positions in <c>bootcamp-d11-positions.json</c>. The per-row comments
    /// below list each row's non-trivial fields by tier (analogues with their owner decision); the manifest carries
    /// the citations, uncertainties, reasoning and the conflicts with the build plan. Literal types must match the
    /// mapped CLR types.
    ///
    /// 1995: Youngblood gives it after 1994; objectives 2 (wounded soldier, conversation 2584) → 3 (use Conrad's
    /// corpse) → 1 (bomb detonated, 600 s timer analogue OD-6, fails the mission) → 4 (Van Valkenberg, conversation
    /// 2564); receiver Rogers (creature 100). 2005: objectives 1 → 4 with the same bomb binding and a 600 s timer
    /// (text 21566); offered after a failed 1995 and again after its own failure. The bomb (7870) is planted at the
    /// wreck (24586, a Structure 31/91) and its detonation sets <c>bootcamp.dropship_destroyed</c>, opens the wreck
    /// and brings Van Valkenberg and the reinforcements; a failed bomb objective clears both facts (D13.4 quirk kept).
    ///
    /// Owner decisions applied here (2026-09-14, manifest OD-25..OD-31, OD-33, OD-34; decided by the agent on the
    /// owner's behalf and pending owner review): wreck class 24586 as usable kind 5 with the 91 alternate state and
    /// the set_placement_state detonation action; Conrad's corpse as the generic-use analogue 21961 with windup 0;
    /// the unnamed wounded soldier on class 3846 with package 2584; inferred indicator ids 432/435; reinforcements at
    /// inferred pad positions, present once the dropship is destroyed and stationary; fuse 4930 ms and windup
    /// 1420 ms (measured); 2005 level 3 without the hidden level-up experience; the level-1 outpost Thrax guarding
    /// its spot with creature_action 33; Rogers (creature 100) as receiver.
    /// </summary>
    public static class BootcampS5ReinforcementsRows
    {
        public const string Migration = "BootcampS5Reinforcements";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // The MissionClientObjectiveSkeleton migration seeded (1995, 1..4) and (2005, 1/4) with NULL
            // server-authoritative flags; this migration owns the evidenced values, so the skeleton rows are
            // replaced (delete + insert, both data operations), as S2 and S4 did.
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1995u, 1u }, { 1995u, 2u }, { 1995u, 3u }, { 1995u, 4u }, { 2005u, 1u }, { 2005u, 4u }
                });

            // ── npc_mission ──
            // {"id": 1995} Calling for Reinforcements: inferred: giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable
            // {"id": 2005} Calling for Reinforcements (retry): inferred: giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 1995u, 198505u, 100u, 2u, (byte)1, 10000032u, false, false, "Calling for Reinforcements" },
                    { 2005u, 198505u, 100u, 3u, (byte)1, 10000032u, false, false, "Calling for Reinforcements (retry)" }
                });

            // ── npc_mission_prerequisite ──
            // {"mission_id": 1995, "or_group": 0, "required_mission_id": 1994} 1995 requires 1994 completed: inferred: required_state
            // {"mission_id": 2005, "or_group": 0, "required_mission_id": 1995} 2005 group 0: 1995 state 2: inferred: required_state
            // {"mission_id": 2005, "or_group": 0, "required_mission_id": 2005} 2005 group 0: 2005 state 3: inferred: required_state
            // {"mission_id": 2005, "or_group": 1, "required_mission_id": 1995} 2005 group 1: 1995 state 2: inferred: required_state
            // {"mission_id": 2005, "or_group": 1, "required_mission_id": 2005} 2005 group 1: 2005 state 2: inferred: required_state
            migrationBuilder.InsertData(
                table: "npc_mission_prerequisite",
                columns: new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                values: new object[,]
                {
                    { 1995u, (byte)0, 1994u, (byte)4, "1995 requires 1994 completed" },
                    { 2005u, (byte)0, 1995u, (byte)2, "2005 group 0: 1995 state 2" },
                    { 2005u, (byte)0, 2005u, (byte)3, "2005 group 0: 2005 state 3" },
                    { 2005u, (byte)1, 1995u, (byte)2, "2005 group 1: 1995 state 2" },
                    { 2005u, (byte)1, 2005u, (byte)2, "2005 group 1: 2005 state 2" }
                });

            // ── npc_mission_objective ──
            // {"mission_id": 1995, "objective_id": 1} Destroy the crashed dropship before the bomb timer expires.: inferred: ordinal, is_required, revealed_on_accept
            // {"mission_id": 1995, "objective_id": 2} Locate the missing AFS soldiers: inferred: ordinal, is_required, revealed_on_accept
            // {"mission_id": 1995, "objective_id": 3} Remove the bomb from Conrad's corpse.: inferred: ordinal, is_required, revealed_on_accept
            // {"mission_id": 1995, "objective_id": 4} Check in with Corporal Van Valkenberg: inferred: ordinal, is_required, revealed_on_accept
            // {"mission_id": 2005, "objective_id": 1} Destroy the crashed dropship before the bomb timer expires.: inferred: ordinal, is_required, revealed_on_accept
            // {"mission_id": 2005, "objective_id": 4} Check in with Corporal Van Valkenberg: inferred: ordinal, is_required, revealed_on_accept
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1995u, 1u, 3u, true, false, "Destroy the crashed dropship before the bomb timer expires." },
                    { 1995u, 2u, 1u, true, true, "Locate the missing AFS soldiers" },
                    { 1995u, 3u, 2u, true, false, "Remove the bomb from Conrad's corpse." },
                    { 1995u, 4u, 4u, true, false, "Check in with Corporal Van Valkenberg" },
                    { 2005u, 1u, 1u, true, true, "Destroy the crashed dropship before the bomb timer expires." },
                    { 2005u, 4u, 2u, true, false, "Check in with Corporal Van Valkenberg" }
                });

            // ── npc_mission_objective_transition ──
            // {"mission_id": 1995, "completed_objective_id": 1, "revealed_objective_id": 4} : observed: revealed_objective_id
            // {"mission_id": 1995, "completed_objective_id": 2, "revealed_objective_id": 3} : inferred: revealed_objective_id
            // {"mission_id": 1995, "completed_objective_id": 3, "revealed_objective_id": 1} : inferred: revealed_objective_id
            // {"mission_id": 2005, "completed_objective_id": 1, "revealed_objective_id": 4} : inferred: revealed_objective_id
            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[,]
                {
                    { 1995u, 1u, 4u },
                    { 1995u, 2u, 3u },
                    { 1995u, 3u, 1u },
                    { 2005u, 1u, 4u }
                });

            // ── npc_mission_objective_binding ──
            // {"mission_id": 1995, "objective_id": 1, "binding_id": 0} 1995/1 bomb detonated: inferred: kind, placement_id; original: target_state
            // {"mission_id": 1995, "objective_id": 3, "binding_id": 0} 1995/3 use Conrad's corpse: inferred: kind, placement_id
            // {"mission_id": 2005, "objective_id": 1, "binding_id": 0} 2005/1 bomb detonated: inferred: kind, placement_id; original: target_state
            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[] { "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id", "target_state", "counter_id", "comment" },
                values: new object[,]
                {
                    { 1995u, 1u, (byte)0, (byte)7, 0u, 198677u, 0u, 0u, false, (byte)0, 0u, 0u, 115u, (byte)255, "1995/1 bomb detonated" },
                    { 1995u, 3u, (byte)0, (byte)2, 0u, 198676u, 0u, 0u, false, (byte)0, 0u, 0u, 0u, (byte)255, "1995/3 use Conrad's corpse" },
                    { 2005u, 1u, (byte)0, (byte)7, 0u, 198677u, 0u, 0u, false, (byte)0, 0u, 0u, 115u, (byte)255, "2005/1 bomb detonated" }
                });

            // ── npc_mission_objective_timer ──
            // {"mission_id": 1995, "objective_id": 1} : analogue: limit_seconds OD-6; inferred: on_expire
            // {"mission_id": 2005, "objective_id": 1} : inferred: limit_seconds, on_expire
            migrationBuilder.InsertData(
                table: "npc_mission_objective_timer",
                columns: new[] { "mission_id", "objective_id", "limit_seconds", "on_expire" },
                values: new object[,]
                {
                    { 1995u, 1u, 600u, (byte)2 },
                    { 2005u, 1u, 600u, (byte)2 }
                });

            // ── npc_mission_objective_indicator ──
            // {"mission_id": 1995, "objective_id": 1, "indicator_index": 0} : inferred: indicator_id; measured: pos_x ±2.5/0.5 m, pos_y ±2.5/0.5 m, pos_z ±2.5/0.5 m
            // {"mission_id": 1995, "objective_id": 2, "indicator_index": 0} : inferred: indicator_id; measured: pos_x ±1.5/1.5 m, pos_y ±1.5/1.5 m, pos_z ±1.5/1.5 m; observed: show_3d
            migrationBuilder.InsertData(
                table: "npc_mission_objective_indicator",
                columns: new[] { "mission_id", "objective_id", "indicator_index", "indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d" },
                values: new object[,]
                {
                    { 1995u, 1u, (byte)0, 432u, -223.26, 100.72, -65.7, 0.0, false },
                    { 1995u, 2u, (byte)0, 435u, -104.6, 86.1, 70.5, 0.0, true }
                });

            // ── creature ──
            // {"id": 198508} Corporal Van Valkenberg: analogue: class_id OD-11, level OD-11, max_hp OD-11; inferred: faction, run_speed, walk_speed, action1; original: name_id
            // {"id": 198509} wounded AFS soldier (package 2584): analogue: class_id OD-17, level OD-17, max_hp OD-17; inferred: faction, name_id, run_speed, walk_speed, action1
            // {"id": 198510} Infantryman L2 (reinforcement): analogue: class_id OD-11, max_hp OD-11; inferred: faction, run_speed, walk_speed, action1; observed: level; original: name_id
            // {"id": 198511} Infantryman L3 (reinforcement): analogue: class_id OD-11, max_hp OD-11; inferred: faction, run_speed, walk_speed, action1; observed: level; original: name_id
            // {"id": 198512} Forean Gunner Initiate L3 (reinforcement): analogue: class_id OD-11, max_hp OD-11; inferred: faction, run_speed, walk_speed, action1; observed: level; original: name_id
            // {"id": 198513} Thrax Infantry Initiate L1 (1995 outpost): analogue: class_id OD-11, max_hp OD-11, run_speed OD-11, walk_speed OD-11, action1 OD-11; inferred: faction; observed: level; original: name_id
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { 198508u, "Corporal Van Valkenberg", 3846u, 1u, 10u, 1000u, 10574u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198509u, "wounded AFS soldier (package 2584)", 3846u, 1u, 1u, 555u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198510u, "Infantryman L2 (reinforcement)", 21910u, 1u, 2u, 555u, 8716u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198511u, "Infantryman L3 (reinforcement)", 21900u, 1u, 3u, 555u, 8716u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198512u, "Forean Gunner Initiate L3 (reinforcement)", 6239u, 1u, 3u, 555u, 7938u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 198513u, "Thrax Infantry Initiate L1 (1995 outpost)", 29769u, 0u, 1u, 555u, 7674u, 9u, 5u, 33u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            // ── content_condition ──
            // {"condition_id": 198903, "or_group": 0, "term_index": 0} : inferred: kind; original: state
            // {"condition_id": 198903, "or_group": 0, "term_index": 1} : inferred: kind; original: state
            // {"condition_id": 198903, "or_group": 1, "term_index": 0} : inferred: kind; original: state
            // {"condition_id": 198904, "or_group": 0, "term_index": 0} : inferred: kind, fact_key
            // {"condition_id": 198905, "or_group": 0, "term_index": 0} : inferred: kind, fact_key
            // {"condition_id": 198906, "or_group": 0, "term_index": 0} : inferred: kind, fact_key, negate
            // {"condition_id": 198907, "or_group": 0, "term_index": 0} : inferred: kind, fact_key
            // {"condition_id": 198908, "or_group": 0, "term_index": 0} : inferred: kind; original: state
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { 198903u, (byte)0, (byte)0, (byte)2, 1995u, 3u, 2u, "", 0, false },
                    { 198903u, (byte)0, (byte)1, (byte)2, 1995u, 1u, 1u, "", 0, false },
                    { 198903u, (byte)1, (byte)0, (byte)2, 2005u, 1u, 1u, "", 0, false },
                    { 198904u, (byte)0, (byte)0, (byte)4, 0u, 0u, 0u, "bootcamp.bomb_planted", 1, false },
                    { 198905u, (byte)0, (byte)0, (byte)4, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 1, false },
                    { 198906u, (byte)0, (byte)0, (byte)4, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 1, true },
                    { 198907u, (byte)0, (byte)0, (byte)4, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 1, false },
                    { 198908u, (byte)0, (byte)0, (byte)2, 1995u, 3u, 1u, "", 0, false }
                });

            // ── content_placement ──
            // {"id": 198675} wounded soldier 2584 (position inferred): inferred: pos_x, pos_y, pos_z, rotation, behavior; original: npc_package_id
            // {"id": 198676} Conrad's corpse (analogue class, position inferred): analogue: entity_class_id OD-17; inferred: usable_kind, pos_x, pos_y, pos_z, rotation, behavior, hit_points, windup_ms, usable_condition_id; original: initial_state
            // {"id": 198677} bomb on the wreck hull: inferred: entity_class_id, usable_kind, pos_y, rotation, behavior, hit_points, alternate_state_condition_id, present_condition_id, usable_condition_id; measured: pos_x ±2.0/1.0 m, pos_z ±2.0/1.0 m, windup_ms ±150 ms, fuse_ms ±150 ms; original: initial_state, alternate_state
            // {"id": 198678} crashed dropship wreck (door 31/91): inferred: entity_class_id, usable_kind, pos_x, pos_y, pos_z, rotation, behavior, hit_points, alternate_state_condition_id; original: initial_state, alternate_state
            // {"id": 198679} Van Valkenberg: measured: pos_x ±2.5/0.5 m, pos_y ±2.5/0.5 m, pos_z ±2.5/0.5 m; inferred: rotation, behavior, present_condition_id; original: npc_package_id
            // {"id": 198680} Forean Gunner Initiate (position inferred): inferred: pos_x, pos_y, pos_z, rotation, behavior, present_condition_id
            // {"id": 198681} Infantryman L2 (position inferred): inferred: pos_x, pos_y, pos_z, rotation, behavior, present_condition_id
            // {"id": 198682} Infantryman L3 (position inferred): inferred: pos_x, pos_y, pos_z, rotation, behavior, present_condition_id
            // {"id": 198683} Thrax Initiate L1 outpost.1: measured: pos_x ±3.3/0.5 m, pos_y ±3.3/0.5 m, pos_z ±3.3/0.5 m; inferred: rotation, behavior
            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 198675u, 1985u, (byte)1, 198509u, 2584u, 0u, (byte)0, -102.4, 86.09, 70.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "wounded soldier 2584 (position inferred)" },
                    { 198676u, 1985u, (byte)2, 0u, 0u, 21961u, (byte)4, -102.4, 85.69, 66.8, 0.0, (byte)1, 44u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198908u, "Conrad's corpse (analogue class, position inferred)" },
                    { 198677u, 1985u, (byte)2, 0u, 0u, 7870u, (byte)3, -223.95, 102.3, -72.34, 0.0, (byte)1, 113u, 114u, 198904u, 1420u, 0u, 0u, 0u, 4930u, 0u, 0u, 198906u, 198903u, "bomb on the wreck hull" },
                    { 198678u, 1985u, (byte)2, 0u, 0u, 24586u, (byte)5, -225.35, 100.81, -70.52, 0.0, (byte)1, 31u, 91u, 198905u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "crashed dropship wreck (door 31/91)" },
                    { 198679u, 1985u, (byte)1, 198508u, 2564u, 0u, (byte)0, -219.78, 100.85, -68.2, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198907u, 0u, "Van Valkenberg" },
                    { 198680u, 1985u, (byte)1, 198512u, 0u, 0u, (byte)0, -225.43, 100.8, -69.77, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198907u, 0u, "Forean Gunner Initiate (position inferred)" },
                    { 198681u, 1985u, (byte)1, 198510u, 0u, 0u, (byte)0, -220.74, 100.8, -69.32, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198907u, 0u, "Infantryman L2 (position inferred)" },
                    { 198682u, 1985u, (byte)1, 198511u, 0u, 0u, (byte)0, -220.67, 100.8, -67.82, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 198907u, 0u, "Infantryman L3 (position inferred)" },
                    { 198683u, 1985u, (byte)1, 198513u, 0u, 0u, (byte)0, -85.0, 86.33, 91.03, 0.0, (byte)2, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Thrax Initiate L1 outpost.1" }
                });

            // ── content_rule ──
            // {"id": 1985006} bomb armed -> planted fact: inferred: event
            // {"id": 1985007} bomb detonated -> dropship destroyed: inferred: event
            // {"id": 1985008} 1995/1 failed -> ship back: inferred: event
            // {"id": 1985009} 2005/1 failed -> ship back: inferred: event
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[,]
                {
                    { 1985006u, 1985u, (byte)11, 0u, 0u, 0u, 198677u, 114u, 0u, "bomb armed -> planted fact" },
                    { 1985007u, 1985u, (byte)11, 0u, 0u, 0u, 198677u, 115u, 0u, "bomb detonated -> dropship destroyed" },
                    { 1985008u, 1985u, (byte)7, 1995u, 1u, 0u, 0u, 0u, 0u, "1995/1 failed -> ship back" },
                    { 1985009u, 1985u, (byte)7, 2005u, 1u, 0u, 0u, 0u, 0u, "2005/1 failed -> ship back" }
                });

            // ── content_rule_action ──
            // {"rule_id": 1985006, "sequence": 0} set bomb_planted: inferred: action
            // {"rule_id": 1985007, "sequence": 0} set dropship_destroyed: inferred: action
            // {"rule_id": 1985007, "sequence": 1} clear bomb_planted: inferred: action
            // {"rule_id": 1985007, "sequence": 2} wreck -> OPEN 91: inferred: action
            // {"rule_id": 1985008, "sequence": 0} clear dropship_destroyed: inferred: action
            // {"rule_id": 1985008, "sequence": 1} clear bomb_planted: inferred: action
            // {"rule_id": 1985009, "sequence": 0} clear dropship_destroyed: inferred: action
            // {"rule_id": 1985009, "sequence": 1} clear bomb_planted: inferred: action
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[,]
                {
                    { 1985006u, (byte)0, (byte)8, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.bomb_planted", 1, 0u, 0u, "set bomb_planted" },
                    { 1985007u, (byte)0, (byte)8, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 1, 0u, 0u, "set dropship_destroyed" },
                    { 1985007u, (byte)1, (byte)9, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.bomb_planted", 0, 0u, 0u, "clear bomb_planted" },
                    { 1985007u, (byte)2, (byte)10, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 198678u, 91u, "", 0, 0u, 0u, "wreck -> OPEN 91" },
                    { 1985008u, (byte)0, (byte)9, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 0, 0u, 0u, "clear dropship_destroyed" },
                    { 1985008u, (byte)1, (byte)9, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.bomb_planted", 0, 0u, 0u, "clear bomb_planted" },
                    { 1985009u, (byte)0, (byte)9, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.dropship_destroyed", 0, 0u, 0u, "clear dropship_destroyed" },
                    { 1985009u, (byte)1, (byte)9, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "bootcamp.bomb_planted", 0, 0u, 0u, "clear bomb_planted" }
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[,]
                {
                    { 1985006u, (byte)0 },
                    { 1985007u, (byte)0 },
                    { 1985007u, (byte)1 },
                    { 1985007u, (byte)2 },
                    { 1985008u, (byte)0 },
                    { 1985008u, (byte)1 },
                    { 1985009u, (byte)0 },
                    { 1985009u, (byte)1 }
                });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 1985006u },
                    { 1985007u },
                    { 1985008u },
                    { 1985009u }
                });

            migrationBuilder.DeleteData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198675u },
                    { 198676u },
                    { 198677u },
                    { 198678u },
                    { 198679u },
                    { 198680u },
                    { 198681u },
                    { 198682u },
                    { 198683u }
                });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,]
                {
                    { 198903u, (byte)0, (byte)0 },
                    { 198903u, (byte)0, (byte)1 },
                    { 198903u, (byte)1, (byte)0 },
                    { 198904u, (byte)0, (byte)0 },
                    { 198905u, (byte)0, (byte)0 },
                    { 198906u, (byte)0, (byte)0 },
                    { 198907u, (byte)0, (byte)0 },
                    { 198908u, (byte)0, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "creature",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 198508u },
                    { 198509u },
                    { 198510u },
                    { 198511u },
                    { 198512u },
                    { 198513u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_indicator",
                keyColumns: new[] { "mission_id", "objective_id", "indicator_index" },
                keyValues: new object[,]
                {
                    { 1995u, 1u, (byte)0 },
                    { 1995u, 2u, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_timer",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1995u, 1u },
                    { 2005u, 1u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,]
                {
                    { 1995u, 1u, (byte)0 },
                    { 1995u, 3u, (byte)0 },
                    { 2005u, 1u, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[,]
                {
                    { 1995u, 1u, 4u },
                    { 1995u, 2u, 3u },
                    { 1995u, 3u, 1u },
                    { 2005u, 1u, 4u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { 1995u, 1u },
                    { 1995u, 2u },
                    { 1995u, 3u },
                    { 1995u, 4u },
                    { 2005u, 1u },
                    { 2005u, 4u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_prerequisite",
                keyColumns: new[] { "mission_id", "or_group", "required_mission_id" },
                keyValues: new object[,]
                {
                    { 1995u, (byte)0, 1994u },
                    { 2005u, (byte)0, 1995u },
                    { 2005u, (byte)0, 2005u },
                    { 2005u, (byte)1, 1995u },
                    { 2005u, (byte)1, 2005u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { 1995u },
                    { 2005u }
                });

            // Restore the skeleton rows this migration replaced (NULL flags, original-tier
            // name comments), keeping the Down path consistent with the pre-S5 state.
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { 1995u, 1u, null, null, null, "Destroy the crashed dropship before the bomb timer expires." },
                    { 1995u, 2u, null, null, null, "Locate the missing AFS soldiers" },
                    { 1995u, 3u, null, null, null, "Remove the bomb from Conrad's corpse." },
                    { 1995u, 4u, null, null, null, "Check in with Corporal Van Valkenberg" },
                    { 2005u, 1u, null, null, null, "Destroy the crashed dropship before the bomb timer expires." },
                    { 2005u, 4u, null, null, null, "Check in with Corporal Van Valkenberg" }
                });
        }
    }
}
