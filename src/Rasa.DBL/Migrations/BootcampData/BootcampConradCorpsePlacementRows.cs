using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Move the inferred Conrad usable from inside the ramp support to the
    /// reachable floor beside it. The original position is retained in the
    /// S5 reconstruction and restored by Down for a reversible correction.
    /// Client playthroughs 31 and 32, 2026-09-22; retail coordinates unknown.
    /// </summary>
    public static class BootcampConradCorpsePlacementRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, -98.0, 85.39, 67.2);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, -102.4, 85.69, 66.8);

        private static void Set(MigrationBuilder migrationBuilder, double x, double y, double z)
        {
            migrationBuilder.UpdateData("content_placement", "id", 198676u,
                new[] { "pos_x", "pos_y", "pos_z" }, new object[] { x, y, z });
            var update = (UpdateDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1];
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "DOUBLE", "DOUBLE", "DOUBLE" };
        }
    }
}
