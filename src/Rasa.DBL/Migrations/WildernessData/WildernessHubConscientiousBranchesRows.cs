using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Corrects 1390's route and gates its two Part Two outcomes. Original-client
    /// objective/conversation IDs and pre-shutdown reward records are traced in
    /// docs/evidence/wilderness-conscientious-branches.json.
    /// </summary>
    public static class WildernessHubConscientiousBranchesRows
    {
        private static void Insert(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] rows)
        {
            builder.InsertData(table, columns, rows);
            ((InsertDataOperation)builder.Operations.Last()).ColumnTypes = types;
        }

        private static void Delete(MigrationBuilder builder, string table, string[] columns, string[] types, object[,] rows)
        {
            builder.DeleteData(table, columns, rows);
            ((DeleteDataOperation)builder.Operations.Last()).KeyColumnTypes = types;
        }

        private static void UpdateRequired(MigrationBuilder builder, bool required)
        {
            builder.UpdateData("npc_mission_objective",
                new[] { "mission_id", "objective_id" }, new object[] { 1390u, 10u },
                "is_required", required);
            var operation = (UpdateDataOperation)builder.Operations.Last();
            operation.KeyColumnTypes = new[] { "INTEGER", "INTEGER" };
            operation.ColumnTypes = new[] { "INTEGER" };
        }

        private static readonly string[] TransitionColumns =
            { "mission_id", "completed_objective_id", "revealed_objective_id" };
        private static readonly string[] TransitionTypes = { "INTEGER", "INTEGER", "INTEGER" };
        private static readonly object[,] ReplacedTransitions =
        {
            { 1390u, 2u, 4u }, { 1390u, 2u, 10u }, { 1390u, 3u, 8u },
            { 1390u, 3u, 10u }, { 1390u, 8u, 10u }
        };
        private static readonly object[,] CorrectTransitions =
        {
            { 1390u, 2u, 8u }, { 1390u, 8u, 11u }
        };
        private static readonly string[] PrerequisiteColumns =
            { "mission_id", "or_group", "required_mission_id", "required_state", "comment" };
        private static readonly string[] PrerequisiteTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        private static readonly object[,] Prerequisites =
        {
            { 1392u, (byte)0, 1390u, (byte)4, "Arrest Milpas and complete Conscientious Objector" },
            { 1393u, (byte)0, 1390u, (byte)4, "Free Milpas and complete Conscientious Objector" }
        };
        private static readonly string[] RewardColumns =
            { "id", "type", "credits", "item_template_id", "quantity" };
        private static readonly string[] RewardTypes =
            { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        private static readonly object[,] Rewards =
        {
            { 1392u, (byte)3, 8000, 0u, 0u }, { 1392u, (byte)1, 800, 0u, 0u },
            { 1393u, (byte)3, 8000, 0u, 0u }, { 1393u, (byte)1, 800, 0u, 0u }
        };

        public static void InsertData(MigrationBuilder builder)
        {
            Delete(builder, "npc_mission_objective_transition", TransitionColumns, TransitionTypes, ReplacedTransitions);
            Insert(builder, "npc_mission_objective_transition", TransitionColumns, TransitionTypes, CorrectTransitions);
            UpdateRequired(builder, false);
            Insert(builder, "npc_mission_prerequisite", PrerequisiteColumns, PrerequisiteTypes, Prerequisites);
            Insert(builder, "npc_mission_reward", RewardColumns, RewardTypes, Rewards);
        }

        public static void DeleteData(MigrationBuilder builder)
        {
            Delete(builder, "npc_mission_reward", new[] { "id", "type", "item_template_id" },
                new[] { "INTEGER", "INTEGER", "INTEGER" }, new object[,]
                {
                    { 1392u, (byte)3, 0u }, { 1392u, (byte)1, 0u },
                    { 1393u, (byte)3, 0u }, { 1393u, (byte)1, 0u }
                });
            Delete(builder, "npc_mission_prerequisite", new[] { "mission_id", "or_group", "required_mission_id" },
                new[] { "INTEGER", "INTEGER", "INTEGER" },
                new object[,] { { 1392u, (byte)0, 1390u }, { 1393u, (byte)0, 1390u } });
            UpdateRequired(builder, true);
            Delete(builder, "npc_mission_objective_transition", TransitionColumns, TransitionTypes, CorrectTransitions);
            Insert(builder, "npc_mission_objective_transition", TransitionColumns, TransitionTypes, ReplacedTransitions);
        }
    }
}
