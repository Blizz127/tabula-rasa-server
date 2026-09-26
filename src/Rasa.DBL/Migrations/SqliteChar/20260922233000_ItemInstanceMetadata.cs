using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
{
    public partial class ItemInstanceMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NULL keeps existing items on their template defaults; no modules are seeded.
            migrationBuilder.AddColumn<string>("loot_modules", "items", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<bool>("tradable_override", "items", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<bool>("sellable_override", "items", type: "INTEGER", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("loot_modules", "items");
            migrationBuilder.DropColumn("tradable_override", "items");
            migrationBuilder.DropColumn("sellable_override", "items");
        }
    }
}
