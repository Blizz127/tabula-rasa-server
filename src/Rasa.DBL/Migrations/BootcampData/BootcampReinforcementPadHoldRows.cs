using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// B2-012 shows the reinforcements still standing on the pad at 98.333 s,
    /// 11.533 s after objective 1995/1 completed. ObjectiveCompleted actions
    /// execute immediately, so the earlier walk-off rules cannot represent
    /// that observed interval. The later departure, if any, is unverified.
    /// Preserve the destroyed-wreck condition and arrival placements.
    /// </summary>
    public static class BootcampReinforcementPadHoldRows
    {
        private const uint OriginalRule = 1985015u;
        private const uint RetryRule = 1985017u;
        private const uint UnsupportedDestination = 19854u;

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            foreach (var rule in new[] { OriginalRule, RetryRule })
            {
                for (byte sequence = 0; sequence < 3; sequence++)
                {
                    migrationBuilder.DeleteData("content_rule_action", new[] { "rule_id", "sequence" },
                        new object[] { rule, sequence });
                    ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                        .KeyColumnTypes = new[] { "INTEGER", "INTEGER" };
                }
                migrationBuilder.DeleteData("content_rule", "id", rule);
                ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                    .KeyColumnTypes = new[] { "INTEGER" };
            }

            migrationBuilder.DeleteData("content_location", "id", UnsupportedDestination);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                .KeyColumnTypes = new[] { "INTEGER" };
        }

        public static void Revert(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                "content_location",
                new[] { "id", "purpose", "map_context_id", "pos_x", "pos_y", "pos_z", "rotation", "comment" },
                new object[] { UnsupportedDestination, (byte)3, 1985u, -215.0, 100.0, -62.0, 0.0,
                    "reinforcement walk-off destination (inferred)" });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                .ColumnTypes = new[] { "INTEGER", "INTEGER", "INTEGER", "DOUBLE", "DOUBLE", "DOUBLE", "DOUBLE", "TEXT" };

            migrationBuilder.InsertData(
                "content_rule",
                new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                new object[,]
                {
                    { OriginalRule, 1985u, (byte)3, 1995u, 1u, 0u, 0u, 0u, 0u, "1995/1 -> reinforcements leave the pad" },
                    { RetryRule, 1985u, (byte)3, 2005u, 1u, 0u, 0u, 0u, 0u, "2005/1 -> reinforcements leave the pad (inferred retry)" }
                });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                .ColumnTypes = new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT" };

            foreach (var rule in new[] { OriginalRule, RetryRule })
            {
                byte sequence = 0;
                foreach (var placement in new[] { 198681u, 198682u, 198680u })
                {
                    migrationBuilder.InsertData(
                        "content_rule_action",
                        new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                        new object[] { rule, sequence++, (byte)14, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0,
                            0u, placement, 0u, string.Empty, 0, UnsupportedDestination, 0u,
                            rule == RetryRule ? "reinforcement leaves the pad after retry (inferred)" : "reinforcement leaves the pad" });
                    ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1])
                        .ColumnTypes = new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT", "INTEGER", "INTEGER", "INTEGER", "TEXT" };
                }
            }
        }
    }
}
