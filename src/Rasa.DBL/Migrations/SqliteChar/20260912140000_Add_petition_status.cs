using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class Add_petition_status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows were filed before there was anywhere for them to go, so they are all
            // still open - which is what the default gives them.
            migrationBuilder.AddColumn<byte>(
                name: "status",
                table: "petition",
                type: "tinyint(3)",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "resolution",
                table: "petition",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQLite cannot drop columns through EF Core 5 (DropColumnOperation has no
            // SQL generation), so the pre-status table is rebuilt and the rows carried
            // over. MySQL keeps the plain DropColumn in its own migration.
            migrationBuilder.Sql(
                "CREATE TABLE \"petition_previous\" (\n" +
                "    \"id\" integer NOT NULL CONSTRAINT \"PK_petition\" PRIMARY KEY AUTOINCREMENT,\n" +
                "    \"account_id\" integer NOT NULL,\n" +
                "    \"character_id\" integer NOT NULL,\n" +
                "    \"type\" tinyint(3) NOT NULL DEFAULT 0,\n" +
                "    \"summary\" varchar(255) NOT NULL,\n" +
                "    \"body\" text NOT NULL,\n" +
                "    \"map_context_id\" int(11) NOT NULL,\n" +
                "    \"pos_x\" double NOT NULL,\n" +
                "    \"pos_y\" double NOT NULL,\n" +
                "    \"pos_z\" double NOT NULL,\n" +
                "    \"created_at\" TEXT NOT NULL\n" +
                ");");
            migrationBuilder.Sql(
                "INSERT INTO \"petition_previous\"\n" +
                "    (\"id\", \"account_id\", \"character_id\", \"type\", \"summary\", \"body\",\n" +
                "     \"map_context_id\", \"pos_x\", \"pos_y\", \"pos_z\", \"created_at\")\n" +
                "    SELECT \"id\", \"account_id\", \"character_id\", \"type\", \"summary\", \"body\",\n" +
                "     \"map_context_id\", \"pos_x\", \"pos_y\", \"pos_z\", \"created_at\"\n" +
                "    FROM \"petition\";");
            migrationBuilder.Sql("DROP TABLE \"petition\";");
            migrationBuilder.Sql("ALTER TABLE \"petition_previous\" RENAME TO \"petition\";");
        }
    }
}
