using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Moves the inferred 1992 receiver / 1994 giver to the final-week footage's
    /// camp-pylon handoff area. DeSimone himself is not visible in the edited
    /// interval; the new position remains an estimate, documented in
    /// docs/evidence/bootcamp-desimone-camp-placement.json.
    /// </summary>
    public static class BootcampDeSimoneCampPlacementRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) =>
            Set(migrationBuilder, 390.5, 119.55, 156.0);

        public static void DeleteData(MigrationBuilder migrationBuilder) =>
            Set(migrationBuilder, 387.2, 127.7, 40.0);

        private static void Set(MigrationBuilder migrationBuilder, double x, double y, double z)
        {
            migrationBuilder.UpdateData("content_placement", "id", 198657u,
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { x, y, z });
            var update = (UpdateDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1];
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "DOUBLE", "DOUBLE", "DOUBLE" };
        }
    }
}
