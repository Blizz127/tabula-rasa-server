using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Divide (1148) and Concordia Palisades (1244) ambient populations - twelve creature rows and 22 spawn pools from the
    /// zones' footage, text and final map, with labelled stand-ins - and the missions they make finishable: 358, 371, 372, 774
    /// and 755 on the Divide, 342 and 1808 on Palisades, and 368's Warnet kill count. See
    /// <see cref="WildernessData.ConcordiaAmbientPopulationsRows"/>.
    /// </summary>
    public partial class ConcordiaAmbientPopulations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.ConcordiaAmbientPopulationsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.ConcordiaAmbientPopulationsRows.DeleteData(migrationBuilder);
    }
}
