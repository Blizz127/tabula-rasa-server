using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class MarshesConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
