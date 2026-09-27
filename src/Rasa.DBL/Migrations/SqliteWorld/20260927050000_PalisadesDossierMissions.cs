using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Concordia Palisades (map 1244), the dossier's seedable missions: 1812 Logos: True, 1813 Logos: Through, 1988 A
    /// Spiritual Pilgrimage, 2014 Crash Course and 1795 Bloody Booty, Derac's and Matlin's packages, package 134 moved to
    /// the Palisades Corporal Orton, Gantic's and Barbrix's respawn and teleporter 624's description. See
    /// <see cref="WildernessData.PalisadesDossierMissionsRows"/>.
    /// </summary>
    public partial class PalisadesDossierMissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            WildernessData.PalisadesDossierMissionsRows.InsertData(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder) =>
            WildernessData.PalisadesDossierMissionsRows.DeleteData(migrationBuilder);
    }
}
