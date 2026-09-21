using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="TarapediaNpcPositionsRows"/>.</summary>
    public partial class TarapediaNpcPositions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaNpcPositionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaNpcPositionsRows.DeleteData(migrationBuilder);
    }
}
