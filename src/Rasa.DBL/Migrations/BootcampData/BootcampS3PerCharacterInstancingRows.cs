using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The frozen row of the boot-camp S3 migration (<c>BootcampS3PerCharacterInstancing</c>):
    /// context 1985 becomes a private per-character instance (content_map_setting instancing 1).
    /// Tier and citations: docs/evidence/bootcamp-d11-reconstruction-manifest.json (OD-2).
    /// Both provider migrations call this class. Never edit after release.
    /// </summary>
    public static class BootcampS3PerCharacterInstancingRows
    {
        public const string Migration = "BootcampS3PerCharacterInstancing";

        public static void InsertData(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData(
                table: "content_map_setting",
                columns: new[] { "map_context_id", "instancing", "comment" },
                values: new object[] { 1985u, (byte)1, "adv_bootcamp: private per-character instance" });

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => migrationBuilder.DeleteData(
                table: "content_map_setting",
                keyColumn: "map_context_id",
                keyValue: 1985u);
    }
}
