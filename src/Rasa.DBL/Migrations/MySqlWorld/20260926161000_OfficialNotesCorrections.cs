using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The 2026-09-26 official-notes audit: 2016 A Mystery Unearthed at the D14 notes' level 50 (given at Twin
    /// Pillars, Wilderness), and 682/3 Childhood's End crediting the Xanx kill to everyone with the objective
    /// (1.6 and D8 notes). See <see cref="OfficialNotesCorrectionsRows"/>.
    /// </summary>
    public partial class OfficialNotesCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => OfficialNotesCorrectionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => OfficialNotesCorrectionsRows.DeleteData(migrationBuilder);
    }
}
