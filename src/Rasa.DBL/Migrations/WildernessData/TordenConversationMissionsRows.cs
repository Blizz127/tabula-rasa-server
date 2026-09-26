using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: nine Torden-segment conversation missions whose givers and receivers already stand in the world.
    ///
    /// What: 1745 Report to Liaison Repp, 526 Aid Packages, 648 Speak to Colonel Franks, 802 Deliver Mire Station
    /// Field Report, 1014 Blue Flu, 1064 Incriminating Delivery, 1070 Go to Incline, Young Soldier, 1326 Spoils of War
    /// and 1330 Retread Planning Part II. Every objective already carries a client objectiveconversation row on the
    /// package of an NPC the world seed places, so each mission is completable through conversations the final client
    /// defines, and no NPC, placement or package is created or moved here.
    ///
    /// Why: the 2026-09-26 Torden dossiers (research/20260926-torden-missions) judged these seed-now (1745) or
    /// seed-with-gaps; they were re-checked against the database the recovered 2026-09-22..24 migrations produce.
    /// 936 The Results Are In is held: its giver, Science Officer Clark (spawnpool 510191), stands on a surface the
    /// Plateau navmesh does not connect to the Wedge Rock outpost floor (GAP-TORDEN-936-CLARK-UNREACHABLE).
    ///
    /// Evidence and tiers (docs/evidence/bootcamp-d11-reconstruction-manifest.json, migration TordenConversationMissions):
    /// objective texts are original (missionobjective.pyo); giver and receiver are inferred from the client's own texts
    /// and dated TaRapedia revisions; objective flags, the 1014 order (2 then 1) and the two prerequisites (887, 1063)
    /// are inferred; experience and credits are TaRapedia's recorded amounts, inferred and labelled with their era -
    /// 526, 648, 802, 1064, 1070 and 1745 are only recorded before Update 1.4 (2008-01-29), as the earlier W3 batches
    /// seeded such amounts, while 1326 and 1330 are post-1.4. 1014 has no reward row: its only figure is a closed-beta
    /// XP value (GAP-TORDEN-1014-REWARDS). Item lists recorded only before 1.4 are never seeded
    /// (GAP-TORDEN-REWARD-ITEMS); 1326 and 1330 carry their post-1.4 consumables as choose-one rows whose templates,
    /// quantities and choice structure are inferred. Levels are the zone band (GAP-MISSION-LEVEL); the Pools has no
    /// band, so 1326 and 1330 take the 1068/1541 value 20 as an analogue under OD-60. Prerequisites on unseeded
    /// missions (804, 1324, 563) and 648's unknown gate are left out, and items handed over at accept (526, 802, 1064)
    /// are not granted; each is a GAP entry.
    /// </summary>
    public static class TordenConversationMissionsRows
    {
        public const string Migration = "TordenConversationMissions";

        private const uint GeneralCategory = 10000001u;
        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte SelectableItem = 5;
        private const byte Completed = 4;

        private static readonly string[] MissionColumns =
            { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" };
        private static readonly string[] MissionTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly string[] TransitionColumns = { "mission_id", "completed_objective_id", "revealed_objective_id" };
        private static readonly string[] RewardColumns = { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardKeyColumns = { "id", "type", "item_template_id" };
        private static readonly string[] PrerequisiteColumns = { "mission_id", "or_group", "required_mission_id", "required_state", "comment" };
        private static readonly string[] PrerequisiteKeyColumns = { "mission_id", "or_group", "required_mission_id" };

        /// <summary>The missions: id, giver creature, receiver creature, level, comment.</summary>
        public static readonly (uint Id, uint Giver, uint Receiver, uint Level, string Comment)[] Missions =
        {
            (1745u, 199085u, 199505u, 15u, "Report to Liaison Repp (Palisades)"),
            (526u, 510068u, 510068u, 35u, "Aid Packages (Incline)"),
            (648u, 199045u, 510084u, 35u, "Speak to Colonel Franks (Plains)"),
            (802u, 510067u, 510066u, 35u, "Deliver Mire Station Field Report (Incline)"),
            (1014u, 199069u, 510187u, 20u, "Blue Flu (Plateau)"),
            (1064u, 199510u, 510084u, 35u, "Incriminating Delivery (Plains)"),
            (1070u, 510084u, 510067u, 35u, "Go to Incline, Young Soldier (Plains)"),
            // Pools: no zone band exists; 20 is the 1068/1541 value, an analogue under OD-60 (GAP-MISSION-LEVEL).
            (1326u, 510198u, 199203u, 20u, "Spoils of War (Pools)"),
            (1330u, 199075u, 199204u, 20u, "Retread Planning Part II (Pools)")
        };

        /// <summary>Objectives with the client's own text; ordinal, required and revealed are inferred.</summary>
        public static readonly (uint Mission, uint Objective, string Text, bool Required, uint Ordinal, bool Revealed)[] Objectives =
        {
            (1745u, 5u, "Report to Receptive Liaison Repp", true, 1u, true),
            (526u, 1u, "Deliver aid package to Coordinator Maila", true, 1u, true),
            (526u, 2u, "Deliver aid package to Salvage Master Orto", true, 2u, true),
            (648u, 1u, "Speak to Colonel Franks", true, 1u, true),
            (802u, 1u, "Find Field Commander Foletto", true, 1u, true),
            // Provost first: the log names only him and his completion line sends the player to Norton.
            (1014u, 2u, "Report to CID Headquarters.", true, 1u, true),
            (1014u, 1u, "Report to the Fort Defiance Warehouse.", true, 2u, false),
            (1064u, 1u, "Deliver Encrypted Data to Colonel Franks", true, 1u, true),
            (1070u, 1u, "Speak to Sergeant Obahmi", true, 1u, true),
            (1326u, 1u, "Report to Snake", true, 1u, true),
            (1330u, 1u, "Speak to Amee Corman", true, 1u, true)
        };

        /// <summary>1014: objective 1 (Norton) is revealed when objective 2 (Provost) completes.</summary>
        public static readonly (uint Mission, uint Completed, uint Revealed)[] Transitions =
        {
            (1014u, 2u, 1u)
        };

        /// <summary>Reward rows in the order they are offered: amount in `credits` for types 1 and 3.</summary>
        public static readonly (uint Mission, byte Type, int Amount, uint Template, uint Quantity)[] Rewards =
        {
            (1745u, Experience, 10500, 0u, 0u), (1745u, Credits, 1050, 0u, 0u),
            (526u, Experience, 33000, 0u, 0u), (526u, Credits, 4350, 0u, 0u),
            (648u, Experience, 12500, 0u, 0u), (648u, Credits, 2500, 0u, 0u),
            (802u, Experience, 14500, 0u, 0u), (802u, Credits, 2700, 0u, 0u),
            (1064u, Experience, 50000, 0u, 0u), (1064u, Credits, 5000, 0u, 0u),
            (1070u, Experience, 12500, 0u, 0u), (1070u, Credits, 2500, 0u, 0u),
            (1326u, Experience, 32500, 0u, 0u), (1326u, Credits, 3600, 0u, 0u),
            // TaRapedia rev 29554 (2008-03-06): Class VII Advanced Med Pack x2 / Power Charger x2 /
            // Cryogenic Grenade x4 / Incendiary Grenade x4, choose one.
            (1326u, SelectableItem, 0, 45062u, 2u), (1326u, SelectableItem, 0, 118907u, 2u),
            (1326u, SelectableItem, 0, 111048u, 4u), (1326u, SelectableItem, 0, 111038u, 4u),
            (1330u, Experience, 30000, 0u, 0u), (1330u, Credits, 3500, 0u, 0u),
            // TaRapedia rev 29474 (2008-03-04): Class VII Advanced Med Pack x2 / Armor Charger x2 /
            // Fragmentation Grenade x4 / Cryogenic Grenade x4, choose one.
            (1330u, SelectableItem, 0, 45062u, 2u), (1330u, SelectableItem, 0, 118898u, 2u),
            (1330u, SelectableItem, 0, 45446u, 4u), (1330u, SelectableItem, 0, 111048u, 4u)
        };

        /// <summary>Gates whose required mission is seeded; every other recorded gate is a GAP entry.</summary>
        public static readonly (uint Mission, uint Required, string Comment)[] Prerequisites =
        {
            (1014u, 887u, "The Defiant Ones completed"),
            (1064u, 1063u, "Incriminating Evidence completed")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "npc_mission", MissionColumns, MissionTypes,
                Rows(Missions.Select(m => new object[] { m.Id, m.Giver, m.Receiver, m.Level, 1u, GeneralCategory, false, false, m.Comment })));

            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, o.Required, o.Ordinal, o.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            InsertTyped(migrationBuilder, "npc_mission_reward", RewardColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Amount, r.Template, r.Quantity })));

            InsertTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteColumns, new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required, Completed, p.Comment })));
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_prerequisite", PrerequisiteKeyColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Prerequisites.Select(p => new object[] { p.Mission, (byte)0, p.Required })));

            DeleteTyped(migrationBuilder, "npc_mission_reward", RewardKeyColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Rewards.Select(r => new object[] { r.Mission, r.Type, r.Template })));

            DeleteTyped(migrationBuilder, "npc_mission_objective_transition", TransitionColumns, new[] { "INTEGER", "INTEGER", "INTEGER" },
                Rows(Transitions.Select(t => new object[] { t.Mission, t.Completed, t.Revealed })));

            // The client skeleton rows come back with their text and all three server flags unknown (NULL).
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective })));
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                Rows(Objectives.Select(o => new object[] { o.Mission, o.Objective, o.Text, null, null, null })));

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

        // EF Core 5 does not retain ColumnTypes through its positional InsertData overload; attach them so both
        // providers generate SQL without a target model (the WildernessHubFormingAlliances pattern).
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
