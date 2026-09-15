using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampAreaVerticalExtent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampAreaVerticalExtentRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampAreaVerticalExtentRows.DeleteData(migrationBuilder);
        }
    }
}
