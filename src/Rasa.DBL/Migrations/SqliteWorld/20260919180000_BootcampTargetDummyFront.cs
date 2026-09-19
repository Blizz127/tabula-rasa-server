using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    /// <summary>
    /// The Target Dummy brought out of its lane's sandbags to stand as the practice dummy does in its own.
    /// See <see cref="BootcampTargetDummyFrontRows"/>.
    /// </summary>
    public partial class BootcampTargetDummyFront : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampTargetDummyFrontRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampTargetDummyFrontRows.DeleteData(migrationBuilder);
    }
}
