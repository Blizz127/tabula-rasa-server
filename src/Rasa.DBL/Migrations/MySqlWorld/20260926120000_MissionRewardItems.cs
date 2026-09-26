using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class MissionRewardItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionRewardItemsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionRewardItemsRows.DeleteData(migrationBuilder);
    }
}
