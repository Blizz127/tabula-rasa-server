using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Seven NPCs from Ellatha's database that this world did not have.
    /// See <see cref="EllathaWorldNpcsRows"/>.
    /// </summary>
    public partial class EllathaWorldNpcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => EllathaWorldNpcsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => EllathaWorldNpcsRows.DeleteData(migrationBuilder);
    }
}
