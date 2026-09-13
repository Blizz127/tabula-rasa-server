using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class MissionObjectiveDefinitions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "npc_mission_objective",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    ordinal = table.Column<uint>(type: "INTEGER", nullable: false),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    revealed_on_accept = table.Column<bool>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "varchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective", x => new { x.mission_id, x.objective_id });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_conversation",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    npc_package_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    player_flag_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_conversation", x => new { x.mission_id, x.objective_id, x.npc_package_id, x.player_flag_id });
                });

            migrationBuilder.CreateTable(
                name: "npc_mission_objective_transition",
                columns: table => new
                {
                    mission_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    completed_objective_id = table.Column<uint>(type: "INTEGER", nullable: false),
                    revealed_objective_id = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_mission_objective_transition", x => new { x.mission_id, x.completed_objective_id, x.revealed_objective_id });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "npc_mission_objective");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_conversation");

            migrationBuilder.DropTable(
                name: "npc_mission_objective_transition");
        }
    }
}
