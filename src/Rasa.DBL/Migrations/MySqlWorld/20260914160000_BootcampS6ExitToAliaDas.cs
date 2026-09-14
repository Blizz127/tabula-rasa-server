using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS6ExitToAliaDas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS6ExitToAliaDasRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS6ExitToAliaDasRows.DeleteData(migrationBuilder);
        }
    }
}
