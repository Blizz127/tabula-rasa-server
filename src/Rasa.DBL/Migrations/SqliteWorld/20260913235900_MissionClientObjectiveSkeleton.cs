using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.MissionClientData;

    public partial class MissionClientObjectiveSkeleton : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MissionClientObjectiveSkeletonRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MissionClientObjectiveSkeletonRows.DeleteData(migrationBuilder);
        }
    }
}
