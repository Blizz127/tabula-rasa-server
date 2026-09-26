using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Migrations.BootcampData;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampMcAllisterWalk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampMcAllisterWalkRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampMcAllisterWalkRows.DeleteData(migrationBuilder);
    }
}
