using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Eighteen missions given or turned in at the wrong NPC, against TaRapedia's own mission infoboxes.
    /// See <see cref="TarapediaGiverAuditRows"/>.
    /// </summary>
    public partial class TarapediaGiverAudit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaGiverAuditRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaGiverAuditRows.DeleteData(migrationBuilder);
    }
}
