using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Concordia Divide's (1148) open-ground wildlife and Bane patrols the earlier batches left out. See
    /// <see cref="WildernessData.DivideOpenGroundPopulationsRows"/>.
    /// </summary>
    public partial class DivideOpenGroundPopulations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.DivideOpenGroundPopulationsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.DivideOpenGroundPopulationsRows.DeleteData(migrationBuilder);
    }
}
