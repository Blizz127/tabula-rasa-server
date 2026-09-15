using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The frozen rows of the class-gear data migration (<c>WildernessClassGear</c>, slice W2), inserted and deleted
    /// by key only. Both provider migrations call this class, so <c>SeedMigrationParityTests</c> sees identical
    /// operations. Never edit after release; corrections go in a new migration with a manifest <c>changes</c> entry.
    ///
    /// The tier-2 class choice (SelectNewCharacterClass) is answered by the two class-gear missions added in D11
    /// (live 2008-08-15): 2010 "Getting It In Gear: Soldier Class" and 2011 "Getting It In Gear: Specialist Class".
    /// Their client texts, names and the Quartermaster Caufield completion binding (package 133) are original, and
    /// the client's missioncategorylanguage names categories 10000002 "Class (Soldier)" and 10000003
    /// "Class (Specialist)". Both are non-abandonable (client NON_ABANDONABLE_MISSIONS = 1990, 2010, 2011).
    ///
    /// The reward is the D11 new-player item block (client itemclass.pyo itemTemplateItemClass ids 122840-122880).
    /// The client skilldata names the block's class gear by class ownership: skill 21 T2_SOLDIER_REFLECTIVE_ARMOR and
    /// 22 T2_SOLDIER_MACHINE_GUN belong to class 2 (Soldier), 30 T2_SPECIALIST_HAZMAT_ARMOR and 14
    /// T2_SPECIALIST_TOOLS to class 3 (Specialist), and the block's item classes require exactly those skills
    /// (itemtemplate_requirement_skill rows 122859/122860/122862/122863/122864 -> 21, 122865 -> 22,
    /// 122866/122867/122868/122869/122870 -> 30, 122871 -> 14; original world-seed rows). So 2010 grants the five
    /// Reflective armor pieces (18504/18596/18458/18550/18412) and the Rage-O-Matic machine gun (27059), and 2011 the
    /// five Hazmat pieces (13710/13802/13664/13756/13618) and the Repair-O-Matic field repair tool (12797). The
    /// per-mission assignment is inferred from that class ownership; the load-out size comes from the block itself.
    ///
    /// Item data the surviving sources still hold:
    /// - The original game-server SQL (upstream gameserver_dev_Full.sql, itemtemplate_armor rows) gives each armor
    ///   piece's armorValue: 126/189/63/158/95 (Soldier) and 95/142/47/118/71 (Specialist). The same values are
    ///   already in itemclass.max_hp and armorclass (regenRate/damageAbsorbed) of the world seed, so only
    ///   itemtemplate_armor.armor_value is inserted here.
    /// - The class ids, equipable slots, level 5 requirement (itemtemplate_requirement by class) and skill
    ///   requirements are already world-seed rows, imported from the final client.
    ///
    /// Item data that is not recoverable and is inserted as a labelled analogue (OD-43):
    /// - itemtemplate quality/flag columns. The whole D11 block carries the same values in the world seed
    ///   (quality 2, sellable, tradeable, no unique/bound/BoE/lockbox flags, category 1); those are used.
    /// - itemtemplate_weapon statistics. Every machine gun and every tool template in the world seed carries one
    ///   identical row; the Rage-O-Matic and Repair-O-Matic use their family's row. Their class damage/clip/ammo
    ///   come from the client's own weaponclass data (already in weaponclass).
    ///
    /// Quartermaster Caufield is emulator creature 132 ("AFS Quartermaster Caufield", level 10, class 29423,
    /// name 2992, stationary), already spawned in context 1220 by spawnpool 210 at (756.88, 293.97, 407.07); the
    /// original mission text places him in the Alia Das supply tent, and the pre-D11 TaRapedia /loc for him
    /// (757.5, 294.0, 405.5) is 1.6 m away. The world seed had no npc_package row for him, so the original
    /// dialogue package 133 (the missions' completion binding) is attached to the existing creature instead of
    /// placing a second Caufield.
    ///
    /// Values and tiers live in <c>docs/evidence/bootcamp-d11-reconstruction-manifest.json</c> (rows with migration
    /// "WildernessClassGear"). Literal types must match the mapped CLR types.
    /// </summary>
    public static class WildernessClassGearRows
    {
        public const string Migration = "WildernessClassGear";

        public const uint SoldierMission = 2010u;
        public const uint SpecialistMission = 2011u;
        public const uint Caufield = 132u;
        public const uint CaufieldPackage = 133u;
        public const uint SoldierOfferCondition = 198911u;
        public const uint SpecialistOfferCondition = 198912u;
        public const uint SoldierOfferRule = 1985012u;
        public const uint SpecialistOfferRule = 1985013u;

        // Soldier: Reflective helmet/vest/gloves/legs/boots and the Rage-O-Matic machine gun.
        public static readonly uint[] SoldierGear = { 122859u, 122860u, 122862u, 122863u, 122864u, 122865u };
        // Specialist: Hazmat helmet/vest/gloves/legs/boots and the Repair-O-Matic field repair tool.
        public static readonly uint[] SpecialistGear = { 122866u, 122867u, 122868u, 122869u, 122870u, 122871u };
        // Original game-server armorValue per armor template (itemtemplate_armor, upstream SQL).
        private static readonly (uint Id, int ArmorValue)[] Armor =
        {
            (122859u, 126), (122860u, 189), (122862u, 63), (122863u, 158), (122864u, 95),
            (122866u, 95), (122867u, 142), (122868u, 47), (122869u, 118), (122870u, 71)
        };

        // The world seed's single machine-gun and single tool weapon row (analogue, OD-43).
        private const double AimRate = 1.0;
        private const double HeatPerShot = 2.0;
        private const uint ReloadTime = 1500u;
        private const uint AltActionId = 1u;
        private const uint AltActionArgId = 133u;
        private const uint AeType = 0u;
        private const uint AeRadius = 1u;
        private const uint RecoilAmount = 1u;
        private const uint ReuseOverride = 0u;
        private const uint CoolRate = 1u;
        private const uint ToolType = 15u;
        private const uint AmmoPerShot = 1u;
        private const uint Windup = 800u;
        private const uint Recovery = 1u;
        private const uint Refire = 800u;
        private const uint Range = 80u;
        private const uint AltMaxDamage = 25u;
        private const uint AltDamageType = 1u;
        private const uint AltRange = 80u;
        private const uint AltAeRadius = 1u;
        private const uint AltAeType = 1u;
        private const uint AttackType = 2u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── npc_package ──
            // {"id": 132} original: package_id 133 (client completion binding of 2010/2011); inferred: the binding is
            // the existing emulator creature 132 "AFS Quartermaster Caufield" (name match + supply-tent text)
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[] { Caufield, CaufieldPackage, "AFS Quartermaster Caufield" });

            // ── itemtemplate ──
            // {"id": 122859..122871} analogue (OD-43): quality_id, the seven flags, inventory_category, buy_price and
            // sell_price take the D11 block's uniform world-seed values; the column has no recovered original.
            foreach (var id in SoldierGear)
                InsertItemTemplate(migrationBuilder, id);
            foreach (var id in SpecialistGear)
                InsertItemTemplate(migrationBuilder, id);

            // ── itemtemplate_armor ──
            // {"id": 122859..122870} original: armor_value from the original game-server itemtemplate_armor rows
            // (upstream gameserver_dev_Full.sql); the pieces' regen and damage-absorbed values are already in armorclass
            foreach (var (id, armorValue) in Armor)
                migrationBuilder.InsertData(
                    table: "itemtemplate_armor",
                    columns: new[] { "id", "armor_value" },
                    values: new object[] { id, armorValue });

            // ── itemtemplate_weapon ──
            // {"id": 122865, "id": 122871} analogue (OD-43): the world seed's uniform machine-gun / tool row
            foreach (var id in new[] { 122865u, 122871u })
                migrationBuilder.InsertData(
                    table: "itemtemplate_weapon",
                    columns: new[] { "id", "aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type",
                        "ae_radius", "recoil_amount", "reuse_override", "cool_rate", "heat_per_shot", "tool_type",
                        "ammo_per_shot", "windup", "recovery", "refire", "range", "alt_max_damage", "alt_damage_type",
                        "alt_range", "alt_ae_radius", "alt_ae_type", "attack_type" },
                    values: new object[] { id, AimRate, ReloadTime, AltActionId, AltActionArgId, AeType, AeRadius,
                        RecoilAmount, ReuseOverride, CoolRate, HeatPerShot, ToolType, AmmoPerShot, Windup, Recovery,
                        Refire, Range, AltMaxDamage, AltDamageType, AltRange, AltAeRadius, AltAeType, AttackType });

            // The MissionClientObjectiveSkeleton migration seeded (2010,1) and (2011,1) with NULL server-authoritative
            // flags; this migration owns the evidenced values, so the skeleton rows are replaced (delete + insert).
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { SoldierMission, 1u });
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[] { SpecialistMission, 1u });

            // ── npc_mission ──
            // {"id": 2010, "id": 2011} original: category 10000002/10000003 (client missioncategorylanguage),
            // client texts; inferred: radio giver 0, receiver Caufield, level 5, group_type 1, shareable false
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { SoldierMission, 0u, Caufield, 5u, (byte)1, 10000002u, false, false, "Getting It In Gear: Soldier Class" },
                    { SpecialistMission, 0u, Caufield, 5u, (byte)1, 10000003u, false, false, "Getting It In Gear: Specialist Class" }
                });

            // ── npc_mission_objective ──
            // {"mission_id": 2010/2011, "objective_id": 1} original: the client objective "Report to Quartermaster
            // Caulfield"; inferred: ordinal 1, is_required, revealed_on_accept
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { SoldierMission, 1u, 1u, true, true, "Report to Quartermaster Caufield" },
                    { SpecialistMission, 1u, 1u, true, true, "Report to Quartermaster Caufield" }
                });

            // ── npc_mission_reward ──
            // {"id": 2010/2011, "type": 4, "item_template_id": ..} inferred: the class load-out is granted whole
            // (fixed items), six per mission; no XP or credit row is recovered (GAP-W2-GEAR-REWARDS)
            foreach (var id in SoldierGear)
                migrationBuilder.InsertData(
                    table: "npc_mission_reward",
                    columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                    values: new object[] { SoldierMission, (byte)4, 0, id, 1u });
            foreach (var id in SpecialistGear)
                migrationBuilder.InsertData(
                    table: "npc_mission_reward",
                    columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                    values: new object[] { SpecialistMission, (byte)4, 0, id, 1u });

            // ── content_condition ──
            // {"condition_id": 198911/198912} the chosen class and no mission row yet: inferred; original: the class
            // ids (2 Soldier, 3 Specialist) and the mission ids
            migrationBuilder.InsertData(
                table: "content_condition",
                columns: new[] { "condition_id", "or_group", "term_index", "kind", "mission_id", "objective_id", "state", "fact_key", "value", "negate" },
                values: new object[,]
                {
                    { SoldierOfferCondition, (byte)0, (byte)0, (byte)6, 0u, 0u, 0u, "", 2, false },
                    { SoldierOfferCondition, (byte)0, (byte)1, (byte)3, SoldierMission, 0u, 0u, "", 0, false },
                    { SpecialistOfferCondition, (byte)0, (byte)0, (byte)6, 0u, 0u, 0u, "", 3, false },
                    { SpecialistOfferCondition, (byte)0, (byte)1, (byte)3, SpecialistMission, 0u, 0u, "", 0, false }
                });

            // ── content_rule ──
            // {"id": 1985012/1985013} the class_selected rule in shared context 1220, where the trainer stands;
            // inferred: the event is the class choice, matching the solved Training Day arrival
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[,]
                {
                    { SoldierOfferRule, 1220u, (byte)13, 0u, 0u, 0u, 0u, 0u, SoldierOfferCondition, "Getting It In Gear radio offer on choosing Soldier" },
                    { SpecialistOfferRule, 1220u, (byte)13, 0u, 0u, 0u, 0u, 0u, SpecialistOfferCondition, "Getting It In Gear radio offer on choosing Specialist" }
                });

            // ── content_rule_action ──
            // {"rule_id": 1985012/1985013, "sequence": 0} dispense the class mission by force: inferred from the
            // broadcast opening ("Attention, new recruits who have chosen the Soldier Class...") and the client's
            // non-abandonable flag
            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                values: new object[,]
                {
                    { SoldierOfferRule, (byte)0, (byte)1, SoldierMission, true, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "dispense 2010 forced" },
                    { SpecialistOfferRule, (byte)0, (byte)1, SpecialistMission, true, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, 0u, 0u, "", 0, 0u, 0u, "dispense 2011 forced" }
                });
        }

        private static void InsertItemTemplate(MigrationBuilder migrationBuilder, uint id)
        {
            // Analogue of the D11 new-player block's uniform world-seed itemtemplate rows (OD-43): quality 2,
            // sellable, tradeable, no character/account unique, BoE or bound flag, placeable in the lockbox,
            // inventory category 1 (equipment). buy_price/sell_price are unrecovered and stored 0 (no vendor
            // price, the neutral value the W1 reward pistols use) rather than the block's nominal 1
            // (GAP-W2-ITEM-PRICES).
            migrationBuilder.InsertData(
                table: "itemtemplate",
                columns: new[] { "id", "quality_id", "has_sellable_flag", "not_tradable_flag", "has_character_unique_flag", "has_account_unique_flag", "has_boe_flag", "bound_to_character_flag", "not_placable_in_lockbox_flag", "inventory_category", "buy_price", "sell_price" },
                values: new object[] { id, (byte)2, (byte)1, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)0, (byte)1, 0, 0 });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[,]
                {
                    { SoldierOfferRule, (byte)0 },
                    { SpecialistOfferRule, (byte)0 }
                });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { SoldierOfferRule }, { SpecialistOfferRule } });

            migrationBuilder.DeleteData(
                table: "content_condition",
                keyColumns: new[] { "condition_id", "or_group", "term_index" },
                keyValues: new object[,]
                {
                    { SoldierOfferCondition, (byte)0, (byte)0 },
                    { SoldierOfferCondition, (byte)0, (byte)1 },
                    { SpecialistOfferCondition, (byte)0, (byte)0 },
                    { SpecialistOfferCondition, (byte)0, (byte)1 }
                });

            foreach (var id in SoldierGear)
                migrationBuilder.DeleteData(
                    table: "npc_mission_reward",
                    keyColumns: new[] { "id", "type", "item_template_id" },
                    keyValues: new object[] { SoldierMission, (byte)4, id });
            foreach (var id in SpecialistGear)
                migrationBuilder.DeleteData(
                    table: "npc_mission_reward",
                    keyColumns: new[] { "id", "type", "item_template_id" },
                    keyValues: new object[] { SpecialistMission, (byte)4, id });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,] { { SoldierMission, 1u }, { SpecialistMission, 1u } });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { SoldierMission }, { SpecialistMission } });

            // Restore the MissionClientObjectiveSkeleton rows this migration replaced.
            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "ordinal", "is_required", "revealed_on_accept", "comment" },
                values: new object[,]
                {
                    { SoldierMission, 1u, null, null, null, "Report to Quartermaster Caulfield" },
                    { SpecialistMission, 1u, null, null, null, "Report to Quartermaster Caulfield" }
                });

            migrationBuilder.DeleteData(
                table: "itemtemplate_weapon",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { 122865u }, { 122871u } });

            foreach (var (id, _) in Armor)
                migrationBuilder.DeleteData(
                    table: "itemtemplate_armor",
                    keyColumns: new[] { "id" },
                    keyValues: new object[] { id });

            foreach (var id in SoldierGear)
                DeleteItemTemplate(migrationBuilder, id);
            foreach (var id in SpecialistGear)
                DeleteItemTemplate(migrationBuilder, id);

            migrationBuilder.DeleteData(
                table: "npc_package",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Caufield });
        }

        private static void DeleteItemTemplate(MigrationBuilder migrationBuilder, uint id)
        {
            migrationBuilder.DeleteData(
                table: "itemtemplate",
                keyColumns: new[] { "id" },
                keyValues: new object[] { id });
        }
    }
}
