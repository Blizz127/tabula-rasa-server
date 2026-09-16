using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class MiresConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MiresConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MiresConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
