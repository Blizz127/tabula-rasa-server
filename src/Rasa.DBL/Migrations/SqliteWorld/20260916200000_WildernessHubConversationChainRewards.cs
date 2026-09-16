using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    public partial class WildernessHubConversationChainRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubConversationChainRewardRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessHubConversationChainRewardRows.DeleteData(migrationBuilder);
        }
    }
}
