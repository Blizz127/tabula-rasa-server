using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Four corrections from Ellatha's NPC database (ellatha.com/TR/npcview.asp), a fan site that recorded the live
    /// game and gives each NPC a coordinate. The owner named it as a source on 2026-09-20.
    ///
    /// It agrees with what we already had: of the 53 of its 71 NPCs this world carries, 51 stand within ten metres
    /// of where it puts them, and Cmd. Sgt. Price - placed here from TaRapedia's own /loc - is within a metre. Every
    /// reading used below lands within 1.1 m of the navmesh floor under it, which is what a real in-game reading
    /// does. The two that disagreed are the two this branch had placed as labelled guesses, and they are replaced:
    ///
    ///   Field Lt. Perkins (199008)   guessed at the Pravus north-west fortification; Ellatha: -77, 31, 176
    ///   Lieutenant Seguine (199011)  guessed at Nidu Dav from Brice's /loc;           Ellatha: -556, 162, 426
    ///
    /// Information Spec. Savious (creature 116, spawn pool 194) closes GAP-W3-SAVIOURS-SON-POSITION. 421 "A Father's
    /// Goodbye" sends the player to Lt. Saviours' son across the Lower Eloh Creek bridge and the world seed spawns
    /// him at Ranja Gorge, a kilometre away, where no player would look. Ellatha puts him at 158, 172, 212, in the
    /// Wilderness, and names the same mission.
    ///
    /// Ranger Tirna closes GAP-W3-TIRNA-NAME. Creature 99 carries client name 135, which renders "Ranger Tarina",
    /// while every mission text of 682 calls her Ranger Tirna - the name the client also has, at 6711. The gap was
    /// held open because one source alone was a guess. Ellatha names her Ranger Tirna at -495, 216, 532, which is
    /// this creature's own spawn point to within a metre, so the name and the NPC are the same thing.
    /// </summary>
    public static class EllathaNpcPositionsRows
    {
        public const string Migration = "EllathaNpcPositions";

        private const double Offset = WorldPlacementFloorSnapRows.OriginalSpawnOffset;

        public const uint Perkins = 199008u, Seguine = 199011u;
        public const uint SaviousPool = 194u, TirnaCreature = 99u;
        public const uint TarinaName = 135u, TirnaName = 6711u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            Move(migrationBuilder, Perkins, -77.0, 32.02 + Offset, 176.0);
            Move(migrationBuilder, Seguine, -556.0, 162.40 + Offset, 426.0);

            migrationBuilder.Sql($"update spawnpool set pos_x = 158.0, pos_y = {173.03 + Offset:0.000}, pos_z = 212.0 where id = {SaviousPool};");
            migrationBuilder.Sql($"update creature set name_id = {TirnaName} where id = {TirnaCreature} and name_id = {TarinaName};");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            // The guesses these replaced, and the seed's own rows.
            Move(migrationBuilder, Perkins, -308.0, 16.54 + Offset, -107.0);
            Move(migrationBuilder, Seguine, -770.0, 179.65 + Offset, 612.0);

            migrationBuilder.Sql($"update spawnpool set pos_x = -757.6211, pos_y = 175.03516, pos_z = -277.9297 where id = {SaviousPool};");
            migrationBuilder.Sql($"update creature set name_id = {TarinaName} where id = {TirnaCreature} and name_id = {TirnaName};");
        }

        private static void Move(MigrationBuilder migrationBuilder, uint placement, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: placement, column: "pos_z", value: z);
        }
    }
}
