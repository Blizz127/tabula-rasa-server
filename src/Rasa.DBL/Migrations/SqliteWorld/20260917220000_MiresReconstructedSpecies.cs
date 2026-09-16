using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class MiresReconstructedSpecies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MiresReconstructedSpeciesRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MiresReconstructedSpeciesRows.DeleteData(migrationBuilder);
        }
    }
}
