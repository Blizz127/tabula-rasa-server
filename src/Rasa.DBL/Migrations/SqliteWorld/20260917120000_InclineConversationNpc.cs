using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class InclineConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            InclineConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            InclineConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
