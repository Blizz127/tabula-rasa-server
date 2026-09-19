using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    /// <summary>
    /// The Target Dummy moved out of a sandbag emplacement into the empty firing-range lane beside the practice
    /// dummy, from the client map's own lane geometry. See <see cref="BootcampTargetDummyLaneRows"/>.
    /// </summary>
    public partial class BootcampTargetDummyLane : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampTargetDummyLaneRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampTargetDummyLaneRows.DeleteData(migrationBuilder);
    }
}
