using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class BootcampRifleMelee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampRifleMeleeRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampRifleMeleeRows.DeleteData(migrationBuilder);
    }
}
