using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Mission 2005 repeats 1995's bomb objective after failure. Both objectives
    /// destroy the same wreck and reveal the same reinforcements. The original
    /// walk-off is observed only on the 1995 path (S5P-05); applying that walk
    /// to the retry is an inferred reconstruction. The destination remains the
    /// estimated point from BootcampScriptedMoves (19854, +/-5 m).
    /// </summary>
    public static class BootcampRetryReinforcementWalkRows
    {
        private const uint Rule = 1985017u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[] { Rule, 1985u, (byte)3, 2005u, 1u, 0u, 0u, 0u, 0u, "2005/1 -> reinforcements leave the pad (inferred retry)" });
            ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT" };

            var placements = new[] { 198681u, 198682u, 198680u };
            for (byte sequence = 0; sequence < placements.Length; sequence++)
            {
                migrationBuilder.InsertData(
                    table: "content_rule_action",
                    columns: new[] { "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id", "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id", "fact_key", "fact_value", "location_id", "audio_set_id", "comment" },
                    values: new object[] { Rule, sequence, (byte)14, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u, placements[sequence], 0u, string.Empty, 0, 19854u, 0u, "reinforcement leaves the pad after retry (inferred)" });
                ((InsertDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).ColumnTypes =
                    new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT", "INTEGER", "INTEGER", "INTEGER", "TEXT" };
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            for (byte sequence = 0; sequence < 3; sequence++)
            {
                migrationBuilder.DeleteData("content_rule_action", new[] { "rule_id", "sequence" }, new object[] { Rule, sequence });
                ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER", "INTEGER" };
            }
            migrationBuilder.DeleteData("content_rule", "id", Rule);
            ((DeleteDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1]).KeyColumnTypes = new[] { "INTEGER" };
        }
    }
}
