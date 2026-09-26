using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampConradCorpsePlacement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampConradCorpsePlacementRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampConradCorpsePlacementRows.DeleteData(migrationBuilder);
    }
}
