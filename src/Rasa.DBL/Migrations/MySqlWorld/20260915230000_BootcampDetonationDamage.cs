using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampDetonationDamage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampDetonationDamageRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampDetonationDamageRows.DeleteData(migrationBuilder);
        }
    }
}
