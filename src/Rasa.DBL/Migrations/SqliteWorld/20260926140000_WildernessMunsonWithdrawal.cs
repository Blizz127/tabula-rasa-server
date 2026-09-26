using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// Dr. Munson's Mighty Miasma (767) withdrawn, as the official Deployment 11 live notes withdraw it.
    /// See <see cref="WildernessMunsonWithdrawalRows"/>.
    /// </summary>
    public partial class WildernessMunsonWithdrawal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => WildernessMunsonWithdrawalRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => WildernessMunsonWithdrawalRows.DeleteData(migrationBuilder);
    }
}
