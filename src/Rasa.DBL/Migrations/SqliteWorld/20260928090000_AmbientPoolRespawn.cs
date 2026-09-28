using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The respawn delay of the hostile ambient pools (the 2026-09-27 population batches' 45 pools and the Wilderness world
    /// seed's 83): from 2-5 s to the owner's staggered 60-300 s (OD-186). See <see cref="WildernessData.AmbientPoolRespawnRows"/>.
    /// </summary>
    public partial class AmbientPoolRespawn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.AmbientPoolRespawnRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.AmbientPoolRespawnRows.DeleteData(migrationBuilder);
    }
}
