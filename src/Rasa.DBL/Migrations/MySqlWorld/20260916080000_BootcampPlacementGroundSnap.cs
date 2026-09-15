using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampPlacementGroundSnap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampPlacementGroundSnapRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampPlacementGroundSnapRows.DeleteData(migrationBuilder);
        }
    }
}
