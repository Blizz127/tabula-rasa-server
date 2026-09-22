using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The 23 Logos missions, each one bound to a shrine this world already places.
    /// See <see cref="SeedLogosMissionsRows"/>.
    /// </summary>
    public partial class SeedLogosMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => SeedLogosMissionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => SeedLogosMissionsRows.DeleteData(migrationBuilder);
    }
}
