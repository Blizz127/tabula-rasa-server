using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Creature loot read from the footage: Thrax Skull junk and standard-grade ammunition on the Thrax infantry, and
    /// Boargar Ear on the Young Forest Boargar, at the rates counted in <c>docs/evidence/creature-loot-footage-ledger.json</c>
    /// (OD-111, OD-112). The emulator's cartridge row on the same creatures goes, because every observed stack contradicts it.
    ///
    /// <b>The ledger.</b> Every loot drop visible in the supplied footage: 40 in the final-week session (7Lrst9SG3pk,
    /// 8VXeKzGUv0c, Ycxm8Pa1-v4, the transcripts of 2026-09-13 and their verification pass), and 32 entries of squad loot
    /// in the January 2009 Pravus run (A4udsM0rcLo, read from its chat at 1/4 fps), with every credited kill around them
    /// in 20 windows. The final-week drops are Thrax Skull (25), standard-grade ammunition (5), Boargar Ear (2), two
    /// schematics, a Mimeomech stack, Motor Assist legs, an experimental shotgun, a player-named "red" Laser Cannon, holiday
    /// snowballs and a mission Thrax Heart.
    ///
    /// <b>Thrax Skull (template 41666, class 20307 Loot_Junk_Thrax_Skull).</b> 22 skulls in 42 credited Thrax Infantry
    /// Initiate kills: 52.38% (measured; Wilson 95% 37.7-66.6%). It is a lower bound of the per-corpse chance - a corpse
    /// the player never opened counts as a kill without a skull - and three of the four corpse windows whose contents are
    /// all on screen hold one. Every one of the 22 lines reads 1 (measured, 1-1). The Initiates are creatures 198507 (L2)
    /// and 198513 (L1), this world's rows for client name 7674, which is what every corpse window and target frame names.
    ///
    /// <b>Ammunition (OD-111).</b> The audit expected ammunition of the killer's weapon. The ledger refutes that: five of
    /// the seven drops whose receiver's weapon is known are power cells or rockets, dropped to a Shinobi Rifle, an AccuMax
    /// Shotgun and a Teleract Rifle - all three fire Standard Grade Cartridges, as their own tooltips say - and in A5 the
    /// rifle's reserve did not grow by the 117 cells. What the 19 ammunition drops do show is all five of the client's
    /// standard-grade weapon ammunition classes (the Ammo_*_1_Standard_Grade classes that weaponclass rows name as ammo):
    /// cartridges 5, power cells 4, rockets 5, canister 2, pharmaceuticals 3. So each Initiate carries one row per type.
    /// The total, 4 drops in the 42 kills (9.52%, Wilson 3.8-22.1%), is measured; its even split, 1.90% a type, is
    /// inferred from that spread. Each type's stack range is the smallest and largest stack observed of it anywhere in
    /// the ledger (measured): cartridges 117-249, power cells 80-159, rockets 33-47, canister 48-55, pharmaceuticals
    /// 105-281. The Pravus stacks come from level 8-12 kills; the Initiate's own four (117, 159, 38, 249) lie inside them.
    ///
    /// <b>Thrax Soldier (creature 3, OD-112).</b> The world's Wilderness Thrax stand-in: Thrax Footsoldier body, 26 pools
    /// in the Wilderness, and the owner of the emulator's "(Raiding) Trainee Thrax Footsoldier" rows. The Wilderness Thrax
    /// on camera are Thrax Infantry Trainees (levels 4-6), a creature this world does not seed: 3 skulls and 1 rockets
    /// stack in 4 kills, inside the Initiate's intervals. Creature 3 takes the Initiate's rows (inferred).
    ///
    /// <b>Young Forest Boargar (creature 44, OD-112).</b> Two credited kills, two Boargar Ear drops (quantities 2 and 1):
    /// 100% measured on the smallest sample in the ledger (Wilson 34.2-100%), template 42296, stack 1-2.
    ///
    /// <b>Removed.</b> creature_loot 1, 8 and 15, the emulator's Standard Grade Cartridges at 12% for 1-35 on creatures 3,
    /// 198507 and 198513 (analogues under OD-96, which lets a final-era observation replace them): no observed stack is
    /// under 33 and the type varies. The emulator's Motor Assist armour (0.5% each) and med pack (5%) rows stay: one
    /// Motor Assist piece and no med pack in 42 kills neither confirms nor contradicts them.
    ///
    /// <b>Not seeded</b> (GAP-LOOT-*): schematics, random gear, crafting resources, seasonal items and the final patch's
    /// named red gear are on camera but no count supports a rate; higher ammunition grades are never seen; squad loot
    /// distribution (need and greed rolls, "X looted") is a separate mechanic. Every creature the ledger does not cover
    /// keeps the stand-in drop, an analogue under OD-110 (CreatureLoot.StandInDrop).
    /// </summary>
    public static class CreatureLootFootageRows
    {
        public const string Migration = "CreatureLootFootage";

        public const uint InitiateL2 = 198507u, InitiateL1 = 198513u, ThraxSoldier = 3u, YoungForestBoargar = 44u;

        public const uint ThraxSkull = 41666u, BoargarEar = 42296u;

        /// <summary>22 skulls in 42 credited Initiate kills (ledger statistics.thrax_infantry_initiate).</summary>
        public const double ThraxSkullChance = 52.38;

        /// <summary>4 ammunition drops in the same 42 kills, split evenly over the five standard-grade types.</summary>
        public const double AmmoChancePerType = 1.90;

        /// <summary>Two Young Forest Boargar kills, two Boargar Ear drops.</summary>
        public const double BoargarEarChance = 100.0;

        /// <summary>The five standard-grade weapon ammunition templates and the observed stack range of each.</summary>
        public static readonly (uint Template, uint Min, uint Max, string Label)[] Ammo =
        {
            (28u, 117u, 249u, "Standard Grade Cartridges"),
            (56u, 80u, 159u, "Standard Grade Power Cells"),
            (30u, 33u, 47u, "Standard Grade Rockets"),
            (32u, 48u, 55u, "Standard Grade Canister Ammunition"),
            (636u, 105u, 281u, "Standard Grade Pharmaceuticals")
        };

        /// <summary>The creatures that take the Thrax rows, in id order of their block: 199400, 199406, 199412.</summary>
        public static readonly uint[] ThraxCreatures = { InitiateL2, InitiateL1, ThraxSoldier };

        public const uint FirstId = 199400u, BoargarEarId = 199418u;

        /// <summary>The emulator rows removed: (creature_loot id, creature).</summary>
        public static readonly (uint Id, uint Creature)[] EmulatorCartridgeRows = { (1u, 3u), (8u, 198507u), (15u, 198513u) };

        private static readonly string[] Columns = { "id", "creature_id", "item_template_id", "chance", "stacksize_min", "stacksize_max", "comment" };
        private static readonly string[] Types = { "INTEGER", "INTEGER", "INTEGER", "double", "INTEGER", "INTEGER", "varchar(96)" };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (id, _) in EmulatorCartridgeRows)
                Delete(migrationBuilder, id);

            var next = FirstId;
            foreach (var creature in ThraxCreatures)
            {
                var basis = creature == ThraxSoldier ? "inferred, Initiate rate" : "measured, 22/42";
                // {"id": 199400|199406|199412} Thrax Skull: inferred: creature_id, item_template_id;
                // measured (199412 inferred): chance; measured: stacksize_min, stacksize_max
                Row(migrationBuilder, next++, creature, ThraxSkull, ThraxSkullChance, 1u, 1u,
                    $"Thrax Skull {ThraxSkullChance}% x1 ({basis}; loot ledger)");

                foreach (var (template, min, max, label) in Ammo)
                    // {"id": ...} ammunition: inferred: creature_id, item_template_id, chance (the even split of the
                    // measured 4/42); measured: stacksize_min, stacksize_max (ledger ammo_types)
                    Row(migrationBuilder, next++, creature, template, AmmoChancePerType, min, max,
                        $"{label} {AmmoChancePerType}% {min}-{max} (OD-111; loot ledger)");
            }

            // {"id": 199418} Boargar Ear: inferred: creature_id, item_template_id; measured: chance, stacksize_min, stacksize_max
            Row(migrationBuilder, BoargarEarId, YoungForestBoargar, BoargarEar, BoargarEarChance, 1u, 2u,
                "Boargar Ear 100% 1-2 (measured, 2/2; OD-112; loot ledger)");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            for (var id = FirstId; id <= BoargarEarId; id++)
                Delete(migrationBuilder, id);

            // Exactly what Add_creature_loot seeded (CreatureLootPreloader).
            foreach (var (id, creature) in EmulatorCartridgeRows)
                Row(migrationBuilder, id, creature, 28u, 12.0, 1u, 35u,
                    $"creature_type_loot type 20 -> creature {creature}: Standard Grade Cartridges (12%, 1-35)");
        }

        // EF Core 5 does not retain column types through the positional overloads; attach them so both providers
        // generate SQL without a target model (the TordenConversationMissions pattern).
        private static void Row(MigrationBuilder migrationBuilder, uint id, uint creature, uint template, double chance, uint min, uint max, string comment)
        {
            migrationBuilder.InsertData(table: "creature_loot", columns: Columns,
                values: new object[] { id, creature, template, chance, min, max, comment });
            ((InsertDataOperation)migrationBuilder.Operations.Last()).ColumnTypes = Types;
        }

        private static void Delete(MigrationBuilder migrationBuilder, uint id)
        {
            migrationBuilder.DeleteData(table: "creature_loot", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            ((DeleteDataOperation)migrationBuilder.Operations.Last()).KeyColumnTypes = new[] { "INTEGER" };
        }
    }
}
