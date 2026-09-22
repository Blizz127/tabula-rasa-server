using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="CodexPlacementFixesRows"/>.</summary>
    public partial class CodexPlacementFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => CodexPlacementFixesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => CodexPlacementFixesRows.DeleteData(migrationBuilder);
    }
}
