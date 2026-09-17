using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The NPCs and objects that were not standing on the ground, lifted onto it - and the calibration that
    /// decides where "the ground" is.
    ///
    /// A live report on 2026-09-17 ("thrax infantry is in the ground too") sent every position in the world back
    /// through the navmesh. Measuring the offsets showed the reference itself is not exact: our 107 content
    /// placements sit a median of <b>0.415 m below</b> the walkable surface the navmesh gives - but so does
    /// everything else. The original server's own 217 creature spawn points sit 0.276 m below it, its 357
    /// teleporters 0.150 m below it, and the three characters' own client-reported standing positions 0.08,
    /// 0.31, 0.41 and 0.47 m below it. A player who is demonstrably standing on the floor reads below the
    /// navmesh, so the navmesh reads high on terrain - Recast's walkable surface is the top of a voxel column,
    /// not the terrain under it - and a placement that matches the navmesh exactly is floating.
    ///
    /// The floor is therefore taken to be <c>navmesh surface - 0.276 m</c>, the offset the original server's own
    /// creature spawns sit at: the one reference that is both original and about the same thing, a creature
    /// standing somewhere. That bias is a terrain effect, not a constant - the 1992 supply crate and Captain
    /// Delessio, corrected in 2026-09-15 to the grating platform's top of 122.11 taken from the client's own map
    /// file, match the navmesh there to 0.01 m - so this batch does <b>not</b> re-snap everything. It moves only
    /// the rows that are more than <b>0.5 m</b> off that floor, which is the distance at which a body is no
    /// longer standing on it in any reading of the measurement.
    ///
    /// Fourteen rows qualify. Seven were buried, the worst by 1.66 m, including the boot camp's
    /// <b>Thrax Initiate at the base gate</b>, 0.73 m under - the live report's own sighting, and the shape of
    /// the invisible attacker of the same day, whose shots rendered while its body did not. Seven were floating,
    /// the worst by 0.72 m. Only X and Z carry evidence for these rows (a TaRapedia /loc, a measured camp
    /// position, an OD-48 ring); Y was never recovered for any of them, so setting it to the measured floor
    /// replaces one derived value with a better derived one and changes no sourced coordinate.
    ///
    /// Not moved: content_placement 198677, the bomb, which sits on the wreck's hull and not on the floor.
    /// </summary>
    public static class WorldPlacementFloorSnapRows
    {
        public const string Migration = "WorldPlacementFloorSnap";

        /// <summary>
        /// Where the original server stood a creature, relative to our navmesh: the median of its own 217
        /// spawnpool points. <c>WorldPositionAuditTests</c> measures against this, not against the raw surface.
        /// </summary>
        public const double OriginalSpawnOffset = -0.276;

        /// <summary>How far off that floor a placement of ours may be before it is not standing on it.</summary>
        public const double FloorTolerance = 0.5;

        /// <summary>The bomb is mounted on the wreck's hull (S5, OD-30), so it is not measured against the floor.</summary>
        public const uint MountedBomb = 198677u;

        /// <summary>(placement, the Y it had, the measured floor it is moved to).</summary>
        public static readonly (uint Id, double Was, double Now)[] Rows =
        {
            (198674u, 109.5, 110.23),       // +0.73  Thrax Initiate base_gate.1
            (199206u, 442.0, 442.78),       // +0.78  Colonel Bosley (TaRapedia /loc)
            (199303u, 216.0, 216.73),       // +0.73  Retread Duvall (TaRapedia /loc)
            (199402u, 230.0, 229.46),       // -0.54  Corporal Cooper (TaRapedia /loc)
            (199603u, 222.0, 223.48),       // +1.48  Lt. Gerry (TaRapedia /loc)
            (199821u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199822u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199823u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199824u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199825u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199828u, 225.0, 224.28),       // -0.72  Professor Long's area (OD-48 analogue)
            (199840u, 224.0, 225.08),       // +1.08  Dr. Robertson's area (OD-48 analogue)
            (199864u, 229.0, 230.06),       // +1.06  Colonel Li Hua's area (OD-48 analogue)
            (199865u, 229.0, 230.66)       // +1.66  Colonel Li Hua's area (OD-48 analogue)
        };

        public static void InsertData(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, forward: true);

        public static void DeleteData(MigrationBuilder migrationBuilder) => Apply(migrationBuilder, forward: false);

        private static void Apply(MigrationBuilder migrationBuilder, bool forward)
        {
            foreach (var row in Rows)
                migrationBuilder.UpdateData(
                    table: "content_placement",
                    keyColumn: "id",
                    keyValue: row.Id,
                    column: "pos_y",
                    value: forward ? row.Now : row.Was);
        }
    }
}
