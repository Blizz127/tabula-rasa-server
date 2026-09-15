using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampObjectiveAreaHeight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveAreaHeightRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveAreaHeightRows.DeleteData(migrationBuilder);
        }
    }
}
