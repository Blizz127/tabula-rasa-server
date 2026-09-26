using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// TaRapedia's 2008-01-26 mission revision lists Receptive Reception at level 4.
    /// The earlier emulator reconstruction assigned level 5 without direct evidence.
    /// </summary>
    public static class WildernessHubReceptiveLevelRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder) => SetLevel(migrationBuilder, 4u);
        public static void DeleteData(MigrationBuilder migrationBuilder) => SetLevel(migrationBuilder, 5u);

        private static void SetLevel(MigrationBuilder migrationBuilder, uint level) => migrationBuilder.UpdateData(
            table: "npc_mission",
            keyColumn: "id",
            keyValue: 1069u,
            column: "level",
            value: level);
    }
}
