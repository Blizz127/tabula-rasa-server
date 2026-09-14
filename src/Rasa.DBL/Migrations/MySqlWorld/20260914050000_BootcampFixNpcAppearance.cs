using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampFixNpcAppearance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampFixNpcAppearanceRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampFixNpcAppearanceRows.DeleteData(migrationBuilder);
        }
    }
}
