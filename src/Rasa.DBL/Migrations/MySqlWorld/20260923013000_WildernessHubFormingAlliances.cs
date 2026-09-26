using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class WildernessHubFormingAlliances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubFormingAlliancesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubFormingAlliancesRows.DeleteData(migrationBuilder);
    }
}
