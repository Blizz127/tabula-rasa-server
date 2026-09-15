using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampEscortDestinationGround : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampEscortDestinationGroundRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampEscortDestinationGroundRows.DeleteData(migrationBuilder);
        }
    }
}
