using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    public partial class BootcampS3PerCharacterInstancing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            BootcampS3PerCharacterInstancingRows.InsertData(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            BootcampS3PerCharacterInstancingRows.DeleteData(migrationBuilder);
        }
    }
}
