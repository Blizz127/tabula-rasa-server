using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Applies the owner's trigger-radius rule (OD-44) to the last tight objective trigger in the camp.
    ///
    /// The rule, set on 2026-09-15 after S1's bridge triggers were widened: an objective or area trigger takes a
    /// larger range than a bare reading of the measurement suggests, enough that a recruit crossing the place
    /// cannot miss it, but not so large that it fires from outside the intended spot. The S1 objectives went from
    /// 4 m to 10 m under it (GAP-S1-TRIGGER-RADIUS).
    ///
    /// This is the cave-in trigger of mission 1994 objective 2 (area 198602, the cavern entrance at
    /// 279.05, 120.5, 66.07). Its 5 m sphere had the same problem as S1's 4 m ones: 1994's cave-in step is walked
    /// past on the way into the tunnel, so the radius becomes 10 m.
    /// </summary>
    public static class BootcampCaveInTriggerRadiusRows
    {
        public const string Migration = "BootcampCaveInTriggerRadius";

        /// <summary>The cave-in area, with its old inferred radius and the value OD-44 gives it.</summary>
        public const uint CaveInArea = 198602u;
        public const double InferredRadius = 5.0;
        public const double WidenedRadius = 10.0;

        public static void InsertData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(
                table: "content_area",
                keyColumn: "id",
                keyValue: CaveInArea,
                column: "radius",
                value: WidenedRadius);

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(
                table: "content_area",
                keyColumn: "id",
                keyValue: CaveInArea,
                column: "radius",
                value: InferredRadius);
    }
}
