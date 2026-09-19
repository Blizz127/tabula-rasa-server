using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The Target Dummy (1992/8, "Use your Lightning power on the Target Dummy") brought out of its lane's sandbags
    /// to where the practice dummy stands in its own.
    ///
    /// BootcampTargetDummyLane put it on the middle lane's centre line (380.66, 188.6), reading the lane's sandbag pair
    /// as flanking an open space. adv_bootcamp.map says otherwise: each lane's two pieces
    /// (ArchHumGenObjSandbags01V08, one turned 90 degrees) overlap at the centre line - the middle lane's bounds are
    /// X 377.33-380.87 and 380.42-383.91, both over Z 186.9-190.3 - so that point is inside the emplacement. In play
    /// the client refused it: "the other target in the middle it says it can't see it" (2026-09-19).
    ///
    /// The practice dummy, the one measured from footage (384.7, 119.4, 186.8), stands in front of its emplacement's
    /// face (Z from 186.69), 1.45 m to the left of its lane's centre (386.15). The Target Dummy now stands the same
    /// way in the next lane: 380.66 - 1.45 = 379.21, Z 186.8, 0.1 m in front of that lane's face (Z from 186.9), on
    /// the same floor. The lane is original map data; the stance is the practice dummy's, one lane over (inferred).
    /// </summary>
    public static class BootcampTargetDummyFrontRows
    {
        public const uint TargetDummy = 198653u;

        public const double WasX = BootcampTargetDummyLaneRows.NowX, WasY = BootcampTargetDummyLaneRows.NowY, WasZ = BootcampTargetDummyLaneRows.NowZ;
        public const double NowX = 379.21, NowY = 119.4, NowZ = 186.8;

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
