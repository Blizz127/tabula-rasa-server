using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessMortarByNumbers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessMortarByNumbersRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessMortarByNumbersRows.DeleteData(migrationBuilder);
        }
    }
}
