using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class MoawiDialogueClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.MoawiDialogueClassRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.MoawiDialogueClassRows.DeleteData(migrationBuilder);
    }
}
