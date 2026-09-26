using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The Wilderness doctors' sample missions collect their items; 758 and 776 seeded, 787 gated on 758.
    /// See <see cref="WildernessXenobiologySamplesRows"/>.
    /// </summary>
    public partial class WildernessXenobiologySamples : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessXenobiologySamplesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessXenobiologySamplesRows.DeleteData(migrationBuilder);
    }
}
