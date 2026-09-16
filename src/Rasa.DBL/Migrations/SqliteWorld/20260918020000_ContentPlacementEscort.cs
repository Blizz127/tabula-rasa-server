using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Structures.World;

    /// <summary>
    /// The escort mechanic's column: which mission a placement's creature walks with the player on. 0 is not an
    /// escort, so every existing placement is unchanged.
    /// </summary>
    public partial class ContentPlacementEscort : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "escort_mission_id",
                table: ContentPlacementEntry.TableName,
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "escort_mission_id",
                table: ContentPlacementEntry.TableName);
        }
    }
}
