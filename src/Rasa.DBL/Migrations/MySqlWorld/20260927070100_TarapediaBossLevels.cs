using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class TarapediaBossLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.TarapediaBossLevelsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.TarapediaBossLevelsRows.DeleteData(migrationBuilder);
    }
}
