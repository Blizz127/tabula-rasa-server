using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessGiverFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessGiverFixRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessGiverFixRows.DeleteData(migrationBuilder);
        }
    }
}
