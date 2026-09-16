using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Drops Receptive Liaison Sage onto the ground the map data describes.
    ///
    /// TaRapedia's /loc for her (137, 237, -268) is right in the horizontal plane but 3.2 m above the surface the
    /// client's own navmesh has there (233.8). A body placed at the reading would stand in the air, and the navmesh
    /// is the original map data, so the placement takes the surface height for its Y and keeps the reading's X and Z:
    /// tier <c>measured</c>, derived from two sources, rather than either source alone.
    ///
    /// This is the only Torden placement that needed it - the audit measures every content position against the same
    /// navmesh (WorldPositionAuditTests) and passed the rest - and it is recorded on the placement's manifest row.
    /// </summary>
    public static class TordenNpcGroundSnapRows
    {
        public const string Migration = "TordenNpcGroundSnap";

        /// <summary>Receptive Liaison Sage, on the Torden incline (map 1761).</summary>
        public const uint Sage = 199504u;

        /// <summary>The surface the navmesh reports under (137, -268) on the incline.</summary>
        public const double SurfaceY = 233.8;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Sage },
                column: "pos_y",
                value: SurfaceY);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "content_placement",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Sage },
                column: "pos_y",
                value: 237.0);
        }
    }
}
