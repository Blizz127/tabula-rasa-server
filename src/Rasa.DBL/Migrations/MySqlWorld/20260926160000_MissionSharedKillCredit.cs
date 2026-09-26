using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// A kill binding may credit every character on the map channel with the objective active, whoever kills
    /// the creature (npc_mission_objective_binding.shared_kill_credit, default off). OfficialNotesCorrections
    /// turns it on for 682/3 Childhood's End, which the official live notes 1.6 (2008-03-26) and D8
    /// (2008-05-19) describe; every other binding keeps killer-only credit.
    /// </summary>
    public partial class MissionSharedKillCredit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.AddColumn<bool>(name: "shared_kill_credit", table: "npc_mission_objective_binding",
                type: "tinyint(1)", nullable: false, defaultValue: false);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropColumn(name: "shared_kill_credit", table: "npc_mission_objective_binding");
    }
}
