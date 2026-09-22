using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="QuestWiringFixesRows"/>.</summary>
    public partial class QuestWiringFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => QuestWiringFixesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => QuestWiringFixesRows.DeleteData(migrationBuilder);
    }
}
