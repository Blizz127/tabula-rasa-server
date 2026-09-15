using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Corrects Delessio and the supply crate onto the platform's top surface.
    ///
    /// BootcampPlacementGroundSnap lifted them from 114 to 120.4, which was the *first* surface the navmesh
    /// offered at that XZ - the ground beside the platform, whose nearest walkable point lies 1.8 m further north
    /// (z 175.06 instead of 172.8). Probing the same XZ at several heights showed the two layers, and the platform
    /// top is 122.11. Both placements were already applied as 120.4 on the live world, so this migration carries
    /// the correction rather than rewriting history.
    ///
    /// DeSimone's 127.7 was measured against the only surface there and is unchanged.
    /// </summary>
    public static class BootcampPlatformTopCorrectionRows
    {
        public const string Migration = "BootcampPlatformTopCorrection";

        public const double LowerSurface = 120.4;
        public const double PlatformTop = 122.1;

        /// <summary>The two placements on the grating platform.</summary>
        public static readonly uint[] Placements = { 198655u, 198651u };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var placementId in Placements)
                migrationBuilder.UpdateData(
                    table: "content_placement",
                    keyColumn: "id",
                    keyValue: placementId,
                    column: "pos_y",
                    value: PlatformTop);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var placementId in Placements)
                migrationBuilder.UpdateData(
                    table: "content_placement",
                    keyColumn: "id",
                    keyValue: placementId,
                    column: "pos_y",
                    value: LowerSurface);
        }
    }
}
