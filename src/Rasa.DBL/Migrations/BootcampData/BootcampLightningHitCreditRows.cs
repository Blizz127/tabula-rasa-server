using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Mission 1992 objective 8 teaches one use of Lightning on the Target Dummy.
    /// Original client text 21665 says to target the dummy and fire Lightning;
    /// objective text 21666 says to use Lightning on it. Neither requires its
    /// destruction. The lesson itself is cut from the surviving final-week video,
    /// so credit on the first damaging hit is an inferred reconstruction.
    /// The action and target restrictions remain in the original S2 binding.
    /// </summary>
    public static class BootcampLightningHitCreditRows
    {
        public const uint MissionId = 1992u;
        public const uint ObjectiveId = 8u;

        public static void InsertData(MigrationBuilder migrationBuilder)
            => Update(migrationBuilder, false);

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => Update(migrationBuilder, true);

        private static void Update(MigrationBuilder migrationBuilder, bool destroyingHitOnly)
        {
            migrationBuilder.UpdateData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[] { MissionId, ObjectiveId, (byte)0 },
                column: "destroying_hit_only",
                value: destroyingHitOnly);
            // The migration shipped without a Designer (one was generated on recovery);
            // explicit column types keep its SQL independent of the target model.
            var update = (UpdateDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1];
            update.KeyColumnTypes = new[] { "INTEGER", "INTEGER", "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER" };
        }
    }
}
