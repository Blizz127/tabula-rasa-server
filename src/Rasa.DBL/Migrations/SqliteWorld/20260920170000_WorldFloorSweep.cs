using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Every body in the world put back on its floor, spawn pools included.
    /// See <see cref="WorldFloorSweepRows"/>.
    /// </summary>
    public partial class WorldFloorSweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WorldFloorSweepRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WorldFloorSweepRows.DeleteData(migrationBuilder);
    }
}
