using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampPlatformTopCorrection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampPlatformTopCorrectionRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampPlatformTopCorrectionRows.DeleteData(migrationBuilder);
        }
    }
}
