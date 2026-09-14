using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The frozen rows of the Wilderness arrival data migration (<c>WildernessArrivalTrainingDay</c>, slice W1), inserted
    /// and deleted by key only. Both provider migrations call this class, so <c>SeedMigrationParityTests</c> sees identical
    /// operations. Never edit after release; corrections go in a new migration with a manifest <c>changes</c> entry.
    ///
    /// Segment 3 begins where the boot camp ends: at the first Alia Das frame the "Headquarters" offer of mission 1526
    /// "Training Day" is already open with Decline greyed (footage B2-023, check SEG3-06), and the mission is turned in at
    /// Training Officer Kincaid in front of the barracks tent (texts 13607-13609, objective conversation (1526,1,2588,1,1),
    /// B2-033/B2-036). Seeded here:
    /// - creature 198515 Training Officer Kincaid (name 10604 original; level 8 read from a partly legible glyph, inferred;
    ///   class 3846 and 1000 hp analogues under OD-11) placed in shared context 1220 at the measured radar-icon position
    ///   (placement 198685, +/-2 m, rotation inferred), package 2588 (the class-trainer package, ClassAdvancement);
    /// - mission 1526 with its objective flags, 120 credits (observed, partly legible) and the Vextronics Pistol /
    ///   Vextronics Pulse Pistol choice (templates 116929/116930 inferred, OD-38);
    /// - condition 198910 and the entered_map rule 1985011 that dispenses 1526 by force once (1995,4) or (2005,4) is
    ///   completed and the character has no 1526 row (OD-40: characters who skip the boot camp are not offered it);
    /// - the item template rows 116929/116930 (itemtemplate, itemtemplate_weapon) the reward needs: observed tooltip
    ///   range and alt damage, inferred quality/category/timings, labelled analogue flags and placeholders (OD-38).
    ///   Their itemtemplate_itemclass and itemtemplate_requirement_skill rows are original world-seed rows and exist already.
    /// The ids 198515/198685/198910/1985011 continue the reserved boot-camp keys, as Rogers (198514/198684) did (OD-36).
    /// Values and tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "WildernessArrivalTrainingDay"); the position in <c>bootcamp-d11-positions.json</c> (npc.training_officer_kincaid).
    /// Literal types must match the mapped CLR types.
    /// </summary>
    public static class WildernessArrivalTrainingDayRows
    {
        public const string Migration = "WildernessArrivalTrainingDay";

        public const uint TrainingDay = 1526u;
        public const uint Kincaid = 198515u;
        public const uint KincaidPlacement = 198685u;
        public const uint OfferCondition = 198910u;
        public const uint OfferRule = 1985011u;
        public const uint VextronicsPistol = 116929u;
        public const uint VextronicsPulsePistol = 116930u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── creature ──
            // {"id": 198515} Training Officer Kincaid: analogue: class_id OD-11, max_hp OD-11; inferred: faction, level (glyph partly legible), run_speed, walk_speed, action1; original: name_id
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[] { Kincaid, "Training Officer Kincaid", 3846u, 1u, 8u, 1000u, 10604u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

            // ── content_placement ──
            // {"id": 198685} Training Officer Kincaid (barracks tent): measured: pos_x ±2.0/0.3 m, pos_y ±2.0/0.3 m, pos_z ±2.0/0.3 m; inferred: rotation, behavior, npc_package_id
            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                values: new object[] { KincaidPlacement, 1220u, (byte)1, Kincaid, 2588u, 0u, (byte)0, 765.4, 294.12, 386.05, 1.5708, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Training Officer Kincaid (barracks tent)" });

            // ── itemtemplate ──
            // {"id": 116929} Vextronics Pistol / {"id": 116930} Vextronics Pulse Pistol: inferred: quality_id, inventory_category;
            // analogue (OD-38): has_sellable_flag, not_tradable_flag, has_character_unique_flag, has_account_unique_flag, has_boe_flag,
            // bound_to_character_flag, not_placable_in_lockbox_flag; buy_price/sell_price 0 omitted (GAP-W1-ITEM-PRICES)
            migrationBuilder.InsertData(
                table: "itemtemplate",
                columns: new[] { "id", "quality_id", "has_sellable_flag", "not_tradable_flag", "has_character_unique_flag", "has_account_unique_flag", "has_boe_flag", "bound_to_character_flag", "not_placable_in_lockbox_flag", "inventory_category", "buy_price", "sell_price" },
                values: new object[,]
                {
                    { VextronicsPistol, (byte)3, (byte)0, (byte)1, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)1, 0, 0 },
                    { VextronicsPulsePistol, (byte)3, (byte)0, (byte)1, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)1, 0, 0 }
                });

            // ── itemtemplate_weapon ──
            // observed: ae_type, range, alt_max_damage, alt_damage_type; inferred: ammo_per_shot, windup, recovery, refire, attack_type;
            // analogue (OD-38): aim_rate, reload_time, alt_action_id, alt_action_arg_id, ae_radius, recoil_amount, cool_rate, heat_per_shot,
            // tool_type, alt_range, alt_ae_radius, alt_ae_type; reuse_override 0 omitted (not sent)
            migrationBuilder.InsertData(
                table: "itemtemplate_weapon",
                columns: new[] { "id", "aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type", "ae_radius", "recoil_amount", "reuse_override", "cool_rate", "heat_per_shot", "tool_type", "ammo_per_shot", "windup", "recovery", "refire", "range", "alt_max_damage", "alt_damage_type", "alt_range", "alt_ae_radius", "alt_ae_type", "attack_type" },
                values: new object[,]
                {
                    { VextronicsPistol, 1.0, 1500u, 1u, 133u, 0u, 1u, 1u, 0u, 1u, 2.0, 15u, 1u, 0u, 250u, 150u, 20u, 115u, 1u, 80u, 1u, 1u, 2u },
                    { VextronicsPulsePistol, 1.0, 1500u, 1u, 133u, 0u, 1u, 1u, 0u, 1u, 2.0, 15u, 1u, 0u, 250u, 100u, 20u, 122u, 1u, 80u, 1u, 1u, 2u }
                });

            // The MissionClientObjectiveSkeleton migration seeded (1526,1) with NULL server-authoritative flags; this migration
            // owns the evidenced values, so the skeleton row is replaced (delete + insert, both data operations).
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { TrainingDay, 1u });

            // ── npc_mission ──
            // {"id": 1526} Training Day: inferred: giver_id (radio), reciver_id, level, group_type, category_id (OD-39), shareable (OD-39), radio_completeable
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[] { TrainingDay, 0u, Kincaid, 4u, (byte)1, 10000001u, false, false, "Training Day" });

            // ── npc_mission_objective ──
            // {"mission_id": 1526, "objective_id": 1} Report to a Class Trainer.: inferred: ordinal, is_required; observed: revealed_on_accept
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[] { TrainingDay, 1u, 1u, true, true, "Report to a Class Trainer." });

            // ── npc_mission_reward ──
            // {"id": 1526, "type": 1} observed: credits 120 (partly legible); {"id": 1526, "type": 5, "item_template_id": 116929/116930}
            // inferred: item_template_id (OD-38), quantity. The Pistol is inserted first: selectable rewards are offered in row order.
            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { TrainingDay, (byte)1, 120, 0u, 0u },
                    { TrainingDay, (byte)5, 0, VextronicsPistol, 1u },
                    { TrainingDay, (byte)5, 0, VextronicsPulsePistol, 1u }
                });

            // ── content_condition ──
            // {"condition_id": 198910} (1995,4) Completed and no 1526 row, or (2005,4) Completed and no 1526 row: inferred: kind, mission_id, objective_id; original: state
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { OfferCondition, (byte)0, (byte)0, (byte)2, 1995u, 4u, 2u, "", 0, false },
                    { OfferCondition, (byte)0, (byte)1, (byte)3, TrainingDay, 0u, 0u, "", 0, false },
                    { OfferCondition, (byte)1, (byte)0, (byte)2, 2005u, 4u, 2u, "", 0, false },
                    { OfferCondition, (byte)1, (byte)1, (byte)3, TrainingDay, 0u, 0u, "", 0, false }
                });

            // ── content_rule ──
            // {"id": 1985011} Training Day radio offer on arrival: inferred: event, condition_id
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[] { OfferRule, 1220u, (byte)1, 0u, 0u, 0u, 0u, 0u, OfferCondition, "Training Day radio offer on arrival" });

            // ── content_rule_action ──
            // {"rule_id": 1985011, "sequence": 0} dispense 1526 forced: inferred: action, forced; observed: mission_id
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[] { OfferRule, (byte)0, (byte)1, TrainingDay, true, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "dispense 1526 forced" });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[] { OfferRule, (byte)0 });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[] { OfferRule });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,]
                {
                    { OfferCondition, (byte)0, (byte)0 },
                    { OfferCondition, (byte)0, (byte)1 },
                    { OfferCondition, (byte)1, (byte)0 },
                    { OfferCondition, (byte)1, (byte)1 }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[,]
                {
                    { TrainingDay, (byte)1, 0u },
                    { TrainingDay, (byte)5, VextronicsPistol },
                    { TrainingDay, (byte)5, VextronicsPulsePistol }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { TrainingDay, 1u });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { TrainingDay });

            // Restore the MissionClientObjectiveSkeleton row this migration replaced.
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[] { TrainingDay, 1u, null, null, null, "Report to a Class Trainer." });

            migrationBuilder.DeleteData(
                table: "itemtemplate_weapon",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { VextronicsPistol }, { VextronicsPulsePistol } });

            migrationBuilder.DeleteData(
                table: "itemtemplate",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { VextronicsPistol }, { VextronicsPulsePistol } });

            migrationBuilder.DeleteData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[] { KincaidPlacement });

            migrationBuilder.DeleteData(
                table: "creature",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Kincaid });
        }
    }
}
