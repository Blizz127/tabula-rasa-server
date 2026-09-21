using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="WorldSweepCorrectionsRows"/>.</summary>
    public partial class WorldSweepCorrections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WorldSweepCorrectionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WorldSweepCorrectionsRows.DeleteData(migrationBuilder);
    }
}
