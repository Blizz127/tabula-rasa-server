using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    // Original class27220 attacks with (1,134), whose action range is60m.
    // A3-018 independently shows60m for the crate rifle. Only the selected
    // crate counterpart is changed; see evidence/bootcamp-rifle-range.json.
    public partial class BootcampCrateRifleRange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData("itemtemplate_weapon", "id", 13713u, "range", 60u);

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData("itemtemplate_weapon", "id", 13713u, "range", 80u);
    }
}
