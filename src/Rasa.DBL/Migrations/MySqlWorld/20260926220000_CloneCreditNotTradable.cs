using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlWorld
{
    using Rasa.Migrations.WildernessData;

    /// <summary>
    /// The Clone Credit token (item template 111219) is not tradable, as TaRapedia's Clone Credit page records it
    /// (rev 31080, 2008-06-03, unchanged at the final rev 35135, 2008-10-16). Data only.
    /// See <see cref="CloneCreditNotTradableRows"/>.
    /// </summary>
    public partial class CloneCreditNotTradable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => CloneCreditNotTradableRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => CloneCreditNotTradableRows.DeleteData(migrationBuilder);
    }
}
