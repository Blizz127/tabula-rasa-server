using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS5Reinforcements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS5ReinforcementsRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS5ReinforcementsRows.DeleteData(migrationBuilder);
        }
    }
}
