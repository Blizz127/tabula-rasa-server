using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class MissionObjectiveProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "change_time",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "character_mission_objective",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    status = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_objective", x => new { x.character_id, x.mission_id, x.objective_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_mission_objective");

            migrationBuilder.DropColumn(
                name: "change_time",
                table: "character_mission");
        }
    }
}
