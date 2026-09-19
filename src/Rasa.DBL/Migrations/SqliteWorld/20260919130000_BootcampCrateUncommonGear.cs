using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.BootcampData;

    /// <summary>
    /// The supply crate's armour is the uncommon level-1 Motor Assist set the footage's tooltip shows. See
    /// <see cref="BootcampCrateUncommonGearRows"/>.
    /// </summary>
    public partial class BootcampCrateUncommonGear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampCrateUncommonGearRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampCrateUncommonGearRows.DeleteData(migrationBuilder);
    }
}
