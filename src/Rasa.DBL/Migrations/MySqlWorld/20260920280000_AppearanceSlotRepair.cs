using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// 421 outfit rows into the slot the client gives their class. See <see cref="AppearanceSlotRepairRows"/>.
    /// </summary>
    public partial class AppearanceSlotRepair : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => AppearanceSlotRepairRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => AppearanceSlotRepairRows.DeleteData(migrationBuilder);
    }
}
