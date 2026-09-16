using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubConscientiousObjector : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubConscientiousObjectorRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubConscientiousObjectorRows.DeleteData(migrationBuilder);
        }
    }
}
