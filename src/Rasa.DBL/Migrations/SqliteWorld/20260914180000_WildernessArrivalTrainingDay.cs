using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessArrivalTrainingDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessArrivalTrainingDayRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessArrivalTrainingDayRows.DeleteData(migrationBuilder);
        }
    }
}
