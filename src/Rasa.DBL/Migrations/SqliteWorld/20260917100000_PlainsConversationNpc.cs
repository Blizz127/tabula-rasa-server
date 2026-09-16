using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class PlainsConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlainsConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlainsConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
