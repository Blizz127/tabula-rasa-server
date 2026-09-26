using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Crater Lake Research Facility (context 1721), the parts of the dossier that are reachable on the navmesh:
    /// Captain Velns' package, The Dead Live, Lt. Casper as a per-copy placement, Overseer Tyryd and the Logos
    /// mission 960. See <see cref="WildernessData.WildernessCraterLakeResearchFacilityRows"/>.
    /// </summary>
    public partial class WildernessCraterLakeResearchFacility : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessCraterLakeResearchFacilityRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.WildernessCraterLakeResearchFacilityRows.DeleteData(migrationBuilder);
    }
}
