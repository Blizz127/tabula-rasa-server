using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Migrations.BootcampData;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class BootcampEquipCrateGear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampEquipCrateGearRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampEquipCrateGearRows.DeleteData(migrationBuilder);
    }
}
