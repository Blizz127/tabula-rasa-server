using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>7Lrst9SG3pk A3-018304.733s: the rifle still in the crate shows 20/20 rounds.</summary>
    public static class BootcampCrateLoadedRifleRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 20u);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 0u);
        private static void Set(MigrationBuilder migrationBuilder, uint rounds)
            => migrationBuilder.UpdateData("content_item_set", new[] { "item_set_id", "item_template_id" },
                new object[] { 19858u, 13713u }, "initial_ammo", rounds);
    }
}
