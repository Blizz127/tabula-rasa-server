using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The 51 NPCs TaRapedia documents that this world did not have.
    /// See <see cref="TarapediaMissingNpcBatchRows"/>.
    /// </summary>
    public partial class TarapediaMissingNpcBatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaMissingNpcBatchRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaMissingNpcBatchRows.DeleteData(migrationBuilder);
    }
}
