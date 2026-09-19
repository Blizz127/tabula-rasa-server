using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The NPCs behind the last of TaRapedia's giver mismatches, and the dialogue no one carried.
    /// See <see cref="TarapediaMissingNpcsRows"/>.
    /// </summary>
    public partial class TarapediaMissingNpcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaMissingNpcsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaMissingNpcsRows.DeleteData(migrationBuilder);
    }
}
