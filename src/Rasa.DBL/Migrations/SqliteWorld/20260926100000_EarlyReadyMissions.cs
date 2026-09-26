using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class EarlyReadyMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.EarlyReadyMissionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.EarlyReadyMissionsRows.DeleteData(migrationBuilder);
    }
}
