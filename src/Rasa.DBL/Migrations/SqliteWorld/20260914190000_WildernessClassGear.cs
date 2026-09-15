using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessClassGear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessClassGearRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessClassGearRows.DeleteData(migrationBuilder);
        }
    }
}
