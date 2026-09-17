using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class MissionAreaLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MissionAreaLinksRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MissionAreaLinksRows.DeleteData(migrationBuilder);
        }
    }
}
