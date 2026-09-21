using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="PhostBenonFloorRows"/>.</summary>
    public partial class PhostBenonFloor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => PhostBenonFloorRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => PhostBenonFloorRows.DeleteData(migrationBuilder);
    }
}
