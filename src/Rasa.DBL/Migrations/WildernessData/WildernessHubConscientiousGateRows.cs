using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Apirka offers Conscientious Objector after Forming Alliances. See
    /// docs/evidence/wilderness-conscientious-gate.json for the historical source
    /// and the final-live verification gap.
    /// </summary>
    public static class WildernessHubConscientiousGateRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData("npc_mission_prerequisite",
                new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                new object[] { 1390u, (byte)0, 479u, (byte)4, "Forming Alliances completed" });
            ((InsertDataOperation)migrationBuilder.Operations.Last()).ColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "varchar(50)" };
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("npc_mission_prerequisite",
                new[] { "mission_id", "or_group", "required_mission_id" },
                new object[] { 1390u, (byte)0, 479u });
            ((DeleteDataOperation)migrationBuilder.Operations.Last()).KeyColumnTypes =
                new[] { "INTEGER", "INTEGER", "INTEGER" };
        }
    }
}
