using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampScriptedMoves : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampScriptedMoveRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampScriptedMoveRows.DeleteData(migrationBuilder);
        }
    }
}
