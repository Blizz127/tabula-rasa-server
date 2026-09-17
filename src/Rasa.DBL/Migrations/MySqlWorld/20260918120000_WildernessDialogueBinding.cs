using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Twelve Wilderness NPCs that were already in the world with nothing to say, given the dialogue package the
    /// client's own objectiveconversation table says they speak. Fourteen objectives over nine missions stop
    /// being dead ends. Every binding and its evidence is in <see cref="WildernessDialogueBindingRows"/>.
    /// </summary>
    public partial class WildernessDialogueBinding : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessDialogueBindingRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessDialogueBindingRows.DeleteData(migrationBuilder);
    }
}
