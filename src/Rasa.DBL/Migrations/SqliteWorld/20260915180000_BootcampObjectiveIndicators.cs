using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampObjectiveIndicators : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveIndicatorRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampObjectiveIndicatorRows.DeleteData(migrationBuilder);
        }
    }
}
