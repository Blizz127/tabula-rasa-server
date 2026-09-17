using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The fourteen NPCs and objects that were not standing on the ground - seven buried, the worst by 1.66 m,
    /// and seven floating - put on the floor the original server's own creature spawns define. The measurement,
    /// the calibration behind it and the one row deliberately left off the floor are in
    /// <see cref="WorldPlacementFloorSnapRows"/>.
    /// </summary>
    public partial class WorldPlacementFloorSnap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WorldPlacementFloorSnapRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WorldPlacementFloorSnapRows.DeleteData(migrationBuilder);
    }
}
