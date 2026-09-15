using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Lifts the scripted escort destination onto the walkable surface.
    ///
    /// BootcampScriptedMoves added it this morning as "the crate placement's own position minus a few metres of
    /// approach", y = 114.0 - which was the crate's own Y, itself the platform static's origin rather than its top.
    /// The whole-world position audit (2026-09-15) caught the consequence: at (390, 172) the walkable surface is
    /// 120.75, so the destination was 6.75 m under the ground McAllister walks on, and he would have been sent into
    /// the platform's base.
    ///
    /// 120.75 is the navmesh surface at that XZ. The point is west of the platform (whose footprint starts at
    /// x 397), so this is the ground beside it, not the platform top.
    ///
    /// The audit rule this satisfies is recorded in the manifest: a location a creature is sent to must be a place
    /// a body can walk, so placements, locations, areas and markers must sit within 2 m of the surface, while Logos
    /// shrines and creature spawns (original data) are only required not to be buried.
    /// </summary>
    public static class BootcampEscortDestinationGroundRows
    {
        public const string Migration = "BootcampEscortDestinationGround";

        public const uint EscortDestination = 19853u;
        public const double SeededY = 114.0;
        public const double SurfaceY = 120.75;

        public static void InsertData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(
                table: "content_location",
                keyColumn: "id",
                keyValue: EscortDestination,
                column: "pos_y",
                value: SurfaceY);

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(
                table: "content_location",
                keyColumn: "id",
                keyValue: EscortDestination,
                column: "pos_y",
                value: SeededY);
    }
}
