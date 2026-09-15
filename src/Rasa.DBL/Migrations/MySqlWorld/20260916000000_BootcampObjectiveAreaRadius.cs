using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampObjectiveAreaRadius : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveAreaRadiusRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveAreaRadiusRows.DeleteData(migrationBuilder);
        }
    }
}
