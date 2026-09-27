using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class TarapediaBossLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.TarapediaBossLevelsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.TarapediaBossLevelsRows.DeleteData(migrationBuilder);
    }
}
