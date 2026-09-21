using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The missions whose giver was not the character their briefing names, and Colonel Bruce with them.
    /// See <see cref="QuestGiverBriefingFixRows"/>.
    /// </summary>
    public partial class QuestGiverBriefingFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => QuestGiverBriefingFixRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => QuestGiverBriefingFixRows.DeleteData(migrationBuilder);
    }
}
