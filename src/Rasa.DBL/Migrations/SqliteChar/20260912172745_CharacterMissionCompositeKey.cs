using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class CharacterMissionCompositeKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission");

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission",
                columns: new[] { "character_id", "mission_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission");

            migrationBuilder.AlterColumn<uint>(
                name: "character_id",
                table: "character_mission",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_character_mission",
                table: "character_mission",
                column: "character_id");
        }
    }
}
