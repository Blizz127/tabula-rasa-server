using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class BootcampCampGunnerCompanion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampCampGunnerCompanionRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampCampGunnerCompanionRows.DeleteData(migrationBuilder);
    }
}
