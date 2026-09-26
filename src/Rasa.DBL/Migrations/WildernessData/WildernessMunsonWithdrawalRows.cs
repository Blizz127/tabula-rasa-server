using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: Dr. Munson's Mighty Miasma (767) is no longer offered, because the final live game did not offer it.
    ///
    /// What: WildernessCollectionDrop (2026-09-17) seeded 767 at Dr. Munson (109) as a count of six Bane Miasma (88)
    /// kills with 4,000 XP and 600 credits. This migration removes that definition - the npc_mission row, the kill
    /// binding and its counter, and both reward rows - and puts the client's objective row (767/2 "Get 6 Miasma Goo
    /// Samples.") back to the NULL-flag skeleton MissionClientObjectiveSkeleton ships. Nothing else used those rows:
    /// Dr. Munson, the Bane Miasma and the client objective text stay in the world.
    ///
    /// Why (tier original): the official Deployment 11 live notes, "Deployment 11.6: 8/15/2008" ("Deployment 11 is on
    /// Live!"), Maps and Missions > Concordia Wilderness: "'Xenobiologist' Missions - The three missions from 'Dr.
    /// Munson' to gather biological samples from Boargar, Treelurkers and Miasmas are no longer available, As Dr. Munson
    /// has more samples than he could ever use! (Note: they can still be finished if a player already has one in
    /// progress.)" - and "Mission: Predatory: This mission has been permanently disabled." Capture:
    /// rgtr.com/news/patch_notes/deployment_116_8152008.html, SHA-256 3dfe1626...71dd7d8
    /// (research/20260926-collection-missions/work/patchnotes/d116.html, the same bytes docs/bootcamp-client-evidence.md
    /// cites). The three Munson missions are 751 Boargar Acquisition, 780 Treelurker Samples and 767 Mighty Miasma; 769
    /// is Predatory. 751, 780 and 769 were never seeded and must stay that way.
    ///
    /// Not modelled (GAP-D11-WITHDRAWN-IN-PROGRESS): the notes let a player who already had one of Munson's missions
    /// finish it, and fail an in-log Predatory automatically. No character of this server can hold a 2008 log entry.
    /// </summary>
    public static class WildernessMunsonWithdrawalRows
    {
        public const string Migration = "WildernessMunsonWithdrawal";

        public const uint MightyMiasma = 767u;
        public const uint Objective = 2u;
        public const uint DrMunson = 109u;
        public const uint BaneMiasma = 88u;

        private const byte Credits = 1;
        private const byte Experience = 3;
        private const byte KillBinding = 6;
        private const string ObjectiveText = "Get 6 Miasma Goo Samples.";

        private static readonly string[] ObjectiveColumns =
            { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" };
        private static readonly string[] ObjectiveTypes = { "INTEGER", "INTEGER", "varchar(100)", "INTEGER", "INTEGER", "INTEGER" };
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
        private static readonly string[] CounterColumns = { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" };
        private static readonly string[] ThreeKeyTypes = { "INTEGER", "INTEGER", "INTEGER" };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_reward", new[] { "id", "type", "item_template_id" }, ThreeKeyTypes,
                new object[,] { { MightyMiasma, Experience, 0u }, { MightyMiasma, Credits, 0u } });
            DeleteTyped(migrationBuilder, "npc_mission_objective_counter", new[] { "mission_id", "objective_id", "counter_id" }, ThreeKeyTypes,
                new object[,] { { MightyMiasma, Objective, (byte)0 } });
            DeleteTyped(migrationBuilder, "npc_mission_objective_binding", new[] { "mission_id", "objective_id", "binding_id" }, ThreeKeyTypes,
                new object[,] { { MightyMiasma, Objective, (byte)0 } });

            // The client's objective row goes back to the skeleton: text kept, flags NULL.
            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                new object[,] { { MightyMiasma, Objective } });
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                new object[,] { { MightyMiasma, Objective, ObjectiveText, null, null, null } });

            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" }, new object[,] { { MightyMiasma } });
        }

        /// <summary>Puts back exactly what WildernessCollectionDrop seeded for 767.</summary>
        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "npc_mission",
                new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                new object[,] { { MightyMiasma, DrMunson, DrMunson, 5u, (byte)1, 10000001u, false, false, "Mighty Miasma (Wilderness)" } });

            DeleteTyped(migrationBuilder, "npc_mission_objective", new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" },
                new object[,] { { MightyMiasma, Objective } });
            InsertTyped(migrationBuilder, "npc_mission_objective", ObjectiveColumns, ObjectiveTypes,
                new object[,] { { MightyMiasma, Objective, ObjectiveText, true, 1u, true } });

            InsertTyped(migrationBuilder, "npc_mission_objective_binding", BindingColumns, BindingTypes,
                new object[,]
                {
                    { MightyMiasma, Objective, (byte)0, KillBinding, 0u, 0u, BaneMiasma, 0u, false, (byte)0, 0u, 0.0, 0u, 0u, (byte)0,
                      "767/2 kill creature 88" }
                });
            InsertTyped(migrationBuilder, "npc_mission_objective_counter", CounterColumns,
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                new object[,] { { MightyMiasma, Objective, (byte)0, 0, 6 } });
            InsertTyped(migrationBuilder, "npc_mission_reward", new[] { "id", "type", "credits", "item_template_id", "quantity" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                new object[,] { { MightyMiasma, Experience, 4000, 0u, 0u }, { MightyMiasma, Credits, 600, 0u, 0u } });
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
