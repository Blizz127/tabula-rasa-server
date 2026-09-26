using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// TaRapedia's pre-shutdown Too Close For Comfort page records level 4.
    /// The client mission tables contain no level; final-live server level remains unverified.
    /// </summary>
    public static class TooCloseForComfortLevelRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => SetLevel(migrationBuilder, 4u);
        public static void DeleteData(MigrationBuilder migrationBuilder) => SetLevel(migrationBuilder, 5u);

        private static void SetLevel(MigrationBuilder migrationBuilder, uint level)
        {
            migrationBuilder.UpdateData(table: "npc_mission", keyColumn: "id", keyValue: 1407u,
                column: "level", value: level);
            var operation = (UpdateDataOperation)migrationBuilder.Operations.Last();
            operation.KeyColumnTypes = new[] { "INTEGER" };
            operation.ColumnTypes = new[] { "INTEGER" };
        }
    }
}
