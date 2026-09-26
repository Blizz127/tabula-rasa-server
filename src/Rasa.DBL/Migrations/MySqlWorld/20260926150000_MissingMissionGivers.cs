using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The missing mission givers of 2026-09-26: Simpson, Tarina, Hanna and Dekay placed from dated guide readings,
    /// the liaison packages 2049/2025 bound, Langerman made conversable, Mela moved to a dated reading, and 1741, 1744,
    /// 390, 818 and 1862 seeded. See <see cref="MissingMissionGiversRows"/>.
    /// </summary>
    public partial class MissingMissionGivers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => MissingMissionGiversRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => MissingMissionGiversRows.DeleteData(migrationBuilder);
    }
}
