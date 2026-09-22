using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The content layer learns to make an NPC speak, and the camp's first spoken line: Major McAllister's
    /// bark 852. See <see cref="ContentRuleBarkRows"/>.
    /// </summary>
    public partial class ContentRuleBark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => ContentRuleBarkRows.InsertData(migrationBuilder, "int unsigned");

        protected override void Down(MigrationBuilder migrationBuilder)
            => ContentRuleBarkRows.DeleteData(migrationBuilder);
    }
}
