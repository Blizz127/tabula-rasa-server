using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class DivideConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DivideConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DivideConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
