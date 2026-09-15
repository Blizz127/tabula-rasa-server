using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Raises the height the S1 objective triggers span, because the bridge they stand on arches.
    ///
    /// The area probe added on 2026-09-15 answered the live report directly. Walking south from the camp with
    /// mission 1990 objective 2 active, the recruit's own position read:
    ///
    ///     at (388.2, 125.1, 47.6)  horizontal 40.9 m of 10, vertical 12.3 m of 20
    ///     at (387.9, 125.2, 45.9)  horizontal 39.2 m of 10, vertical 12.4 m of 20
    ///     at (388.0, 127.2, 40.2)  horizontal 33.5 m of 10, vertical 14.5 m of 20
    ///     at (388.8, 129.2, 33.6)  horizontal 26.9 m of 10, vertical 16.4 m of 20
    ///
    /// The height climbs with every metre south: the ceremonial bridge arches, and by the marker at Z 6.71 the
    /// deck is above the trigger's ceiling (the area's Y 112.75, the terrain under the span, plus the 20 m
    /// half-height, ends at 132.75). So the recruit walked *through* the marker while being above the volume that
    /// tests it - a radius change could never have fixed this.
    ///
    /// Both S1 areas therefore span 40 m instead of 20 (OD-44: enough to catch the crossing, without reaching a
    /// different place). The recorded Y values stay as they came from the terrain; the extent is the
    /// reconstruction's adaptation and the next probe pass at the marker can pin the deck height so the volume can
    /// be tightened around it later.
    /// </summary>
    public static class BootcampObjectiveAreaHeightRows
    {
        public const string Migration = "BootcampObjectiveAreaHeight";

        /// <summary>The S1 objective areas, with the extent the vertical fix gives them.</summary>
        public static readonly (uint AreaId, double OldHalfHeight, double NewHalfHeight)[] Areas =
        {
            (198600u, 20.0, 40.0),   // objective 1, mid-causeway
            (198601u, 20.0, 40.0)    // objective 2, north end of the bridge
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, _, newHalfHeight) in Areas)
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "half_height",
                    value: newHalfHeight);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (areaId, oldHalfHeight, _) in Areas)
                migrationBuilder.UpdateData(
                    table: "content_area",
                    keyColumn: "id",
                    keyValue: areaId,
                    column: "half_height",
                    value: oldHalfHeight);
        }
    }
}
