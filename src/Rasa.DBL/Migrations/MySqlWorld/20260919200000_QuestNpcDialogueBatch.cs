using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Eight NPCs whose dialogue no creature carried, and Captain Reyko given his own.
    /// See <see cref="QuestNpcDialogueBatchRows"/>.
    /// </summary>
    public partial class QuestNpcDialogueBatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => QuestNpcDialogueBatchRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => QuestNpcDialogueBatchRows.DeleteData(migrationBuilder);
    }
}
