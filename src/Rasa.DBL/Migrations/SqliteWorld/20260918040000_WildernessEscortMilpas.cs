using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessEscortMilpas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessEscortMilpasRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessEscortMilpasRows.DeleteData(migrationBuilder);
        }
    }
}
