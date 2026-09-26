using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// One class trainer per hub, as D12 left them: the 38 per-class trainers retired (pools 0/0) and Training
    /// Officer Stratton on the client's "Class Trainer: Daghda's Urn" marker.
    /// See <see cref="SingleClassTrainersRows"/>.
    /// </summary>
    public partial class SingleClassTrainers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => SingleClassTrainersRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => SingleClassTrainersRows.DeleteData(migrationBuilder);
    }
}
