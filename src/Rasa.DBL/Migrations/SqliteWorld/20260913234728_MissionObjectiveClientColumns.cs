using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class MissionObjectiveClientColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_mission_objective_conversation",
                table: "npc_mission_objective_conversation");

            migrationBuilder.AddColumn<uint>(
                name: "convo_type",
                table: "npc_mission_objective_conversation",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<bool>(
                name: "revealed_on_accept",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<uint>(
                name: "ordinal",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "is_required",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "npc_mission_objective",
                type: "varchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_mission_objective_conversation",
                table: "npc_mission_objective_conversation",
                columns: new[] { "mission_id", "objective_id", "npc_package_id", "player_flag_id", "convo_type" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_npc_mission_objective_conversation",
                table: "npc_mission_objective_conversation");

            migrationBuilder.DropColumn(
                name: "convo_type",
                table: "npc_mission_objective_conversation");

            migrationBuilder.AlterColumn<bool>(
                name: "revealed_on_accept",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "ordinal",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(uint),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_required",
                table: "npc_mission_objective",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "npc_mission_objective",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_npc_mission_objective_conversation",
                table: "npc_mission_objective_conversation",
                columns: new[] { "mission_id", "objective_id", "npc_package_id", "player_flag_id" });
        }
    }
}
