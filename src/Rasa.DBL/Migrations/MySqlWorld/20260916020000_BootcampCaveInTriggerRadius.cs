using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampCaveInTriggerRadius : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampCaveInTriggerRadiusRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampCaveInTriggerRadiusRows.DeleteData(migrationBuilder);
        }
    }
}
