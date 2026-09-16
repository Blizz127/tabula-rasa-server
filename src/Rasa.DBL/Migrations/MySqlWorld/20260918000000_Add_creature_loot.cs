using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Services.Preloader;
    using Structures.World;

    public partial class Add_creature_loot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: CreatureLootEntry.TableName,
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    creature_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_template_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    chance = table.Column<double>(type: "double", nullable: false),
                    stacksize_min = table.Column<uint>(type: "int unsigned", nullable: false),
                    stacksize_max = table.Column<uint>(type: "int unsigned", nullable: false),
                    comment = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_creature_loot", x => x.id);
                });

            // The seven rows of the original creature_type_loot that survived, on this world's Thrax soldiers.
            new CreatureLootPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: CreatureLootEntry.TableName);
        }
    }
}
