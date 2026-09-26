using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class TooCloseForComfortLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.TooCloseForComfortLevelRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.TooCloseForComfortLevelRows.DeleteData(migrationBuilder);
    }
}
