using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// McAllister remains beside the recruit until Gearing Up is accepted, then walks away
    /// (7Lrst9SG3pk A2-056/057/060, 296.2/296.6/297-301 s). The 2.5 m/s pace is an analogue:
    /// the original animation reference rate of his reconstructed human class, not a measured
    /// speed of the lost original spawn. See docs/bootcamp-opening-movement-audit.md.
    /// </summary>
    public static class BootcampMcAllisterWalkRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData("creature", "id", 198500u, "walk_speed", 2.5d);
            UpdateRule(migrationBuilder, 2, 1992u, "1992 accepted -> McAllister walks to the gear");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            UpdateRule(migrationBuilder, 6, 1990u, "1990 turned in -> McAllister walks to the gear");
            migrationBuilder.UpdateData("creature", "id", 198500u, "walk_speed", 0d);
        }

        private static void UpdateRule(MigrationBuilder migrationBuilder, byte trigger, uint mission, string comment)
            => migrationBuilder.UpdateData("content_rule", "id", 1985014u,
                new[] { "event", "mission_id", "comment" }, new object[] { trigger, mission, comment });
    }
}
