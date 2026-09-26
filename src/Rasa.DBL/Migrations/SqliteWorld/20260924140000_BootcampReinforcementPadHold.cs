using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class BootcampReinforcementPadHold : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampReinforcementPadHoldRows.Apply(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampReinforcementPadHoldRows.Revert(migrationBuilder);
    }
}
