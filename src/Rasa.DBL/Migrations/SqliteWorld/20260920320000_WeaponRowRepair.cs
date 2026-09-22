using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The 3381 weapon templates with no itemtemplate_weapon row, and the 123 rows that read wrong.
    /// See <see cref="WeaponRowRepairRows"/>.
    /// </summary>
    public partial class WeaponRowRepair : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WeaponRowRepairRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WeaponRowRepairRows.DeleteData(migrationBuilder);
    }
}
