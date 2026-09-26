using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class LocalTeleporterGraveyards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.LocalTeleporterGraveyardsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.LocalTeleporterGraveyardsRows.DeleteData(migrationBuilder);
    }
}
