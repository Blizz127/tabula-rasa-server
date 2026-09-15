using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubReceptiveReception : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubReceptiveReceptionRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubReceptiveReceptionRows.DeleteData(migrationBuilder);
        }
    }
}
