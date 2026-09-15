using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Fixes the two S1 objective areas so they can actually be reached.
    ///
    /// Both stand on the ceremonial bridge: the footage places objective 1's icon at (388.95, -26.28) and
    /// objective 2's at (387.83, 6.71), and the decoded map puts ArchForeanBridgeCeremonialV04 spanning
    /// Z -40.7..56.3 at X 380.6..397.4 with its origin at Y 131.3 and crystal torches at Y 134.6. The areas'
    /// X,Z are the verified radar measurements (uncertainty 1.5-2.0 m), but their Y came from the decoded
    /// terrain heightmap, which under a bridge is the ground the bridge spans rather than the deck the
    /// recruit walks on: 118.75 and 112.75, i.e. 12.6 and 18.6 m below it. A sphere of radius 4 m centred
    /// there can never contain a player crossing the bridge, which is why mission 1990's first objective
    /// never completed in play on 2026-09-15.
    ///
    /// The XZ measurement is the trustworthy axis, so the areas become vertical cylinders with a 20 m
    /// half-height: the trigger keeps its 4 m radius and now works at any height from the terrain below to
    /// well above the deck. The recorded Y values are kept as they are, because the position record says
    /// where they came from; the extent is the reconstruction's adaptation and is labelled as such in the
    /// manifest.
    ///
    /// The rows are replaced rather than updated because EF needs a migration's target model to generate a
    /// data update, and the merged `Add_map_region` migration still carries upstream's model without the
    /// content tables (recorded in docs/retail-accuracy.md); delete+insert with explicit column types, the
    /// pattern every other reconstruction slice uses, does not depend on it.
    /// </summary>
    public static class BootcampAreaVerticalExtentRows
    {
        public const string Migration = "BootcampAreaVerticalExtent";

        private const byte Sphere = 1;
        private const byte VerticalCylinder = 2;

        /// <summary>The two S1 objective areas that stand on the bridge, with the extent they get.</summary>
        public static readonly (uint AreaId, double HalfHeight)[] Areas =
        {
            (198600u, 20.0),   // 1990 objective 1, mid-causeway
            (198601u, 20.0)    // 1990 objective 2, north end of the bridge
        };

        private static readonly string[] Columns =
        {
            "id", "map_context_id", "shape", "pos_x", "pos_y", "pos_z", "radius", "half_height", "comment"
        };

        private static readonly string[] ColumnTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "REAL", "varchar(50)"
        };

        private static readonly (uint Id, double X, double Y, double Z, double Radius, string Comment)[] Original =
        {
            (198600u, 387.22, 118.75, -28.3, 4.0, "1990 obj1 causeway"),
            (198601u, 387.83, 112.75, 6.71, 4.0, "1990 obj2 terrace")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
            => Replace(migrationBuilder, VerticalCylinder);

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => Replace(migrationBuilder, Sphere);

        private static void Replace(MigrationBuilder migrationBuilder, byte shape)
        {
            foreach (var (id, x, y, z, radius, comment) in Original)
            {
                var halfHeight = shape == VerticalCylinder
                    ? Areas.Single(area => area.AreaId == id).HalfHeight
                    : 0.0;

                migrationBuilder.DeleteData(table: "content_area", keyColumn: "id", keyValue: id);

                migrationBuilder.InsertData(
                    table: "content_area",
                    columns: Columns,
                    columnTypes: ColumnTypes,
                    values: new object[] { id, 1985u, shape, x, y, z, radius, halfHeight, comment });
            }
        }
    }
}
