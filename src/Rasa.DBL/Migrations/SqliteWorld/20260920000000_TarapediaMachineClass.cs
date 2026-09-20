using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The two machines given a class the server can spawn.
    /// See <see cref="TarapediaMachineClassRows"/>.
    /// </summary>
    public partial class TarapediaMachineClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => TarapediaMachineClassRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => TarapediaMachineClassRows.DeleteData(migrationBuilder);
    }
}
