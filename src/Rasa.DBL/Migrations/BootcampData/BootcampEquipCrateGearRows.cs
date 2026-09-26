using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The already equipped Recruit outfit must not satisfy Gearing Up's crate-gear step.
    /// 7Lrst9SG3pk A3-027 (308.533 s) leaves it open until the crate boots are equipped,
    /// A3-036/037 (321.667/322.267 s). Matching any member of the crate set is inferred;
    /// the recording proves boots, not every possible matching item or original predicate.
    /// </summary>
    public static class BootcampEquipCrateGearRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
            => Update(migrationBuilder, 2, 19858u, "1992/2 equip crate gear");

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => Update(migrationBuilder, 0, 0u, "1992/2 equip any");

        private static void Update(MigrationBuilder migrationBuilder, byte match, uint itemSet, string comment)
            => migrationBuilder.UpdateData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[] { 1992u, 2u, (byte)0 },
                columns: new[] { "equip_match", "item_set_id", "comment" },
                values: new object[] { match, itemSet, comment });
    }
}
