using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessCollectionDrop : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessCollectionDropRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessCollectionDropRows.DeleteData(migrationBuilder);
        }
    }
}
