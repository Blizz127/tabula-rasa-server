using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class SolisCavernsPlacement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.SolisCavernsPlacementRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.SolisCavernsPlacementRows.DeleteData(migrationBuilder);
    }
}
