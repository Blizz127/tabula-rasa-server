using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Four positions and eleven NPCs the codex-tr.net archive recovers, once a second source agrees.
    /// Every Y in it is provisional until the navmesh is probed. See <see cref="CodexNpcCorrectionsRows"/>.
    /// </summary>
    public partial class CodexNpcCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => CodexNpcCorrectionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => CodexNpcCorrectionsRows.DeleteData(migrationBuilder);
    }
}
