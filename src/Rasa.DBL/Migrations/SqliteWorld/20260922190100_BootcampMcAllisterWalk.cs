using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Migrations.BootcampData;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class BootcampMcAllisterWalk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampMcAllisterWalkRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampMcAllisterWalkRows.DeleteData(migrationBuilder);
    }
}
