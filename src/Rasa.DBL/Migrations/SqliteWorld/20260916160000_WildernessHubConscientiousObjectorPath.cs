using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubConscientiousObjectorPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubConscientiousObjectorPathRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubConscientiousObjectorPathRows.DeleteData(migrationBuilder);
        }
    }
}
