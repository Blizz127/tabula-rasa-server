using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Widens mission 1990's two objective triggers to the width of the bridge they stand on.
    ///
    /// The markers' X,Z came from the footage's objective icons and are good to 1.5-2.0 m, but their radius was
    /// never observed: the S1 slice inferred a 4 m sphere from the marker. Live play on 2026-09-15 exposed the
    /// consequence - a recruit crossed the ceremonial bridge, completed objective 1, and then walked the rest of
    /// the 16.8 m wide span (X 380.6..397.4) past objective 2's 4 m sphere without touching it, leaving the
    /// tracker reading "Approach the Eloh Hologram" while they stood in front of the hologram itself.
    ///
    /// The objective is to approach the hologram along the bridge, and the footage shows objective 2 completing
    /// at (388.19, 14.98) on that span. The radius therefore becomes 10 m, which covers the walking surface
    /// around each marker without reaching the terrain beyond the bridge's ends. The vertical extent the areas
    /// already have (20 m half-height) keeps them working from the deck.
    ///
    /// This is a reconstruction parameter, not a recovered value: GAP-S1-TRIGGER-RADIUS records that the original
    /// radius is unknown and that 10 m is the smallest value that makes the crossing reliable in play.
    /// </summary>
    public static class BootcampObjectiveAreaRadiusRows
    {
        public const string Migration = "BootcampObjectiveAreaRadius";

        /// <summary>The two 1990 objective areas and their original inferred radius.</summary>
        public static readonly (uint AreaId, double OldRadius, double NewRadius)[] Areas =
        {
            (198600u, 4.0, 10.0),   // objective 1, mid-causeway
            (198601u, 4.0, 10.0)    // objective 2, north end of the bridge
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, _, newRadius) in Areas)
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "radius",
                    value: newRadius);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, oldRadius, _) in Areas)
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "radius",
                    value: oldRadius);
        }
    }
}
