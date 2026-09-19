using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The fifty content NPCs that rendered bare, dressed in a shipped analogue set (GAP-NPC-BODY).
    /// See <see cref="ContentNpcAppearanceRows"/>.
    /// </summary>
    public partial class ContentNpcAppearance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => ContentNpcAppearanceRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => ContentNpcAppearanceRows.DeleteData(migrationBuilder);
    }
}
