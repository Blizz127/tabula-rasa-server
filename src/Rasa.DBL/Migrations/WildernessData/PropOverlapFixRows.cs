using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Seven NPCs standing inside the furniture, pushed clear of it.
    ///
    /// A live report on 2026-09-20 - "npc placement seems off too", with a shot of an NPC standing in a cot at
    /// Alia Das - found what no floor check could: a cot is half a metre tall, so a body standing in one reads
    /// as standing on the floor. The props come from the client's own map files (<c>mapprops/</c>, the boxes
    /// rotated into each prop's own frame), so where a body may not stand is original-tier evidence.
    ///
    /// Sixteen bodies in the world stand inside a prop. Nine are deliberate or original and are listed in
    /// <c>PropOverlapAuditTests.Allowed</c>: mission 430's four mortars, which are placed on the client map's
    /// own mortar launchers; the two boot camp dummies on the gate catwalk and two Thrax Initiates on corridor
    /// debris, all measured from D11 footage; and the original server's own Twin Pillars weapon vendor, who
    /// stands behind her counter.
    ///
    /// The seven below are upstream's coordinates, and each one puts an NPC inside a piece of furniture: the
    /// Alia Das ranger trainer in a cot (the report's own NPC), the Torden Mires ranger trainer in a bed, two
    /// military surplus vendors inside armour mannequins, a medical vendor on a chair, a hospital marker inside
    /// a Wardenbot computer, and - ours, not upstream's - Outpost Cmdr. Russ 1.8 m inside a workstation at Wedge
    /// Rock Outpost, where only his X and Z came from TaRapedia.
    ///
    /// Each is moved along the line out of the prop's centre to the first point that is clear of every prop by
    /// 0.3 m, between 0.75 m and 1.5 m in every case, and re-seated on the navmesh floor there.
    /// </summary>
    public static class PropOverlapFixRows
    {
        public const string Migration = "PropOverlapFix";

        /// <summary>
        /// (id, whether it is a content placement, the x,y,z it had, the x,z it takes, and the floor there -
        /// the navmesh surface less the original spawn offset, which is what the Y becomes).
        /// </summary>
        public static readonly (uint Id, bool Placement, double WasX, double WasY, double WasZ, double X, double Z, double Floor)[] Rows =
        {
            (500088u, false, -81.000, 10.100, -14.600, -80.002, -14.669, 9.839),    // Raksha Robotics hospital, inside a Wardenbot computer
            (501026u, false, -616.900, 229.084, -516.600, -616.865, -517.599, 226.684), // Torden Mires ranger trainer, in a bed; WorldFloorSweep moved it first
            (500106u, false, 375.000, 433.800, -91.000, 374.837, -91.732, 433.324), // Irendas Colony surplus, inside an armour mannequin
            (500154u, false, -285.800, 57.400, 27.100, -286.541, 26.981, 56.985),   // Hydro Plant medical vendor, on a chair
            (501004u, false, 761.000, 294.100, 386.100, 760.671, 386.774, 294.042), // Alia Das ranger trainer, in the cot of the live report
            (500267u, false, -120.100, 400.200, 859.000, -120.225, 858.261, 399.505), // Fort Defiance surplus, inside an armour mannequin
            (199210u, true, 449.000, 378.004, 367.000, 450.440, 367.421, 378.002)   // Outpost Cmdr. Russ, inside a workstation
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                Move(migrationBuilder, row.Placement ? "content_placement" : "spawnpool", row.Id,
                    row.X, row.Floor, row.Z);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                Move(migrationBuilder, row.Placement ? "content_placement" : "spawnpool", row.Id,
                    row.WasX, row.WasY, row.WasZ);
        }

        private static void Move(MigrationBuilder migrationBuilder, string table, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
