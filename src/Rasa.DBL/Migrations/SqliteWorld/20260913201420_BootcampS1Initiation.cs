using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS1Initiation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS1InitiationRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS1InitiationRows.DeleteData(migrationBuilder);
        }
    }
}
