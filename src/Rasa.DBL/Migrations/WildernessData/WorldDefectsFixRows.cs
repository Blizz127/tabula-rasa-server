using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Five bodies the world had in the wrong place, one hostile boss standing on a quest receiver, and one
    /// Colonel Whitaker too many: the corrections of the 2026-09-26 world-data audits
    /// (research/20260926-world-defects, research/20260926-torden-missions "World-data defects").
    ///
    /// <b>Field Lt. Bagby and Lt. Galloway (placements 199106, 199107) are in the Treeback Camp instance.</b>
    /// PalisadesConversationNpc put them on the Palisades overworld (1244) at TaRapedia's /loc, and WorldFloorSweep
    /// then lifted them 34 and 40 m, on the reading that the wiki gave x and z only. It did not: both pages give a
    /// Y, and both pages' infoboxes say <c>Instance=Treeback Camp</c> (Bagby pageid 9003 rev 22867, 2007-12-20;
    /// Galloway pageid 9010 rev 22879, 2007-12-20). The final client's own mission log says so too - 1788 "deliver a
    /// bottle of gin to Field Lt. Bagby at the Treeback Camp", 1789 "Lt. Galloway, who is currently operating out
    /// of the Treeback Camp" - and the post-D11 Treeback Camp page (pageid 6829 rev 32558, 2008-08-30) lists both
    /// as its important NPCs. On the Treeback Camp navmesh (adv_foreas_concordia_palisades_treebackcamp, 1397)
    /// the wiki's own y is 0.12 m and 0.02 m off the floor; on the overworld the same x,z is 34-40 m under the
    /// terrain. So the map was the error, not the Y: both go to 1397 at the wiki's x,y,z. The map is inferred
    /// (high), the position inferred (pre-D11 readings, GAP-TREEBACK-POSITIONS-PRE-D11). Galloway is "mobile" and
    /// meets the player at the entrance shield after in-instance events; a stationary body approximates that
    /// (GAP-TREEBACK-GALLOWAY-SCRIPTED). WorldFloorSweep's rows are not edited - this migration supersedes them,
    /// so Down returns both to where WorldFloorSweep left them.
    ///
    /// <b>Spawn pool 520046 "Warnet Queen - Palisades" is removed.</b> Upstream's boss import (InfiniteRasa
    /// 492954a, Add_boss_spawns) stood a generic level-28 hostile Warnet Queen (client name 461) exactly on the
    /// Divide's "Foreas Base" static map label, 0.5 m from Receptive Liaison Noonan, the receiver of seeded 1743.
    /// No source puts a Warnet Queen at Foreas Base, and none gives an overworld position for one in the
    /// Palisades: TaRapedia's post-D11 Palisades Targets of Opportunity (rev 35441, 2008-10-26) lists nine
    /// important Bane and no queen, and the only Palisades queen on record is the Queen Warnet Matriarch (name
    /// 8581), a different creature lured out of Warnet Caverns by mission 396. It is left out rather than moved
    /// (GAP-PALISADES-WARNET-QUEEN). The pool is seeded by a migration's preloader, which the deployed world has
    /// already run, so it is removed here; the creature and its stats row stay, unused.
    ///
    /// <b>Three receivers off the map markers upstream stacked them on.</b> Upstream placed its mission NPCs "at
    /// the map marker of that place" (MissionNpcSpawnpoolPreloader). Where a dated reading exists it is taken:
    ///   * Valerie Corman (510137): TaRapedia "A Casualty Notification" rev 33050/33051 (2008-09-13, post-D11)
    ///     "Valerie Corman Location: -772.7, 140.3, 774.9", and codex-tr.net POI 1425 (-774, 771) captured
    ///     2008-01-12, two independent sources 4.1 m apart. x,z inferred (high); y the navmesh floor, measured.
    ///   * Ranger Jorai (510133): TaRapedia "Ranger Jorai" rev 16456 (2007-11-26) and "Revealing Treeback
    ///     Experimentation" rev 22240 (2007-12-18), "-294,-577", inside the Forean dwellings of Viands Village.
    ///     Pre-D11 only, so medium confidence (GAP-JORAI-PRE-D11-LOC); y the navmesh floor, measured.
    ///   * Lieutenant Epp (510068): TaRapedia "Lieutenant Epp" rev 35625/35626 (2008-11-11, post-D11) added
    ///     "Location -180 235 -244" to the page's standing text "in the Nyxroq Post hospital"; the final client's
    ///     "Hospital: Nyxroq Post" marker is 2.2 m from it. x,z inferred; y the navmesh floor, measured.
    /// The other marker-stacked NPCs of the two audits are left where they are, each with its gap: Kearney
    /// (510118) and Mela (510117), for which no positional source exists; and Clark, Norton, Perdu, Obahmi,
    /// Franks, Foletto and Nicholson, for which no post-D11 reading exists - the "2008-09-25" coordinates of Clark
    /// and Norton are a table-template reformat of their pages' 2007-08-17 beta readings, not a new reading.
    ///
    /// <b>The second Colonel Whitaker stops spawning.</b> Spawn pool 510085 (upstream, at the Irendas "Crafting
    /// Stations" marker) drew a Whitaker 107 m from placement 199500, which stands at TaRapedia's one Whitaker
    /// reading ("Up in the Headquarters tower", 320/499/-47) and gives seeded 640. QuestWiringFixes already dropped
    /// the pool's duplicate npc_package row. Its counts go to 0/0, the way SolisCavernsPlacement retired pool 92,
    /// so the creature row (client name 5428) stays: the client has two "Colonel Whitaker" names, 5428 and 8919,
    /// and which one the Irendas Whitaker used is not recorded (GAP-WHITAKER-NAME-ID).
    ///
    /// <b>Lieutenant Liu's comment</b> said "(Irendas Penal Colony)"; she stands at her /loc in the Eir Crater
    /// Field Hospital. Cosmetic.
    /// </summary>
    public static class WorldDefectsFixRows
    {
        public const string Migration = "WorldDefectsFix";

        public const uint TreebackCamp = 1397u, Palisades = 1244u;

        /// <summary>(placement, the map and x,y,z it had after WorldFloorSweep, the map and x,y,z it takes).</summary>
        public static readonly (uint Id, uint WasMap, double WasX, double WasY, double WasZ, uint Map, double X, double Y, double Z)[] Placements =
        {
            (199106u, Palisades, -337.2, 137.794, 353.7, TreebackCamp, -337.2, 103.6, 353.7),   // Field Lt. Bagby, "just inside entrance"
            (199107u, Palisades, -122.2, 140.442, 128.2, TreebackCamp, -122.2, 100.3, 128.2),   // Lt. Galloway, "close to the entrance shield"
        };

        /// <summary>
        /// (pool, the x,y,z it had, the x,y,z it takes). 510137 and 510133 still hold the preloader's single-precision
        /// values, so their old values are written as those floats; WorldFloorSweep rewrote 510068 in double precision.
        /// </summary>
        public static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Y, double Z)[] Pools =
        {
            (510137u, -515.0f, 141.62f, 701.5f, -772.7, 140.17, 774.9),       // Valerie Corman, off "Cumbria Research Facility"
            (510133u, -235.1f, 178.17f, -543.8f, -294.0, 172.707, -577.0),    // Ranger Jorai, off "Viands Village"
            (510068u, -238.3, 233.758, -253.5, -180.0, 235.229, -244.0),      // Lieutenant Epp, off "Waypoint: Nyxroq Post"
        };

        public const uint WarnetQueenPool = 520046u;

        public static readonly string[] SpawnPoolColumns =
        {
            "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
            "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
            "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
            "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
            "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
            "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
            "creature_6_Id", "creature_6_min_count", "creature_6_max_count"
        };

        /// <summary>BossSpawnpoolPreloader's row for 520046, exactly as Add_boss_spawns inserted it.</summary>
        public static readonly object[] WarnetQueenRow =
            { 520046, 0, 0, 500, -42.3000f, 116.3000f, 478.7000f, 0, 1148, 520046, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public const uint WhitakerDuplicatePool = 510085u;

        public const uint Liu = 199510u;
        public const string LiuCreatureWas = "Lieutenant Liu (Irendas Penal Colony)";
        public const string LiuCreatureNow = "Lieutenant Liu (Eir Crater Field Hospital)";
        public const string LiuPlacementWas = "Lieutenant Liu (Irendas Penal Colony) (TaRapedia /loc)";
        public const string LiuPlacementNow = "Lieutenant Liu (Eir Crater Field Hospital) (TaRapedia /loc)";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Placements)
                Place(migrationBuilder, row.Id, row.Map, row.X, row.Y, row.Z);

            foreach (var row in Pools)
                Move(migrationBuilder, row.Id, row.X, row.Y, row.Z);

            migrationBuilder.DeleteData(table: "spawnpool", keyColumn: "id", keyValue: WarnetQueenPool);

            Counts(migrationBuilder, WhitakerDuplicatePool, 0);

            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: Liu, column: "comment", value: LiuCreatureNow);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: Liu, column: "comment", value: LiuPlacementNow);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: Liu, column: "comment", value: LiuPlacementWas);
            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: Liu, column: "comment", value: LiuCreatureWas);

            Counts(migrationBuilder, WhitakerDuplicatePool, 1);

            migrationBuilder.InsertData(table: "spawnpool", columns: SpawnPoolColumns, values: WarnetQueenRow);

            foreach (var row in Pools)
                Move(migrationBuilder, row.Id, row.WasX, row.WasY, row.WasZ);

            foreach (var row in Placements)
                Place(migrationBuilder, row.Id, row.WasMap, row.WasX, row.WasY, row.WasZ);
        }

        private static void Place(MigrationBuilder migrationBuilder, uint id, uint map, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: id, column: "map_context_id", value: map);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "content_placement", keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }

        private static void Move(MigrationBuilder migrationBuilder, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }

        private static void Counts(MigrationBuilder migrationBuilder, uint id, byte count)
        {
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "creature_1_min_count", value: count);
            migrationBuilder.UpdateData(table: "spawnpool", keyColumn: "id", keyValue: id, column: "creature_1_max_count", value: count);
        }
    }
}
