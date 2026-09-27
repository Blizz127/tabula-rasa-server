using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class LogosGiveNegativeSwap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.LogosGiveNegativeSwapRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.LogosGiveNegativeSwapRows.DeleteData(migrationBuilder);
    }
}
