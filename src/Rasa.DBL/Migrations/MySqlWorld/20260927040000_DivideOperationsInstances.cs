using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    public partial class DivideOperationsInstances : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.DivideOperationsInstancesRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.DivideOperationsInstancesRows.DeleteData(migrationBuilder);
    }
}
