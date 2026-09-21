using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Phost'Benon, one of the 51 created in <see cref="TarapediaMissingNpcBatchRows"/>, onto the floor above him.
    ///
    /// The wiki puts him at -282, 220, 71 in the Mires, and the navmesh has two walkable levels in that column:
    /// one at 227.56 and one at 229.39. The probe took the wiki's own Y as its hint and so found the lower, which
    /// left him reading as 1.83 m under the upper - the same shape of mistake Ranger Kaely's row made in
    /// <see cref="WorldSweepCorrectionsRows"/>, and caught the same way, by the audits rather than by eye.
    /// </summary>
    public static class PhostBenonFloorRows
    {
        public const string Migration = "PhostBenonFloor";

        public const uint PhostBenon = 199034u;
        public const double Was = 227.280, Now = 229.114;

        public static void InsertData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: PhostBenon,
                column: "pos_y", value: Now);

        public static void DeleteData(MigrationBuilder migrationBuilder)
            => migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: PhostBenon,
                column: "pos_y", value: Was);
    }
}
