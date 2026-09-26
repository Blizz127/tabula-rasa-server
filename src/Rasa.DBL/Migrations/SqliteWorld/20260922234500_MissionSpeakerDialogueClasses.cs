using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    public partial class MissionSpeakerDialogueClasses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionSpeakerDialogueClassesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.MissionSpeakerDialogueClassesRows.DeleteData(migrationBuilder);
    }
}
