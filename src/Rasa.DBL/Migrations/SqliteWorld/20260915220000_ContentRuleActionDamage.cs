using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The content layer can now hurt a player: ContentRuleAction.DamagePlayer needs an amount, and
    /// content_rule_action has no column for one. The boot camp's bomb blast is the recorded case - the recruit who
    /// arms it sees "-21" take off their health bar when it detonates (footage B1-049, and the 2005 text "Not
    /// yourself!" 21566), so the seeded action carries 21.
    /// </summary>
    public partial class ContentRuleActionDamage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "damage",
                table: "content_rule_action",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "damage", table: "content_rule_action");
        }
    }
}
