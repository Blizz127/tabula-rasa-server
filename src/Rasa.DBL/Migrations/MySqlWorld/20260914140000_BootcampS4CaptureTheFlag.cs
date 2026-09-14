using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS4CaptureTheFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS4CaptureTheFlagRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS4CaptureTheFlagRows.DeleteData(migrationBuilder);
        }
    }
}
