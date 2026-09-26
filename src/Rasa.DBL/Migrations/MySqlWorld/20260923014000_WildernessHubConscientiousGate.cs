using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class WildernessHubConscientiousGate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubConscientiousGateRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessHubConscientiousGateRows.DeleteData(migrationBuilder);
    }
}
