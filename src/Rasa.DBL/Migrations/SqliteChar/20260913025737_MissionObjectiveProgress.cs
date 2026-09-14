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

            // SQLite cannot drop columns through EF Core 5 (DropColumnOperation has no
            // SQL generation), so the pre-change_time table is rebuilt and the rows
            // carried over. MySQL keeps the plain DropColumn in its own migration.
            migrationBuilder.Sql(
                "CREATE TABLE \"character_mission_previous\" (\n" +
                "    \"character_id\" INTEGER NOT NULL,\n" +
                "    \"mission_id\" INTEGER NOT NULL,\n" +
                "    \"mission_state\" INTEGER NOT NULL,\n" +
                "    CONSTRAINT \"PK_character_mission\"\n" +
                "        PRIMARY KEY (\"character_id\", \"mission_id\")\n" +
                ");");
            migrationBuilder.Sql(
                "INSERT INTO \"character_mission_previous\"\n" +
                "    (\"character_id\", \"mission_id\", \"mission_state\")\n" +
                "    SELECT \"character_id\", \"mission_id\", \"mission_state\"\n" +
                "    FROM \"character_mission\";");
            migrationBuilder.Sql("DROP TABLE \"character_mission\";");
            migrationBuilder.Sql("ALTER TABLE \"character_mission_previous\" RENAME TO \"character_mission\";");
        }
    }
}
