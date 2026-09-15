using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampRemainingTriggerHeight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampRemainingTriggerHeightRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampRemainingTriggerHeightRows.DeleteData(migrationBuilder);
        }
    }
}
