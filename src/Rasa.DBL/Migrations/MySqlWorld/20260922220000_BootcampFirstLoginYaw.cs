using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampFirstLoginYaw : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampFirstLoginYawRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampData.BootcampFirstLoginYawRows.DeleteData(migrationBuilder);
    }
}
