using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class InstanceTravelRetiredRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.InstanceTravelRetiredRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.InstanceTravelRetiredRows.DeleteData(migrationBuilder);
    }
}
