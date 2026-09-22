using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>See <see cref="MissionSpeakersRows"/>.</summary>
    public partial class MissionSpeakers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => MissionSpeakersRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => MissionSpeakersRows.DeleteData(migrationBuilder);
    }
}
