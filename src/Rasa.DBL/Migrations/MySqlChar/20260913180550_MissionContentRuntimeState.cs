using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class MissionContentRuntimeState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "timer_anchor_ms",
                table: "character_mission_objective",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "timer_disarmed",
                table: "character_mission_objective",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "timer_remaining_ms",
                table: "character_mission_objective",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "character_content_fact",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map_context_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    fact_key = table.Column<string>(type: "varchar(64)", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    change_time = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_content_fact", x => new { x.character_id, x.map_context_id, x.fact_key });
                });

            migrationBuilder.CreateTable(
                name: "character_mission_objective_counter",
                columns: table => new
                {
                    character_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    mission_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    objective_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    counter_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mission_objective_counter", x => new { x.character_id, x.mission_id, x.objective_id, x.counter_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_content_fact");

            migrationBuilder.DropTable(
                name: "character_mission_objective_counter");

            migrationBuilder.DropColumn(
                name: "timer_anchor_ms",
                table: "character_mission_objective");

            migrationBuilder.DropColumn(
                name: "timer_disarmed",
                table: "character_mission_objective");

            migrationBuilder.DropColumn(
                name: "timer_remaining_ms",
                table: "character_mission_objective");
        }
    }
}
