using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The pre-shutdown Receptive Reception mission record names Too Close For Comfort as its requirement.
    /// Both missions now exist in the world seed, so enforce the recorded completion gate.
    /// </summary>
    public static class WildernessHubReceptiveGateRows
    {
        // Explicit types let EF generate the insert without a generated target model and keep
        // the SQLite/MySQL migration pair visible to the seed parity check.
        public static void InsertData(MigrationBuilder migrationBuilder) => migrationBuilder.InsertData(
            table: "npc_mission_prerequisite",
            columns: new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
            columnTypes: new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" },
            values: new object[] { 1069u, (byte)0, 1407u, (byte)4, "Too Close For Comfort" });

        public static void DeleteData(MigrationBuilder migrationBuilder) => migrationBuilder.DeleteData(
            table: "npc_mission_prerequisite",
            keyColumns: new[] { "mission_id", "or_group", "required_mission_id" },
            keyColumnTypes: new[] { "INTEGER", "INTEGER", "INTEGER" },
            keyValues: new object[] { 1069u, (byte)0, 1407u });
    }
}
