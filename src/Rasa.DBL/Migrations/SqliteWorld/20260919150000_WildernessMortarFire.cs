using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Mission 430's Bane Mortars fire their ground-target launcher. See <see cref="WildernessMortarFireRows"/>.
    /// </summary>
    public partial class WildernessMortarFire : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessMortarFireRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessMortarFireRows.DeleteData(migrationBuilder);
    }
}
