using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The fifteen entity classes this server labelled Missing_ItemClassId_N/15, given the names the client
    /// has for them. See <see cref="ClientNamesForItemsRows"/>.
    /// </summary>
    public partial class ClientNamesForItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => ClientNamesForItemsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => ClientNamesForItemsRows.DeleteData(migrationBuilder);
    }
}
