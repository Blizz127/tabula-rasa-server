using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Four NPC corrections from Ellatha's live-game NPC database.
    /// See <see cref="EllathaNpcPositionsRows"/>.
    /// </summary>
    public partial class EllathaNpcPositions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => EllathaNpcPositionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => EllathaNpcPositionsRows.DeleteData(migrationBuilder);
    }
}
