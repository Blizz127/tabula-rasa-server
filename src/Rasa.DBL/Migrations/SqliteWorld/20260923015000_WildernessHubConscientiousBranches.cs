using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class WildernessHubConscientiousBranches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubConscientiousBranchesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubConscientiousBranchesRows.DeleteData(migrationBuilder);
    }
}
