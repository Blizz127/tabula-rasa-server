using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubKillObjective : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubKillObjectiveRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubKillObjectiveRows.DeleteData(migrationBuilder);
        }
    }
}
