using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class TordenConversationMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.TordenConversationMissionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.TordenConversationMissionsRows.DeleteData(migrationBuilder);
    }
}
