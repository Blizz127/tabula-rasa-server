using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Inferred minimal HP reproducing five single-hit destructions in 7Lrst9SG3pk,
    /// 347.133–361.200 s. One observed hit displays 84 damage. This is not recovered
    /// original HP: low health and a server script forcing destruction remain indistinguishable.
    /// The unfilmed Lightning target retains its prior estimate.
    /// </summary>
    public static class BootcampPracticeDummyHealthRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 1u);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 100u);
        private static void Set(MigrationBuilder migrationBuilder, uint health)
            => migrationBuilder.UpdateData("content_placement", "id", 198652u, "hit_points", health);
    }
}
