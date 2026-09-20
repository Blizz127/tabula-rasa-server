using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.BootcampData;

    /// <summary>
    /// The retry of Calling for Reinforcements gets the waypoints its original has.
    /// See <see cref="BootcampRetryIndicatorRows"/>.
    /// </summary>
    public partial class BootcampRetryIndicators : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => BootcampRetryIndicatorRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => BootcampRetryIndicatorRows.DeleteData(migrationBuilder);
    }
}
