using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The last eight NPCs the wiki documents and this world lacked, and five bodies it puts elsewhere.
    /// See <see cref="TarapediaLastNpcsRows"/>.
    /// </summary>
    public partial class TarapediaLastNpcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaLastNpcsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaLastNpcsRows.DeleteData(migrationBuilder);
    }
}
