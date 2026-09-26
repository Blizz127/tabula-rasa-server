using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    // Footage 7Lrst9SG3pk A3-035/040/046/047: Recruit clothes and Pistol
    // display Not Tradeable / Not Sellable. See evidence/new-character-loadout.json.
    public partial class RecruitLoadoutFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE itemtemplate SET has_sellable_flag = 0, not_tradable_flag = 1
                WHERE id IN (122854, 122855, 122856, 122875)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE itemtemplate SET has_sellable_flag = 1, not_tradable_flag = 0
                WHERE id IN (122854, 122855, 122856, 122875)");
        }
    }
}
