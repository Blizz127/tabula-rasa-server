using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Mission 430's four mortars become targetable Bane Mortar creatures. See <see cref="WildernessMortarCreatureRows"/>.
    /// </summary>
    public partial class WildernessMortarCreature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessMortarCreatureRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessMortarCreatureRows.DeleteData(migrationBuilder);
    }
}
