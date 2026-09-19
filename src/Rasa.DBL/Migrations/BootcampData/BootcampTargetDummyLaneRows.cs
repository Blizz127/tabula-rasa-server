using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The boot camp's Target Dummy - the one 1992/8 "Use your Lightning power on the Target Dummy" is bound to -
    /// moved out of a sandbag emplacement and into the firing-range lane beside the practice dummy.
    ///
    /// Its position was never measured: the Lightning step falls inside the footage cut at 362.067/362.133, and
    /// OD-13 placed a second dummy "by labelled match", 2.9 m from the practice dummy at (387.0, 119.4, 188.5). In
    /// play it could not be hit ("the dummy on the right is out of range, looks a bit misplaced", 2026-09-19), and
    /// the client's own map says why. adv_bootcamp.map builds the range as lanes 5.35 m apart, each with a
    /// flashlight (ArchHumGenObjFlashlight) at z 184.0, an overhead light (ArchHumGenObjLightV03) at z 189.5 and a
    /// pair of sandbag emplacements (ArchHumGenObjSandbags01V08) flanking where the target stands at z ~188.6:
    ///
    ///   lane x 386.15 - sandbags 384.35 / 387.95 - the practice dummy, measured at 384.7 +/-1.5
    ///   lane x 380.66 - sandbags 378.91 / 382.38 - empty
    ///   lane x 375.31 - sandbags 373.54 / 377.02 - empty
    ///
    /// The OD-13 position was 0.95 m from the sandbag at (387.95, 188.58), inside the emplacement. The target
    /// dummy now stands in the next lane, at its centre line (380.66) between that lane's sandbags (z 188.6), on
    /// the same floor as the practice dummy (119.4). The lane is original map data; which lane the original used
    /// is inferred - the adjacent one, as the objective text calls it "the Target Dummy" beside the one just shot.
    /// </summary>
    public static class BootcampTargetDummyLaneRows
    {
        public const uint TargetDummy = 198653u;

        public const double WasX = 387.0, WasY = 119.4, WasZ = 188.5;
        public const double NowX = 380.66, NowY = 119.4, NowZ = 188.6;

        public static void InsertData(MigrationBuilder migrationBuilder) => Move(migrationBuilder, NowX, NowY, NowZ);

        public static void DeleteData(MigrationBuilder migrationBuilder) => Move(migrationBuilder, WasX, WasY, WasZ);

        private static void Move(MigrationBuilder migrationBuilder, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: TargetDummy, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: TargetDummy, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: TargetDummy, column: "pos_z", value: z);
        }
    }
}
