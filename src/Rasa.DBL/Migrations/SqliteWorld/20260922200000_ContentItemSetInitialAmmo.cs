using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class ContentItemSetInitialAmmo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.AddColumn<uint>(name: "initial_ammo", table: "content_item_set", type: "INTEGER", nullable: false, defaultValue: 0u);

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DropColumn(name: "initial_ammo", table: "content_item_set");
    }
}
