using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Forming Alliances (479): original objective and item class; historical Apirka chain and
    /// rewards; explicitly estimated drop chance, template and vest counterpart. See
    /// docs/evidence/wilderness-forming-alliances-reconstruction.json for each field's tier.
    /// </summary>
    public static class WildernessHubFormingAlliancesRows
    {
        public const string Migration = "WildernessHubFormingAlliances";
        public const uint Mission = 479u;
        public const uint Apirka = 43u;
        public const uint HeartTemplate = 2285u;
        public const uint VestTemplate = 13738u;
        public const double HeartDropChance = 50.0;

        // EF Core 5 does not retain ColumnTypes through its positional InsertData overload.
        // Attach the types to the operation so both providers generate SQL without relying
        // on a target model (the migration shipped without one; its Designer was added on recovery).
        private static void InsertTyped(MigrationBuilder builder, string table, string[] columns,
            string[] types, object[] values)
        {
            builder.InsertData(table, columns, values);
            ((InsertDataOperation)builder.Operations.Last()).ColumnTypes = types;
        }

        private static void InsertTyped(MigrationBuilder builder, string table, string[] columns,
            string[] types, object[,] values)
        {
            builder.InsertData(table, columns, values);
            ((InsertDataOperation)builder.Operations.Last()).ColumnTypes = types;
        }

        private static void DeleteTyped(MigrationBuilder builder, string table, string[] columns,
            string[] types, object[] values)
        {
            builder.DeleteData(table, columns, values);
            ((DeleteDataOperation)builder.Operations.Last()).KeyColumnTypes = types;
        }

        private static void DeleteTyped(MigrationBuilder builder, string table, string[] columns,
            string[] types, object[,] values)
        {
            builder.DeleteData(table, columns, values);
            ((DeleteDataOperation)builder.Operations.Last()).KeyColumnTypes = types;
        }

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            InsertTyped(migrationBuilder, "npc_mission",
                new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(96)" },
                new object[] { Mission, Apirka, Apirka, 5u, 1u, 10000001u, false, false, "Forming Alliances (estimated drop and vest)" });

            DeleteTyped(migrationBuilder, "npc_mission_objective",
                new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" }, new object[] { Mission, 1u });
            InsertTyped(migrationBuilder, "npc_mission_objective",
                new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                new[] { "INTEGER", "INTEGER", "varchar(150)", "INTEGER", "INTEGER", "INTEGER" },
                new object[] { Mission, 1u, "Collect twelve Thrax hearts", true, 1u, true });

            InsertTyped(migrationBuilder, "npc_mission_prerequisite",
                new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                new object[] { Mission, (byte)0, 1069u, (byte)4, "Receptive Reception completed" });
            InsertTyped(migrationBuilder, "npc_mission_prerequisite",
                new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                new object[] { 427u, (byte)0, Mission, (byte)4, "Forming Alliances completed" });

            InsertTyped(migrationBuilder, "npc_mission_objective_counter",
                new[] { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                new object[] { Mission, 1u, (byte)0, 0, 12 });
            InsertTyped(migrationBuilder, "npc_mission_objective_binding",
                new[] { "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id", "action_id", "destroying_hit_only", "equip_match", "item_template_id", "drop_chance", "item_set_id", "target_state", "counter_id", "comment" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "double", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
                new object[] { Mission, 1u, (byte)0, (byte)9, 0u, 0u, 3u, 0u, false, (byte)0, HeartTemplate, HeartDropChance, 0u, 0u, (byte)0, "Thrax Soldier heart, estimated 50%" });

            // The reported armor value is observed, while template 13738 is the nearest
            // original level-range/class counterpart. The Luminar module tuple remains a gap.
            InsertTyped(migrationBuilder, "itemtemplate_armor", new[] { "id", "armor_value" },
                new[] { "INTEGER", "INTEGER" },
                new object[] { VestTemplate, 154u });
            InsertTyped(migrationBuilder, "npc_mission_reward",
                new[] { "id", "type", "credits", "item_template_id", "quantity" },
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" },
                new object[,]
                {
                    { Mission, (byte)3, 4000, 0u, 0u },
                    { Mission, (byte)1, 400, 0u, 0u },
                    { Mission, (byte)4, 0, VestTemplate, 1u }
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            DeleteTyped(migrationBuilder, "npc_mission_reward",
                new[] { "id", "type", "item_template_id" },
                new[] { "INTEGER", "INTEGER", "INTEGER" },
                new object[,]
                {
                    { Mission, (byte)3, 0u }, { Mission, (byte)1, 0u }, { Mission, (byte)4, VestTemplate }
                });
            DeleteTyped(migrationBuilder, "itemtemplate_armor", new[] { "id" }, new[] { "INTEGER" }, new object[] { VestTemplate });
            DeleteTyped(migrationBuilder, "npc_mission_objective_binding",
                new[] { "mission_id", "objective_id", "binding_id" }, new[] { "INTEGER", "INTEGER", "INTEGER" }, new object[] { Mission, 1u, (byte)0 });
            DeleteTyped(migrationBuilder, "npc_mission_objective_counter",
                new[] { "mission_id", "objective_id", "counter_id" }, new[] { "INTEGER", "INTEGER", "INTEGER" }, new object[] { Mission, 1u, (byte)0 });
            DeleteTyped(migrationBuilder, "npc_mission_prerequisite",
                new[] { "mission_id", "or_group", "required_mission_id" },
                new[] { "INTEGER", "INTEGER", "INTEGER" },
                new object[,] { { Mission, (byte)0, 1069u }, { 427u, (byte)0, Mission } });
            DeleteTyped(migrationBuilder, "npc_mission_objective",
                new[] { "mission_id", "objective_id" }, new[] { "INTEGER", "INTEGER" }, new object[] { Mission, 1u });
            InsertTyped(migrationBuilder, "npc_mission_objective",
                new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                new[] { "INTEGER", "INTEGER", "varchar(150)", "INTEGER", "INTEGER", "INTEGER" },
                new object[] { Mission, 1u, "Collect twelve Thrax hearts", null, null, null });
            DeleteTyped(migrationBuilder, "npc_mission", new[] { "id" }, new[] { "INTEGER" }, new object[] { Mission });
        }
    }
}
