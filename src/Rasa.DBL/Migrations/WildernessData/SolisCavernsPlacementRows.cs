using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// TaRapedia's dated Council Elder Solis location (784.7, 287.2, 581.1) and a
    /// second location pin (781, 577) agree on the Alia Caverns side of Alia Das.
    /// Original-seed pool 92 already places an unnamed Forean shaman 2.2 m from
    /// the dated report at the same elevation. The named Solis in pool 184 is
    /// obstructed by the stonework of Moawi's hut in the compatibility client.
    ///
    /// Moving pool 184 onto pool 92's original X/Z/rotation and the probed
    /// navmesh floor is an inferred identity binding, not a recovered final-live
    /// server placement. Pool 92 is disabled to avoid duplicating that body.
    /// The provenance and remaining uncertainty are in
    /// docs/evidence/solis-caverns-placement.json.
    /// </summary>
    public static class SolisCavernsPlacementRows
    {
        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE spawnpool SET pos_x = 786.8711, pos_y = 287.32, pos_z = 581.46875, rotation = 3.0 WHERE id = 184");
            migrationBuilder.Sql(
                "UPDATE spawnpool SET creature_1_min_count = 0, creature_1_max_count = 0 WHERE id = 92");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE spawnpool SET pos_x = 809.3008, pos_y = 302.09375, pos_z = 503.76562, rotation = 5.54 WHERE id = 184");
            migrationBuilder.Sql(
                "UPDATE spawnpool SET creature_1_min_count = 1, creature_1_max_count = 1 WHERE id = 92");
        }
    }
}
