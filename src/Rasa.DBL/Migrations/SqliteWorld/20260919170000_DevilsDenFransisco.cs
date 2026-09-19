using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Captain Fransisco in the Devil's Den, and Field Lt. Brody given his own dialogue. See <see cref="DevilsDenFransiscoRows"/>.
    /// </summary>
    public partial class DevilsDenFransisco : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => DevilsDenFransiscoRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => DevilsDenFransiscoRows.DeleteData(migrationBuilder);
    }
}
