using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>Preserves fractional creature speeds; all former uint values are exactly representable in storage.</summary>
    public partial class CreatureFractionalMovementRates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var column in new[] { "walk_speed", "run_speed" })
                migrationBuilder.AlterColumn<double>(name: column, table: "creature", type: "double",
                    nullable: false, oldClrType: typeof(uint), oldType: "int unsigned");
        }

        // The following data migration restores McAllister to zero before this rollback.
        // Arbitrary fractional edits made after upgrading cannot survive an integer rollback.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var column in new[] { "walk_speed", "run_speed" })
                migrationBuilder.AlterColumn<uint>(name: column, table: "creature", type: "int unsigned",
                    nullable: false, oldClrType: typeof(double), oldType: "double");
        }
    }
}
