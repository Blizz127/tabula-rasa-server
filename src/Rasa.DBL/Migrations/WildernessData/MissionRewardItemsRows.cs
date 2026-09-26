using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The reward items of seven missions that were already seeded with their credits and experience:
    /// 1541 New Orders From The General, 1673 It Lies in Ruins, 1040 Incommunicado, 983 You're the Guy,
    /// 970 Lookout Down Below, 1068 South Of The Border and 1863 Missing in Action. Frozen rows, inserted and
    /// deleted by key only; corrections go in a new migration with a manifest <c>changes</c> entry.
    ///
    /// <b>What the rows say.</b> Each mission offers its items as one choice (type 5, selectable, in the wiki's
    /// order, which is the order the client lists them). Equipment comes one at a time; the consumable bundles
    /// come as 2 med packs, 2 chargers, 4 grenades and 4 grenades. Every value here is <c>inferred</c>.
    ///
    /// <b>Where the item names come from.</b> TaRapedia's <c>RewardItem</c> field, each list first recorded after
    /// update 1.4 (2008-01-29), when "855 missions have had their rewards re-evaluated and replaced with new
    /// items" (RGTR patch notes): 1541 rev 29408 (2008-03-02), 1673 rev 29815 (2008-03-17), 1040 rev 29133
    /// (2008-02-23), 983 rev 30043 (2008-03-29), 970 rev 29117 (2008-02-23), 1068 rev 29406 (2008-03-02), 1863
    /// rev 29490 (2008-03-05, restated unchanged in rev 30095 on 2008-04-01). The final-state claim is only
    /// "post-1.4, and no later documented reward change": the 2008-04-28 notes say roughly 70 unnamed mission
    /// rewards had armor or tool requirements corrected, after every one of these lists was written
    /// (GAP-MISSION-REWARD-FINAL-STATE).
    ///
    /// <b>How a name becomes a template.</b> The final client's itemclass.pyo itemTemplateItemClass stores mission
    /// reward templates as runs of consecutive ids. Each equipment list matches exactly one consecutive run whose
    /// classes carry the wiki's family, armor version (Icon/Armor v3 = _V03_), rarity (green = UNC) and level (the
    /// client's reqData level equals every wiki "lvl N"): 1541 120418-120421, 1673 120101-120104, 1040
    /// 120382-120385, 983 120804-120806. A random list of the 4-entry shape matches a run 0 times in 500; 983's
    /// 3-entry, name-only list 3-5% of the time, so 983 is medium confidence. For 1541 a near-duplicate block
    /// 120109-120112 shares three of its four classes; only "Stealth Legs v4" picks 120418-120421. The consumable
    /// names each map to one client class with one template. No client table links a mission to a template, so
    /// every template id stays inferred (GAP-MISSION-REWARD-TEMPLATE-ID).
    ///
    /// <b>What is not here.</b> The wiki's manufacturer prefixes (Dynamo, Teleract, Astra, Titan, Wellcare,
    /// ChiTech, Prodigy, Shinobi, Animatics) are item modules that no client table maps to a template; no module is
    /// invented (GAP-MISSION-REWARD-MODULES). The seeded mission levels (20-25 for all but 983) stay as they are
    /// although these rewards require 26-39 (GAP-MISSION-LEVEL). Whether the window offered one item or the whole
    /// bundle, and the stack counts, are read from the wiki's layout, not from a completion window
    /// (GAP-MISSION-REWARD-CHOICE).
    ///
    /// Evidence: research/20260926-reward-items (README.md, REVIEW.md, reward-items.json); every field's tier and
    /// citation is in docs/evidence/bootcamp-d11-reconstruction-manifest.json (rows with migration
    /// "MissionRewardItems"). Every template, its class, stack size and inventory category are already in the
    /// world seed (ItemTemplateItemClassPreloader, ItemTemplateRegeneratedPreloader, ItemClassPrelaoder); the seven
    /// weapons have WeaponRowRepair's placeholder itemtemplate_weapon rows.
    /// </summary>
    public static class MissionRewardItemsRows
    {
        public const string Migration = "MissionRewardItems";

        public const uint NewOrdersFromTheGeneral = 1541u;
        public const uint ItLiesInRuins = 1673u;
        public const uint Incommunicado = 1040u;
        public const uint YoureTheGuy = 983u;
        public const uint LookoutDownBelow = 970u;
        public const uint SouthOfTheBorder = 1068u;
        public const uint MissingInAction = 1863u;

        public const byte SelectableItem = 5;

        /// <summary>The seeded rows in insert order: selectable rewards are offered in row order.</summary>
        public static readonly (uint MissionId, uint TemplateId, uint Quantity)[] Rewards =
        {
            // 1541, TaRapedia rev 29408: Hazmat Gloves v3 lvl 34, Motor Assist Gloves v6 lvl 33,
            // Reflective Gloves v3 lvl 34, Stealth Legs v4 lvl 35, all green.
            (NewOrdersFromTheGeneral, 120418u, 1u),
            (NewOrdersFromTheGeneral, 120419u, 1u),
            (NewOrdersFromTheGeneral, 120420u, 1u),
            (NewOrdersFromTheGeneral, 120421u, 1u),
            // 1673, rev 29815: Hazmat Vest v3 lvl 39, Mech Vest v4 lvl 35, Motor Assist Vest v6 lvl 38,
            // Reflective Vest v3 lvl 39, all green.
            (ItLiesInRuins, 120101u, 1u),
            (ItLiesInRuins, 120102u, 1u),
            (ItLiesInRuins, 120103u, 1u),
            (ItLiesInRuins, 120104u, 1u),
            // 1040, rev 29133: Laser Chaingun 33, Electric Net Gun 31, Laser Pistol 30, Endothermic Polarity Gun 33, green.
            (Incommunicado, 120382u, 1u),
            (Incommunicado, 120383u, 1u),
            (Incommunicado, 120384u, 1u),
            (Incommunicado, 120385u, 1u),
            // 983, rev 30043: Electric Pistol, Endothermic Polarity Gun, Cryogenic Net Gun (names only).
            (YoureTheGuy, 120804u, 1u),
            (YoureTheGuy, 120805u, 1u),
            (YoureTheGuy, 120806u, 1u),
            // 970, rev 29117: Class VI Advanced Med Pack (2), Armor Charger (2), Incendiary Grenade (4), EMP Grenade (4).
            (LookoutDownBelow, 45059u, 2u),
            (LookoutDownBelow, 118897u, 2u),
            (LookoutDownBelow, 111037u, 4u),
            (LookoutDownBelow, 111027u, 4u),
            // 1068, rev 29406: Class VI Advanced Med Pack (2), Power Charger (2), Concussion Grenade (4), EMP Grenade (4).
            (SouthOfTheBorder, 45059u, 2u),
            (SouthOfTheBorder, 118906u, 2u),
            (SouthOfTheBorder, 111017u, 4u),
            (SouthOfTheBorder, 111027u, 4u),
            // 1863, rev 29490/30095: Class VII Advanced Med Pack (2), Armor Charger (2), Cryogenic Grenade (4),
            // Fragmentation Grenade (4).
            (MissingInAction, 45062u, 2u),
            (MissingInAction, 118898u, 2u),
            (MissingInAction, 111048u, 4u),
            (MissingInAction, 45446u, 4u)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── npc_mission_reward ──
            // {"id": m, "type": 5, "item_template_id": t} inferred: type (choose one), item_template_id (consecutive
            // client run / unique class), quantity (wiki count); credits 0 (an item row carries no amount)
            var values = new object[Rewards.Length, 5];
            for (var i = 0; i < Rewards.Length; i++)
            {
                values[i, 0] = Rewards[i].MissionId;
                values[i, 1] = SelectableItem;
                values[i, 2] = 0;
                values[i, 3] = Rewards[i].TemplateId;
                values[i, 4] = Rewards[i].Quantity;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: values);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            var keys = new object[Rewards.Length, 3];
            for (var i = 0; i < Rewards.Length; i++)
            {
                keys[i, 0] = Rewards[i].MissionId;
                keys[i, 1] = SelectableItem;
                keys[i, 2] = Rewards[i].TemplateId;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: keys);
        }
    }
}
