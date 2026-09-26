using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class LiaisonLogosMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.LiaisonLogosMissionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.LiaisonLogosMissionsRows.DeleteData(migrationBuilder);
    }
}
