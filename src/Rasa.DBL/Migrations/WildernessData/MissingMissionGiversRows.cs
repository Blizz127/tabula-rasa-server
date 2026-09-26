using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the mission givers the 2026-09-26 dossiers could not place, and the five conversation missions they unblock.
    ///
    /// What. Four NPCs are created where Ten Ton Hammer's dated area guides (research/20260926-missing-npcs, the source
    /// the earlier dossiers had not searched) put them: Cmd. Sgt. Simpson and Ranger Tarina in Foreas Base, Field
    /// Sergeant Hanna at Raintree Post (carrying her client package 423) and Sergeant Dekay in Irendas Penal Colony.
    /// Receptive Liaison Standley and Arizpe take the client packages 2049 and 2025 that only 1741 and 1744 complete on,
    /// and Receptive Liaison Langerman, who gives 1741, moves off the Redshirt class the client cannot converse with.
    /// Warrior Mela, the receiver of 390, moves from upstream's map-marker analogue (OD-59) to the dated reading the same
    /// guide gives. Seeded: 1741 Report to Liaison Standley (Langerman -> Standley), 1744 Report to Liaison Arizpe
    /// (Noonan -> Arizpe), 390 Supplies for Thoria Das (Tarina -> Mela), 818 Report to Field Sergeant Hanna
    /// (Foletto -> Hanna) and 1862 The Infensus Garrison Directive (Dekay -> Michan).
    ///
    /// Held, each with its gap: Col. Almos and 551 (his only reading is 3.16 m above the only floor under it, never
    /// within 0.3 m of any floor inside its 8 m uncertainty, and its y/z equal the client's "Viands Village" label -
    /// GAP-ALMOS-HEIGHT); Sergeant Conway and 835 (no position, GAP-CONWAY-POSITION); 340 (the 340/1905 escort split);
    /// 833/841 and 842/848 (the two arms of 827, neither of which can be gated while 831/840 are unseeded).
    ///
    /// Evidence and tiers (docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration MissingMissionGivers;
    /// docs/evidence/missing-mission-givers-navmesh.json for every probe). Names are original (creaturenamelanguage.pyo);
    /// x and z are the guides' /loc readings, inferred and dated (all pre-D11; none of these settlements was rebuilt),
    /// except Dekay's, the measured midpoint of the "between the wormhole and waypoint" the guide describes (+/-26 m);
    /// y is the navmesh floor under that x,z, measured. Hanna's and Dekay's levels are TaRapedia's infobox values,
    /// inferred; Simpson's and Tarina's are the same-post counterpart's 20, analogue. Entity classes, bodies, hit points
    /// and movement rates are analogues under OD-45: 3846/3848 NPC_Human_Swapset for the AFS NPCs with the bodies the
    /// NPC-creation batches copy, and for the Forean ranger Tarina 7034 NPC_Forean_Spearman with the spear the world's
    /// Forean rangers of that class carry. Mission objectives are the client's own rows with the single-objective flags
    /// inferred; givers and receivers are inferred from the client texts and dated TaRapedia revisions; experience and
    /// credits are TaRapedia's with their era labelled (only 1862 is recorded after Update 1.4, and only its item choice
    /// is seeded - it resolves to one template per name); levels are the zone band (GAP-MISSION-LEVEL). Left out and
    /// recorded instead: 818's prerequisite 816 (unseeded), the pre-1.4 item lists of 390 and 818, and the crate and
    /// directive handed over at accept.
    ///
    /// No row here completes an objective through a giver's package: every objectiveconversation row of the five
    /// missions sits on the receiver's own package (MissionRedirectConversations needs no entry).
    /// </summary>
    public static class MissingMissionGiversRows
    {
        public const string Migration = "MissingMissionGivers";

        public const uint Langerman = 133u, Standley = 134u, Noonan = 199004u, Arizpe = 199085u;
        public const uint Foletto = 510066u, Mela = 510117u, Michan = 510090u;
        public const uint Simpson = 199950u, Tarina = 199951u, Hanna = 199952u, Dekay = 199953u;

        public const uint StandleyPackage = 2049u, ArizpePackage = 2025u, HannaPackage = 423u;

        private const uint RedshirtClass = 29423u;
        private const uint HumanSwapsetMale = 3846u;
        private const uint HumanSwapsetFemale = 3848u;
        private const uint ForeanSpearman = 7034u;

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte SelectableItem = 5;
        private const byte CreaturePlacement = 1;
        private const byte Stationary = 1;

        /// <summary>creature_appearance[510094] (Senior Quartermaster Hacienda), the outfit EarlyReadyMissions gave Standley.</summary>
        public static readonly (uint Slot, uint Class, uint Color)[] LiaisonOutfit =
        {
            (2u, 4021u, 4294934528u), (13u, 6271u, 1u), (14u, 9781u, 4278655809u),
            (15u, 4023u, 4294934528u), (16u, 4022u, 4294934528u), (17u, 24008u, 4286690539u)
        };

        /// <summary>The bodies the NPC-creation batches copy (TarapediaMissingNpcBatch, OD-45), per entity class.</summary>
        public static readonly Dictionary<uint, (uint Slot, uint Class, uint Color)[]> Bodies = new()
        {
            // Field Sgt. Witherspoon, creature 101.
            { HumanSwapsetMale, new[] { (2u, 4021u, 22120u), (13u, 27120u, 1u), (14u, 3672u, 1u), (16u, 4022u, 13933202u), (15u, 4023u, 13933202u), (17u, 20824u, 4286886614u) } },
            // General Supply Vendor Twin Pillars, creature 63.
            { HumanSwapsetFemale, new[] { (2u, 4021u, 266553u), (14u, 25255u, 266553u), (15u, 19296u, 266553u), (16u, 19250u, 266553u), (17u, 24019u, 4286886614u) } },
            // The spear every world Forean ranger of this class carries (Mela 510117, Jorai 510133, Cyrida 510131).
            { ForeanSpearman, new[] { (13u, 10532u, 1u) } }
        };

        /// <summary>The created NPCs: id, comment, name id, class, level, map, x, floor y, z, placement package, placement comment.</summary>
        public static readonly (uint Id, string Comment, uint NameId, uint Class, uint Level, uint Map, double X, double Y, double Z, uint Package, string PlacementComment)[] Npcs =
        {
            (Simpson, "Cmd. Sgt. Simpson (Foreas Base command center)", 127u, HumanSwapsetMale, 20u, 1148u, -34.3, 120.924, 426.8, 0u, "Cmd. Sgt. Simpson (TTH and TaRapedia /loc)"),
            (Tarina, "Ranger Tarina (Foreas Base)", 135u, ForeanSpearman, 20u, 1148u, -55.1, 116.139, 507.7, 0u, "Ranger Tarina (TTH /loc, 2007-10-23)"),
            (Hanna, "Field Sergeant Hanna (Raintree Post)", 4846u, HumanSwapsetFemale, 28u, 1761u, -176.0, 276.008, 279.0, HannaPackage, "Field Sergeant Hanna (TTH /loc, waypoint)"),
            (Dekay, "Sergeant Dekay (Irendas Penal Colony)", 8908u, HumanSwapsetMale, 23u, 1764u, 312.3, 429.924, -191.1, 0u, "Sergeant Dekay (wormhole-waypoint midpoint)")
        };

        /// <summary>Warrior Mela's pool: the preloader's single-precision marker position, and the guide's reading it takes.</summary>
        public static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Y, double Z) MelaMove =
            (Mela, 282.7f, 170.62f, 1071.8f, 222.9, 167.614, 977.0);

        /// <summary>The two liaisons' client conversation packages, each completing one mission only (1741, 1744).</summary>
        public static readonly (uint Id, uint Package, string Comment)[] Packages =
        {
            (Standley, StandleyPackage, "Receptive Liaison Standley (client package 2049)"),
            (Arizpe, ArizpePackage, "Receptive Liaison Arizpe (client package 2025)")
        };

        /// <summary>The missions: id, giver creature, receiver creature, level (zone band), comment.</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (1741u, Langerman, Standley, 5u, "Report to Liaison Standley (W3)"),
            (1744u, Noonan, Arizpe, 10u, "Report to Liaison Arizpe (Divide)"),
            (390u, Tarina, Mela, 10u, "Supplies for Thoria Das (Divide)"),
            (818u, Foletto, Hanna, 35u, "Report to Field Sergeant Hanna (Incline)"),
            (1862u, Dekay, Michan, 35u, "The Infensus Garrison Directive (Plains)")
        };

        /// <summary>The client's single objective of each mission; required, first and revealed on acceptance (inferred).</summary>
        public static readonly (uint Mission, uint Objective, string Text)[] Objectives =
        {
            (1741u, 1u, "Report to Liaison Standley"),
            (1744u, 4u, "Report to Receptive Liaison Arizpe"),
            (390u, 1u, "Deliver Crate to Warrior Mela"),
            (818u, 1u, "Report to Field Sergeant Hanna"),
            (1862u, 1u, "Deliver the Infensus Garrison Directive")
        };

        /// <summary>Reward rows in offer order; the amount of types 1 and 3 lives in `credits`.</summary>
        public static readonly (uint Mission, byte Type, int Amount, uint Template, uint Quantity)[] Rewards =
        {
            // TaRapedia rev 11590 (2007-10-23): RewardXP 4,000, RewardCredits "None" - no credit row.
            (1741u, Experience, 4000, 0u, 0u),
            (1744u, Experience, 8000, 0u, 0u), (1744u, Credits, 800, 0u, 0u),
            (390u, Experience, 13000, 0u, 0u), (390u, Credits, 1950, 0u, 0u),
            (818u, Experience, 15500, 0u, 0u), (818u, Credits, 2800, 0u, 0u),
            (1862u, Experience, 12500, 0u, 0u), (1862u, Credits, 2500, 0u, 0u),
            // TaRapedia rev 27938 (2008-01-30, after 1.4): Class V Advanced Med Pack x2 / Adrenaline Booster x2 /
            // Fragmentation Grenade x3, choose one.
            (1862u, SelectableItem, 0, 45056u, 2u), (1862u, SelectableItem, 0, 118812u, 2u), (1862u, SelectableItem, 0, 45444u, 3u)
        };

        private static readonly string[] CreatureColumns =
            { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
              "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" };
        private static readonly string[] CreatureTypes =
            { "INTEGER", "varchar(50)", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] PlacementColumns =
            { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind", "pos_x", "pos_y", "pos_z",
              "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id", "windup_ms", "name_override_id",
              "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id",
              "escort_mission_id", "comment" };
        private static readonly string[] PlacementTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL",
              "REAL", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
              "INTEGER", "varchar(50)" };
        private static readonly string[] AppearanceColumns = { "id", "slot_id", "Class_id", "color" };
        private static readonly string[] AppearanceKeyColumns = { "id", "slot_id" };
        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };

        private static string[] Integers(int count) => Enumerable.Repeat("INTEGER", count).ToArray();

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── Langerman gives 1741: off the Redshirt class, on the NPC class his fellow liaison Standley took ──
            SetClass(migrationBuilder, Langerman, HumanSwapsetMale);
            InsertTyped(migrationBuilder, "creature_appearance", AppearanceColumns, Integers(4),
                Rows(LiaisonOutfit.Select(piece => new object[] { Langerman, piece.Slot, piece.Class, piece.Color })));

            InsertTyped(migrationBuilder, "npc_package", new[] { "id", "package_id", "comment" }, new[] { "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Packages.Select(package => new object[] { package.Id, package.Package, package.Comment })));

            // ── the four givers ──
            InsertTyped(migrationBuilder, "creature", CreatureColumns, CreatureTypes,
                Rows(Npcs.Select(npc => new object[] { npc.Id, npc.Comment, npc.Class, 1u, npc.Level, 555u, npc.NameId, 9.0, 5.0,
                    0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u })));
            InsertTyped(migrationBuilder, "content_placement", PlacementColumns, PlacementTypes,
                Rows(Npcs.Select(npc => new object[] { npc.Id, npc.Map, CreaturePlacement, npc.Id, npc.Package, 0u, (byte)0, npc.X, npc.Y, npc.Z,
                    0.0, Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, npc.PlacementComment })));
            InsertTyped(migrationBuilder, "creature_appearance", AppearanceColumns, Integers(4),
                Rows(Npcs.SelectMany(npc => Bodies[npc.Class].Select(piece => new object[] { npc.Id, piece.Slot, piece.Class, piece.Color }))));

            // ── Mela to the dated reading ──
            MovePool(migrationBuilder, MelaMove.Id, MelaMove.X, MelaMove.Y, MelaMove.Z);

            // ── the missions ──
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, 1u, GeneralCategory, false, false, m.Comment })));

            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, true, 1u, true })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, Integers(5),
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Amount, r.Template, r.Quantity })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, Integers(3),
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Template })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, Integers(2),
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, Integers(1), Rows(Missions.Select(m => new object[] { m.Id })));

            MovePool(migrationBuilder, MelaMove.Id, MelaMove.WasX, MelaMove.WasY, MelaMove.WasZ);

            DeleteTyped(migrationBuilder, "creature_appearance", AppearanceKeyColumns, Integers(2),
                Rows(Npcs.SelectMany(npc => Bodies[npc.Class].Select(piece => new object[] { npc.Id, piece.Slot }))));
            DeleteTyped(migrationBuilder, "content_placement", new[] { "id" }, Integers(1), Rows(Npcs.Select(npc => new object[] { npc.Id })));
            DeleteTyped(migrationBuilder, "creature", new[] { "id" }, Integers(1), Rows(Npcs.Select(npc => new object[] { npc.Id })));

            DeleteTyped(migrationBuilder, "npc_package", new[] { "id" }, Integers(1), Rows(Packages.Select(package => new object[] { package.Id })));

            DeleteTyped(migrationBuilder, "creature_appearance", AppearanceKeyColumns, Integers(2),
                Rows(LiaisonOutfit.Select(piece => new object[] { Langerman, piece.Slot })));
            SetClass(migrationBuilder, Langerman, RedshirtClass);
        }

        private static void SetClass(MigrationBuilder builder, uint creature, uint entityClass)
        {
            builder.UpdateData(table: "creature", keyColumn: "id", keyValue: creature, column: "class_id", value: entityClass);
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }

        private static void MovePool(MigrationBuilder builder, uint pool, double x, double y, double z)
        {
            builder.UpdateData(table: "spawnpool", keyColumns: new[] { "id" }, keyValues: new object[] { pool },
                columns: new[] { "pos_x", "pos_y", "pos_z" }, values: new object[] { x, y, z });
            var update = (UpdateDataOperation)builder.Operations.Last();
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "REAL", "REAL", "REAL" };
        }

        private static object[,] Rows(IEnumerable<object[]> rows)
        {
            var list = rows.ToList();
            var values = new object[list.Count, list.Count == 0 ? 0 : list[0].Length];
            for (var row = 0; row < list.Count; row++)
                for (var column = 0; column < list[row].Length; column++)
                    values[row, column] = list[row][column];
            return values;
        }

        // EF Core 5 does not retain ColumnTypes through its positional InsertData overload; attach them so both
        // providers generate SQL without a target model (the TordenConversationMissions pattern).
        private static void InsertTyped(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] values)
        {
            builder.InsertData(table, columns, values);
            ((InsertDataOperation)builder.Operations.Last()).ColumnTypes = types;
        }

        private static void DeleteTyped(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] values)
        {
            builder.DeleteData(table, columns, values);
            ((DeleteDataOperation)builder.Operations.Last()).KeyColumnTypes = types;
        }
    }
}
