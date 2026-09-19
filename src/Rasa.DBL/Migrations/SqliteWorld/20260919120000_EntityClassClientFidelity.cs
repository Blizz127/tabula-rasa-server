using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.ClientData;

    /// <summary>
    /// Seven entityclass rows put back to the client's own table. See <see cref="EntityClassClientFidelityRows"/>.
    /// </summary>
    public partial class EntityClassClientFidelity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => EntityClassClientFidelityRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => EntityClassClientFidelityRows.DeleteData(migrationBuilder);
    }
}
