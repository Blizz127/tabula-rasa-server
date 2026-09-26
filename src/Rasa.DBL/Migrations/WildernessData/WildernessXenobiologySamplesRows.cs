using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Wilderness doctors' sample missions collect the sample itself, as Forming Alliances (479) collects its
    /// Thrax hearts, and two more of the series are offered.
    ///
    /// What:
    /// * <b>758 Fithikally Challenged</b> (Dr. Soji 111 gives and receives): "Acquire 10 Fithik Spleens", item class
    ///   11161 "Fithik Spleen", template 2533, dropped by the Wilderness Fithik (creature 1). Gated on 771 Droning On.
    /// * <b>776 Soldier's Blood</b> (Dr. Ojy 110 gives and receives): "Acquire 10 Samples of Thrax Blood", item class
    ///   11150 "Thrax Blood Sample", template 2524, dropped by the Thrax Soldier (creature 3, 479's source). No gate.
    /// * Both pay 4,000 XP and 600 credits and sit at level 5.
    /// * <b>787 Xanx For the Help</b> is gated on 758: the client's own 787 opening says "Oh, say, you really hooked me up
    ///   with those Fithik spleens".
    /// * <b>771 Droning On</b> and <b>787</b> stop counting kills (the OD-47 stand-in of WildernessCollectionDrop) and
    ///   collect their items: 771 Shield Drone Scraps 11153 (template 2527) from the Bane Shield Drone (85), 787 Xanx
    ///   Pincers 11160 (template 2532) from the Bane Xanx (87). 771's credits become 600.
    ///
    /// Evidence (research/20260926-collection-missions/dossiers.json; tiers per field in
    /// docs/evidence/bootcamp-d11-reconstruction-manifest.json, migrations WildernessXenobiologySamples):
    /// objective texts and targets are original (missionobjective / missiontextlanguage). The item classes are original
    /// (the MisXeno* mission-item block 11146-11167 of entityclass, named in physicalentityclassnamelanguage and matched
    /// to each objective by name, as 479 was matched to 10346); each class maps to two identical templates and the lower
    /// one, inside the consecutive block 2521-2539, is taken by 479's rule (inferred). Givers, receivers, the 758 gate,
    /// experience and credits are inferred from the client log texts and dated TaRapedia and Ellatha records. Where the
    /// two disagree on credits (776 and 771: Ellatha 600, TaRapedia's 2007 beta page 300) the later Ellatha record is
    /// taken, the rule 479 used. Reward items are left out: every list predates update 1.4 (2008-01-29).
    ///
    /// Drop chance: no rate survives for any of these items. 758 and 787 drop on every kill of their source (100,
    /// inferred, low confidence), because TaRapedia's walkthroughs count kills equal to the objective's own target -
    /// "Kill ten collect item" for ten spleens (rev 15141, 2007-11-16) and "Travel South Kill 4 Xanx" for four pincers
    /// (revs 15143, 22864). 776 and 771 have no such evidence and repeat 479's 50% estimate as an analogue under OD-61.
    /// Every value is open in GAP-COLLECTION-DROP-CHANCE.
    ///
    /// Known geography gap (GAP-758-FITHIK-GEOGRAPHY): TaRapedia and Ellatha send the player to the Fithik of Ranja
    /// Cavern, where the world seed has only the Hive Monarch boss (89); the nearest ordinary Fithik pool (141, Gellman
    /// Meadow) is 392 m from Soji.
    /// </summary>
    public static class WildernessXenobiologySamplesRows
    {
        public const string Migration = "WildernessXenobiologySamples";

        public const uint FithikallyChallenged = 758u;
        public const uint SoldiersBlood = 776u;
        public const uint DroningOn = 771u;
        public const uint XanxForTheHelp = 787u;

        public const uint DrOjy = 110u;
        public const uint DrSoji = 111u;

        public const uint Fithik = 1u;
        public const uint ThraxSoldier = 3u;
        public const uint BaneShieldDrone = 85u;
        public const uint BaneXanx = 87u;

        public const uint FithikSpleenTemplate = 2533u;
        public const uint ThraxBloodTemplate = 2524u;
        public const uint ShieldDroneScrapsTemplate = 2527u;
        public const uint XanxPincersTemplate = 2532u;

        /// <summary>758 and 787: TaRapedia's walkthrough kill count equals the objective target (inferred, low confidence).</summary>
        public const double EveryKillDropChance = 100.0;

        /// <summary>776 and 771: 479's estimate, an analogue under OD-61.</summary>
        public const double EstimatedDropChance = 50.0;

        private const uint WildernessBand = 5u;
        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte Completed = 4;
        private const byte KillBinding = 6;
        private const byte ItemCollected = 9;

        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] CounterColumns = { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" };
        private static readonly string[] CounterTypes = { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] BindingColumns =
        {
            "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id",
            "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment"
        };
        private static readonly string[] BindingTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER",
            "INTEGER", "INTEGER", "INTEGER", "double", "INTEGER", "INTEGER", "INTEGER", "varchar(50)"
        };
        private static readonly string[] PrerequisiteColumns = { "mission_id", "or_group", "required_mission_id", "required_state", "comment" };
        private static readonly string[] PrerequisiteTypes = { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardTypes = { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] ThreeKeyTypes = { "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] ConvertedColumns = { "kind", "item_template_id", "drop_chance", "comment" };
        private static readonly string[] ConvertedTypes = { "INTEGER", "INTEGER", "double", "varchar(50)" };

        /// <summary>The two new missions: id, giver = receiver, the client objective and its text, experience, credits, comment.</summary>
        public static readonly (uint Id, uint Doctor, uint Objective, string Text, int Experience, int Credits, string Comment)[] Missions =
        {
            (FithikallyChallenged, DrSoji, 3u, "Acquire 10 Fithik Spleens", 4000, 600, "Fithikally Challenged (W3)"),
            (SoldiersBlood, DrOjy, 2u, "Acquire 10 Samples of Thrax Blood", 4000, 600, "Soldier's Blood (W3)")
        };

        /// <summary>The item drops: mission, objective, target, source creature, template, drop chance, comment.</summary>
        public static readonly (uint Mission, uint Objective, int Target, uint Creature, uint Template, double DropChance, string Comment)[] NewDrops =
        {
            (FithikallyChallenged, 3u, 10, Fithik, FithikSpleenTemplate, EveryKillDropChance, "758/3 Fithik spleen, every kill (inferred)"),
            (SoldiersBlood, 2u, 10, ThraxSoldier, ThraxBloodTemplate, EstimatedDropChance, "776/2 Thrax blood, estimated 50%")
        };

        /// <summary>The two OD-47 kill counts that become item drops: mission, objective, template, drop chance, comments (new, old).</summary>
        public static readonly (uint Mission, uint Objective, uint Template, double DropChance, string Comment, string OldComment)[] Converted =
        {
            (DroningOn, 2u, ShieldDroneScrapsTemplate, EstimatedDropChance, "771/2 Shield Drone Scraps, estimated 50%", "771/2 kill creature 85"),
            (XanxForTheHelp, 3u, XanxPincersTemplate, EveryKillDropChance, "787/3 Xanx Pincers, every kill (inferred)", "787/3 kill creature 87")
        };

        /// <summary>The gates: mission, required mission, comment.</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Gates =
        {
            (FithikallyChallenged, DroningOn, "Droning On completed"),
            (XanxForTheHelp, FithikallyChallenged, "Fithikally Challenged completed")
        };

        public const int DroningOnCredits = 600;
        public const int DroningOnPreviousCredits = 300;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Doctor, m.Doctor, WildernessBand, (byte)1, GeneralCategory, false, false, m.Comment })));

            // Each mission's single client objective: required, first, revealed on acceptance.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id, m.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Objective, m.Text, true, 1u, true })));

            InsertTyped(migrationBuilder, "npc_mission_objective_counter", CounterColumns, CounterTypes,
                Rows(NewDrops.Select(d => new object[] { d.Mission, d.Objective, (byte)0, 0, d.Target })));
            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                Rows(NewDrops.Select(d => new object[]
                {
                    d.Mission, d.Objective, (byte)0, ItemCollected, 0u, 0u, d.Creature, 0u, false, (byte)0, d.Template, d.DropChance,
                    0u, 0u, (byte)0, d.Comment
                })));

            InsertTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteColumns, PrerequisiteTypes,
                Rows(Gates.Select(g => new object[] { g.Mission, (byte)0, g.Required, Completed, g.Comment })));

            // The amount lives in the `credits` column for either reward type.
            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, RewardTypes,
                Rows(Missions.SelectMany(m => new[]
                {
                    new object[] { m.Id, Experience, m.Experience, 0u, 0u },
                    new object[] { m.Id, Credits, m.Credits, 0u, 0u }
                })));

            // 771 and 787: the kill count becomes the item the objective asks for.
            foreach (var c in Converted)
                UpdateTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, ThreeKeyTypes,
                    new object[] { c.Mission, c.Objective, (byte)0 }, ConvertedColumns, ConvertedTypes,
                    new object[] { ItemCollected, c.Template, c.DropChance, c.Comment });

            UpdateTyped(migrationBuilder, "npc_mission_reward", new[] { "id", "type", "item_template_id" }, ThreeKeyTypes,
                new object[] { DroningOn, Credits, 0u }, new[] { "credits" }, new[] { "INTEGER" }, new object[] { DroningOnCredits });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            UpdateTyped(migrationBuilder, "npc_mission_reward", new[] { "id", "type", "item_template_id" }, ThreeKeyTypes,
                new object[] { DroningOn, Credits, 0u }, new[] { "credits" }, new[] { "INTEGER" }, new object[] { DroningOnPreviousCredits });

            foreach (var c in Converted)
                UpdateTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, ThreeKeyTypes,
                    new object[] { c.Mission, c.Objective, (byte)0 }, ConvertedColumns, ConvertedTypes,
                    new object[] { KillBinding, 0u, 0.0, c.OldComment });

            DeleteTyped(migrationBuilder, "npc_mission_reward", new[] { "id", "type", "item_template_id" }, ThreeKeyTypes,
                Rows(Missions.SelectMany(m => new[] { new object[] { m.Id, Experience, 0u }, new object[] { m.Id, Credits, 0u } })));
            DeleteTyped(migrationBuilder, "npc_mission_prerequisite", new[] { "mission_id", "or_group", "required_mission_id" }, ThreeKeyTypes,
                Rows(Gates.Select(g => new object[] { g.Mission, (byte)0, g.Required })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, ThreeKeyTypes,
                Rows(NewDrops.Select(d => new object[] { d.Mission, d.Objective, (byte)0 })));
            DeleteTyped(migrationBuilder, "npc_mission_objective_counter", new[] { "mission_id", "objective_id", "counter_id" }, ThreeKeyTypes,
                Rows(NewDrops.Select(d => new object[] { d.Mission, d.Objective, (byte)0 })));

            // The client's objective rows go back with their text and NULL flags.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id, m.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Objective, m.Text, null, null, null })));

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" },
                Rows(Missions.Select(m => new object[] { m.Id })));
        }

        private static object[,] Rows(System.Collections.Generic.IEnumerable<object[]> rows)
        {
            var list = rows.ToList();
            var values = new object[list.Count, list.Count == 0 ? 0 : list[0].Length];
            for (var row = 0; row < list.Count; row++)
                for (var column = 0; column < list[row].Length; column++)
                    values[row, column] = list[row][column];
            return values;
        }

        // EF Core 5 does not retain ColumnTypes through its positional data overloads; attach them so both providers
        // generate SQL without a target model (the WildernessHubFormingAlliances pattern).
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

        private static void UpdateTyped(MigrationBuilder builder, string table, string[] keyColumns, string[] keyTypes, object[] keyValues,
            string[] columns, string[] types, object[] values)
        {
            builder.UpdateData(table, keyColumns, keyValues, columns, values);
            var operation = (UpdateDataOperation)builder.Operations.Last();
            operation.KeyColumnTypes = keyTypes;
            operation.ColumnTypes = types;
        }
    }
}
