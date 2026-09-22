using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The blood analyser and the Eloh obelisk, given a body the two missions waiting on them can talk to.
    /// See <see cref="MissionPropSpeakersRows"/>.
    /// </summary>
    public partial class MissionPropSpeakers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => MissionPropSpeakersRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => MissionPropSpeakersRows.DeleteData(migrationBuilder);
    }
}
