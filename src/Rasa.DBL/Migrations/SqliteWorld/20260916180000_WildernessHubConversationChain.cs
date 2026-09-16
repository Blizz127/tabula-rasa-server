using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubConversationChain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubConversationChainRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubConversationChainRows.DeleteData(migrationBuilder);
        }
    }
}
