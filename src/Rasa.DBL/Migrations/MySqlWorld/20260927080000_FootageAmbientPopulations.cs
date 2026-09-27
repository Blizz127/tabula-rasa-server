using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Torden Plains (1764) and Torden Mires (1759) ambient populations and six more Palisades (1244) groups from the
    /// Raisuly captures, the Palisades analogue levels, names and Warnet place the same footage replaces, and 1067 Can't
    /// Survive Without My Radio. See <see cref="WildernessData.FootageAmbientPopulationsRows"/>.
    /// </summary>
    public partial class FootageAmbientPopulations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.FootageAmbientPopulationsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.FootageAmbientPopulationsRows.DeleteData(migrationBuilder);
    }
}
