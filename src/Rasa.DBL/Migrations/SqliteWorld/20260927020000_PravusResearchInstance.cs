using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class PravusResearchInstance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.PravusResearchInstanceRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.PravusResearchInstanceRows.DeleteData(migrationBuilder);
    }
}
