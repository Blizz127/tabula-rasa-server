using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="PropOverlapFixRows"/>.</summary>
    public partial class PropOverlapFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => PropOverlapFixRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => PropOverlapFixRows.DeleteData(migrationBuilder);
    }
}
