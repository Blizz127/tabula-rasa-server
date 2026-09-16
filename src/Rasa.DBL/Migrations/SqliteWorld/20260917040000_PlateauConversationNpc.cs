using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class PlateauConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
