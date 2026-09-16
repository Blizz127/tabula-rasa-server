using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class PalisadesConversationNpc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PalisadesConversationNpcRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PalisadesConversationNpcRows.DeleteData(migrationBuilder);
        }
    }
}
