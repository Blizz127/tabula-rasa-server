using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class MissionContextSquadInstancing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionContextSquadInstancingRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionContextSquadInstancingRows.DeleteData(migrationBuilder);
    }
}
