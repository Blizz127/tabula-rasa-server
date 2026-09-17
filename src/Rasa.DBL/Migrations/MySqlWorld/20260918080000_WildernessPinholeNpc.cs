using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// W3: Mining Coord. Richards at the Pinhole Falls Caverns and the wounded Forean Ranger at the top of the falls,
    /// which is what missions 422 and 429 complete their objectives through - plus the objective flags 429 was never
    /// offered over. The rows and their provenance are in <see cref="WildernessPinholeNpcRows"/>.
    /// </summary>
    public partial class WildernessPinholeNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessPinholeNpcRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessPinholeNpcRows.DeleteData(migrationBuilder);
    }
}
