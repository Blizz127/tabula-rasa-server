using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class WildernessHubReceptiveLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubReceptiveLevelRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubReceptiveLevelRows.DeleteData(migrationBuilder);
    }
}
