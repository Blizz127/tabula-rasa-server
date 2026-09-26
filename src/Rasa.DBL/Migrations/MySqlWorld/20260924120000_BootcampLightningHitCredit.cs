using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class BootcampLightningHitCredit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampLightningHitCreditRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            BootcampData.BootcampLightningHitCreditRows.DeleteData(migrationBuilder);
    }
}
