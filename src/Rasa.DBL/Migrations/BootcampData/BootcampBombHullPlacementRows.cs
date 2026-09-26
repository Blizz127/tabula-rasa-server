using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The S5 bomb's measured radar point is hidden behind the reconstructed
    /// wreck hull in the recovered client. This inferred position stays at the
    /// edge of the original +/-2 m per-axis estimate and exposes the use prompt
    /// on the near hull face. Playthroughs 35-40, 2026-09-22; retail position
    /// and wreck mesh remain unverified.
    /// </summary>
    public static class BootcampBombHullPlacementRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, -221.95, -70.5);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, -223.95, -72.34);

        private static void Set(MigrationBuilder migrationBuilder, double x, double z)
        {
            migrationBuilder.UpdateData("content_placement", "id", 198677u,
                new[] { "pos_x", "pos_z" }, new object[] { x, z });
            var update = (UpdateDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1];
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "DOUBLE", "DOUBLE" };
        }
    }
}
