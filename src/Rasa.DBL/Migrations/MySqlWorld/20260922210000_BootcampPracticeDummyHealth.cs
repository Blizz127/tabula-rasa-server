using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampPracticeDummyHealth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampPracticeDummyHealthRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampPracticeDummyHealthRows.DeleteData(migrationBuilder);
    }
}
