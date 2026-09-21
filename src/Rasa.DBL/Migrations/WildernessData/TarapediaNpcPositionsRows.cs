using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// 20 NPCs moved to where TaRapedia's own location table puts them.
    ///
    /// The 2026-09-21 sweep read the wiki's complete pre-shutdown history (3,516 pages, last revision before
    /// 2009-03-01) rather than the handful of pages cited so far, and found 165 NPC pages carrying a location
    /// table with coordinates. Against this world: 45 stand within 25 m of the reading, which corroborates them;
    /// 24 are further than that; 62 the wiki documents are not here at all; and three sit on a different map
    /// from the zone the wiki names.
    ///
    /// The ones moved here are the far ones. All but a few are upstream's mission NPCs, whose positions upstream
    /// derived from the sentence of a mission ("the mission NPCs whose place a mission sentence states"), while
    /// the wiki gives a coordinate with the revision date it was set on. A dated reading beats a derived one, so
    /// the wiki's X and Z are taken. Y is the navmesh floor under them rather than the wiki's own: 21 of the 23
    /// readings are whole metres, and against the floor they measure a median of 0.12 m low, 22 of 23 within a
    /// metre - close enough to corroborate the reading, too coarse to stand a body on.
    ///
    /// The clearest of them is Brigadier General Beacham, 57 m from the wiki's reading and away from Outpost
    /// Commander Rogers, whom the same wiki places next to him at the Alia Das command post; and the Irendas
    /// Penal Colony group, which stood about 200 m east of the colony.
    ///
    /// Left alone, with the reason:
    ///   * <b>Lieutenant Burke</b> (pool 171) and <b>Council Elder Solis</b> (184) are the original server's own
    ///     spawn rows. An original coordinate outranks a community transcription of one, so a wiki reading is
    ///     not grounds to move them - it is grounds to write the disagreement down, which is done here.
    ///   * <b>Corporal Orton</b> (510196) stands on the Pools map while the wiki puts him in the Palisades:
    ///     that is a map change, not a move, and it needs its own evidence.
    ///   * <b>Ranger Cyrida</b>, whose wiki reading (38, 154, 48) has no walkable ground under it while ours
    ///     (-46, 154, 48) does - the same Y and the same Z, so the wiki's X looks to have lost its sign.
    ///   * <b>Dr. Eleanor Corman</b>, whose wiki row reads 0, 0, 0.
    ///   * <b>Lieutenant Donners</b>, 17 m from the reading but inside the Kardash Atta Colony sub-map rather
    ///     than on the Plains map the zone names.
    ///   * <b>Medic Campbell</b>, where the name is shared by two creatures and ours is a Thunderhead class
    ///     trainer, not the Foreas Base medic the wiki describes.
    /// </summary>
    public static class TarapediaNpcPositionsRows
    {
        public const string Migration = "TarapediaNpcPositions";

        /// <summary>(id, whether it is a content placement rather than a spawn pool, the x,y,z it had, the x,y,z it takes).</summary>
        public static readonly (uint Id, bool Placement, double WasX, double WasY, double WasZ, double X, double Y, double Z)[] Rows =
        {
            (510136u, false, -32.700, 118.599, 390.800, -43.000, 153.558, 40.000), // Thomas Jansona, 351.0 m from the wiki reading; WorldFloorSweep moved it first
            (520036u, false, -505.000, 217.400, 51.000, -501.000, 213.602, -213.000), // Seymour, 264.0 m from the wiki reading
            (510086u, false, 383.000, 433.200, -98.500, 211.000, 425.324, -230.000), // Dr. Finobee, 216.5 m from the wiki reading
            (510173u, false, -186.300, 423.680, 869.500, 19.000, 399.602, 925.000), // Bartender McLaughlin, 212.7 m from the wiki reading
            (510090u, false, 377.000, 433.200, -107.000, 207.000, 425.324, -233.000), // Lieutenant Commander Michan, 211.6 m from the wiki reading
            (510082u, false, 382.300, 433.200, -103.900, 210.000, 425.324, -215.000), // Agent Zim, 205.0 m from the wiki reading
            (510093u, false, 381.600, 433.200, -110.800, 204.000, 433.124, -133.000), // Master Salvager Miru, 179.0 m from the wiki reading
            (510192u, false, -186.300, 423.480, 864.100, -61.000, 402.402, 940.000), // Senior Engineer Mauer, 146.5 m from the wiki reading
            (510178u, false, 482.300, 426.420, 430.000, 363.000, 369.202, 360.000), // Comm Officer Devinchy, 138.3 m from the wiki reading
            (510116u, false, -55.100, 116.110, 460.500, 19.100, 116.724, 538.400), // Spec. Kerr, 107.6 m from the wiki reading
            (510180u, false, -186.300, 423.420, 874.900, -92.000, 399.396, 914.000), // Commander Grissom, 102.1 m from the wiki reading
            (510094u, false, 385.400, 433.200, -109.200, 287.000, 432.924, -100.000), // Senior Quartermaster Hacienda, 98.8 m from the wiki reading
            (510184u, false, -191.700, 423.680, 869.500, -105.000, 399.291, 913.000), // Lieutenant Colonel Doss, 97.0 m from the wiki reading
            (510185u, false, 339.200, 363.166, 647.600, 292.000, 347.002, 563.000), // Loremaster Talisen, 96.9 m from the wiki reading; WorldFloorSweep moved it first
            (510189u, false, 339.200, 366.146, 638.800, 286.000, 349.468, 569.000), // Ranger Kaely, 87.8 m from the wiki reading; WorldFloorSweep moved it first
            (510175u, false, -182.500, 419.280, 873.300, -230.000, 420.802, 938.000), // CID Detective Crais, 80.3 m from the wiki reading
            (510186u, false, 334.800, 365.640, 643.200, 259.000, 351.802, 619.000), // Luminary Sampei, 79.6 m from the wiki reading
            (510179u, false, 477.100, 425.670, 428.300, 466.000, 378.002, 359.000), // Comm Officer McKinley, 70.2 m from the wiki reading
            (510002u, false, 812.900, 294.690, 388.200, 870.000, 294.042, 389.000), // Brigadier General Beacham, 57.1 m from the wiki reading
            (510063u, false, 396.900, 258.715, 33.000, 381.000, 260.054, 9.000), // Colonel Deiley, 28.8 m from the wiki reading; WorldFloorSweep moved it first
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                Move(migrationBuilder, row.Placement ? "content_placement" : "spawnpool", row.Id, row.X, row.Y, row.Z);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                Move(migrationBuilder, row.Placement ? "content_placement" : "spawnpool", row.Id, row.WasX, row.WasY, row.WasZ);
        }

        private static void Move(MigrationBuilder migrationBuilder, string table, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
