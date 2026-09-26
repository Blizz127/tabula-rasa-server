using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class InstanceTravelLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.InstanceTravelLinksRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.InstanceTravelLinksRows.DeleteData(migrationBuilder);
    }
}
