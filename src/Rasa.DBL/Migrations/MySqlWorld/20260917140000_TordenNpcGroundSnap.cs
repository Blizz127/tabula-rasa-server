using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class TordenNpcGroundSnap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            TordenNpcGroundSnapRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            TordenNpcGroundSnapRows.DeleteData(migrationBuilder);
        }
    }
}
