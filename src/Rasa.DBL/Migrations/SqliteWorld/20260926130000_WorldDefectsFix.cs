using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The 2026-09-26 world-data audits: Bagby and Galloway into Treeback Camp, the Foreas Base Warnet Queen
    /// removed, three receivers off upstream's map markers, the second Whitaker retired.
    /// See <see cref="WorldDefectsFixRows"/>.
    /// </summary>
    public partial class WorldDefectsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WorldDefectsFixRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WorldDefectsFixRows.DeleteData(migrationBuilder);
    }
}
