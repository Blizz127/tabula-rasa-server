using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Converts the measured compass heading 345 degrees to original-client actor
    /// yaw 165 degrees. Native yaw pi faces +Z; pi/2 faces -X, verified with the
    /// original 1.16.5.0 client. The measured heading retains its +/-15 degree
    /// uncertainty. See docs/evidence/client-world-admission-orientation.json.
    /// </summary>
    public static class BootcampFirstLoginYawRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 2.879793);
        public static void DeleteData(MigrationBuilder migrationBuilder) => Set(migrationBuilder, 6.02139);
        private static void Set(MigrationBuilder migrationBuilder, double yaw)
            => migrationBuilder.UpdateData("content_location", "id", 19851u, "rotation", yaw);
    }
}
