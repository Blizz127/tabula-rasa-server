using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampCrateLoadedRifle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampCrateLoadedRifleRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampCrateLoadedRifleRows.DeleteData(migrationBuilder);
    }
}
