using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Lifts Delessio, the supply crate and DeSimone onto the walkable surface, which is where the player sees
    /// them.
    ///
    /// Live report (2026-09-15): "I do not see Captain Delessio". He was there in the database - creature 198501,
    /// placement 198655, package 2560, present unconditionally - but his Y came from the *origin* of the platform
    /// static he stands on (ArchHumBaseGatesystemPlatform16Mx24M at (405, 114, 166)), and a static's origin is its
    /// base, not its top.
    ///
    /// The walkable surface now comes from the map itself: the navmesh the client's map data was built into. At
    /// each placement's XZ the navmesh answers with original-tier ground, no guesswork:
    ///
    ///     Delessio      seeded y 114.00  platform top 122.11  -8.11 m  (buried under the platform)
    ///     supply crate  seeded y 114.00  platform top 122.11  -8.11 m  (same platform, same mistake)
    ///     DeSimone      seeded y 125.57  ground       127.70  -2.13 m  (position inferred; snapped to the surface)
    ///
    /// The platform's top is 122.11, measured by probing the navmesh at several heights at that XZ: below it the
    /// query falls through to the ground beside the platform at 120.4, whose nearest point lies 1.8 m further north
    /// (z 175.06 instead of 172.8). Taking 120.4 would have looked correct - it is within 0.05 m of that lower
    /// surface - which is exactly why the measured value, not the first plausible one, was used.
    ///
    /// Every other camp placement already stood on the surface within 0.6 m, so the camp's other heights are
    /// confirmed rather than changed. The two deliberate exceptions are kept: the bomb sits 1.06 m above ground
    /// because it rests on the wreck hull, and Thrax in the gate area are within 1.0 m.
    ///
    /// PlacementHeightAuditTests keeps this honest: it re-measures every camp placement against the navmesh and
    /// fails if any drifts off the surface again.
    /// </summary>
    public static class BootcampPlacementGroundSnapRows
    {
        public const string Migration = "BootcampPlacementGroundSnap";

        /// <summary>placement id -> (old y, ground y)</summary>
        public static readonly (uint PlacementId, double OldY, double GroundY)[] Placements =
        {
            (198655u, 114.0, 122.1),   // Delessio, on the grating platform (top 122.11)
            (198651u, 114.0, 122.1),   // the 1992 supply crate, same platform
            (198657u, 125.57, 127.7)   // DeSimone (position inferred), snapped to the surface
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (placementId, _, groundY) in Placements)
                migrationBuilder.UpdateData(
                    table: "content_placement",
                    keyColumn: "id",
                    keyValue: placementId,
                    column: "pos_y",
                    value: groundY);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (placementId, oldY, _) in Placements)
                migrationBuilder.UpdateData(
                    table: "content_placement",
                    keyColumn: "id",
                    keyValue: placementId,
                    column: "pos_y",
                    value: oldY);
        }
    }
}
