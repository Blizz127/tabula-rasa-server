using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Puts the two remaining camp triggers on the player's level, so their height can never block them the way
    /// S1's did.
    ///
    /// The S1 lesson (2026-09-15): a trigger whose Y came from the terrain was ~15-20 m below the deck the recruit
    /// actually walked on, and it silently did nothing. The two triggers left are spheres, which makes them worse
    /// than that: a sphere tests one distance in three dimensions, so a height error eats the radius - the cave-in
    /// (198602, 10 m) would fail outright if the tunnel floor is 10 m from the recorded point, and the dropship pad
    /// (198603, 12 m) loses reach the same way.
    ///
    /// Both become vertical cylinders of the same horizontal size - so "the place" is unchanged - with a 25 m
    /// half-height, which spans the walking surface above and below the recorded Y. Their recorded Y values are
    /// kept: the cave-in's is its objective marker's own (120.5, matching the courtyard placements), and the pad's
    /// is 1.2 m under its own markers and reinforcement placements (100.7-100.8), so in both cases the measured
    /// value is the player's level and only the shape needed the OD-44 treatment.
    /// </summary>
    public static class BootcampRemainingTriggerHeightRows
    {
        public const string Migration = "BootcampRemainingTriggerHeight";

        /// <summary>The two sphere triggers, with the cylinder the height fix gives them.</summary>
        public static readonly (uint AreaId, byte SphereShape, byte CylinderShape, double HalfHeight)[] Areas =
        {
            (198602u, 1, 2, 25.0),   // 1994 objective 2, the cave-in
            (198603u, 1, 2, 25.0)    // 1995/2005, the exit dropship pad
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, _, cylinderShape, halfHeight) in Areas)
            {
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "shape",
                    value: cylinderShape);
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "half_height",
                    value: halfHeight);
            }
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, sphereShape, _, _) in Areas)
            {
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "shape",
                    value: sphereShape);
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "half_height",
                    value: 0.0);
            }
        }
    }
}
