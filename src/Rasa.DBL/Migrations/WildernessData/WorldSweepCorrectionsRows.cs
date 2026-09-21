using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Three rows the sweep of 2026-09-21 left in a worse place than it found them, corrected by the audits it
    /// shipped with.
    ///
    /// Two are its own doing. The wargame vendors at Edmund Range and the Proving Grounds stood 2.4 m in the air,
    /// so <see cref="WorldFloorSweepRows"/> put them on the floor - and the floor under them is inside an ammo
    /// crate (ArchHumGenObjCrateAmo01V03), which only <see cref="PropOverlapFixRows"/>'s check could see, and
    /// which it ran before the snap rather than after. They step 1.25 m clear of the crate and keep that floor.
    ///
    /// The third is Ranger Kaely at New Velon Village, moved to TaRapedia's reading in
    /// <see cref="TarapediaNpcPositionsRows"/>. That spot has two walkable levels about 1.2 m apart, and the
    /// probe took the lower one, which left her reading as buried under the upper. She takes the upper floor,
    /// which is the one WorldPositionAuditTests measures her against.
    /// </summary>
    public static class WorldSweepCorrectionsRows
    {
        public const string Migration = "WorldSweepCorrections";

        /// <summary>(id, the x,y,z it had, the x,y,z it takes).</summary>
        public static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Y, double Z)[] Pools =
        {
            (500319u, -32.600, 363.324, -397.500, -33.850, 363.324, -397.507), // out of the ammo crate, Edmund Range
            (500325u, 71.400, 363.433, -51.700, 70.151, 363.433, -51.648),     // out of the ammo crate, Proving Grounds
            (510189u, 286.000, 349.468, 569.000, 286.000, 350.383, 569.000)    // Ranger Kaely, on the upper floor
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Pools)
                Move(migrationBuilder, row.Id, row.X, row.Y, row.Z);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Pools)
                Move(migrationBuilder, row.Id, row.WasX, row.WasY, row.WasZ);
        }

        private static void Move(MigrationBuilder migrationBuilder, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
