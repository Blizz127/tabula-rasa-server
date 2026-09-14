using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS2GearingUp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS2GearingUpRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS2GearingUpRows.DeleteData(migrationBuilder);
        }
    }
}
