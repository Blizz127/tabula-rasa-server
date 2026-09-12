using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class CorrectRogersNpcPackage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Original client mission dialogues identify Rogers as package 116.
            // Correct only the known seed defect; preserve all other rows/values.
            // Provenance: docs/river-recon-client-evidence.md.
            migrationBuilder.Sql("UPDATE `npc_package` SET `package_id` = 116 WHERE `id` = 100 AND `package_id` = 726;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not restore the known wrong dialogue package or overwrite rows
            // that already had 116 before Up. Restore a backup if reversal is needed.
        }
    }
}
