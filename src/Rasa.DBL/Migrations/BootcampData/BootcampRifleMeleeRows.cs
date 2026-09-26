using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The crate rifle's original client class has a rifle primary attack, while the
    /// emulator's alternate (1,133) is the client pistol recovery animation. The
    /// original client names rifle melee (174,5) with a 4 m range, and final-week
    /// boot-camp footage shows 82 physical melee damage in this rifle's tooltip.
    /// The server template identity and minimum damage are not recovered: this is
    /// a labelled reconstruction for the inferred template 13713.
    /// </summary>
    public static class BootcampRifleMeleeRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 174, 5, 82, 4);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 1, 133, 25, 80);

        private static void Set(MigrationBuilder migrationBuilder, uint action, uint argument, uint damage, uint range)
        {
            // Explicit store types keep this SQL independent of the target model. The migration
            // shipped without a Designer; one was generated from the model snapshot on recovery.
            var columns = new[] { "alt_action_id", "alt_action_arg_id", "alt_max_damage", "alt_damage_type", "alt_range" };
            var values = new object[] { action, argument, damage, 1u, range };
            migrationBuilder.UpdateData("itemtemplate_weapon", "id", 13713u, columns, values);
            var update = (UpdateDataOperation)migrationBuilder.Operations[migrationBuilder.Operations.Count - 1];
            update.KeyColumnTypes = new[] { "INTEGER" };
            update.ColumnTypes = new[] { "INTEGER", "INTEGER", "INTEGER", "INTEGER", "INTEGER" };
        }
    }
}
