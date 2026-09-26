using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The world seed gives Council Elder Moawi Redshirt_Forean_Elder (6163), whose original client
    /// augmentation list is 1,59 (creature, harvestable). The client cannot converse with that class,
    /// even though the server binds dialogue package 113. The original client class 28415 has the same
    /// Forean elder mesh and class flags, plus NPC augmentation 52. This is an explicit class analogue;
    /// Moawi's final-live entity class has not been recovered.
    /// </summary>
    public static class MoawiDialogueClassRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => SetClass(migrationBuilder, 28415u);
        public static void DeleteData(MigrationBuilder migrationBuilder) => SetClass(migrationBuilder, 6163u);

        // SQL is identical on SQLite and MySQL. Written as raw SQL because the migration
        // shipped without a generated target model (its Designer was added on recovery).
        private static void SetClass(MigrationBuilder migrationBuilder, uint classId) =>
            migrationBuilder.Sql($"UPDATE creature SET class_id = {classId} WHERE id = 38");
    }
}
