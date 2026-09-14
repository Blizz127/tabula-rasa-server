using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampFixRogersTurnIn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampFixRogersTurnInRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampFixRogersTurnInRows.DeleteData(migrationBuilder);
        }
    }
}
