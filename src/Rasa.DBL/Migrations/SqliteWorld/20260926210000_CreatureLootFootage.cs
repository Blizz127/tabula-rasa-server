using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Creature loot read from the footage ledger: Thrax Skull and standard-grade ammunition on the Thrax infantry,
    /// Boargar Ear on the Young Forest Boargar, and the emulator's contradicted cartridge rows removed.
    /// See <see cref="CreatureLootFootageRows"/>.
    /// </summary>
    public partial class CreatureLootFootage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => CreatureLootFootageRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => CreatureLootFootageRows.DeleteData(migrationBuilder);
    }
}
