using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class MissionItemDropChance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<double>(name: "drop_chance", table: "npc_mission_objective_binding",
                type: "double", nullable: false, defaultValue: 0.0);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(name: "drop_chance", table: "npc_mission_objective_binding");
    }
}
