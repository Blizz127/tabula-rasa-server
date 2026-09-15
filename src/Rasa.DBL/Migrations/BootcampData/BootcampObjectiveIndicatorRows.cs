using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// Gives mission 1990's two objectives the world markers the footage shows.
    ///
    /// The position records for the two objective areas note "the yellow objective icon (probable indicator 430)"
    /// while 1990 objective 1 was active and "(probable indicator 431)" while objective 2 was active, both read
    /// from the verified radar frames of 7Lrst9SG3pk. Neither had an `npc_mission_objective_indicator` row, so in
    /// play the recruit had nothing to walk towards: the areas are 4 m triggers on a 97 m bridge, which is why
    /// "approached the Eloh hologram and nothing happened" was the first live report of the S1 slice.
    ///
    /// The marker positions are the recorded ones (X,Z; +/-1.5 m and +/-2.0 m) with Y taken from the ceremonial
    /// bridge's own map entity (ArchForeanBridgeCeremonialV04, Y 131.3): the recorded Y values are the terrain
    /// *under* the bridge, 13-19 m below the deck the recruit walks on, so a marker there would hang under the
    /// span. The area triggers themselves keep their recorded Y and reach the deck through their vertical extent.
    /// </summary>
    public static class BootcampObjectiveIndicatorRows
    {
        public const string Migration = "BootcampObjectiveIndicators";

        /// <summary>1990 objective 1: indicator 430 at the footage's objective-1 marker.</summary>
        public const uint Objective1Indicator = 430u;

        /// <summary>1990 objective 2: indicator 431 at the footage's objective-2 marker.</summary>
        public const uint Objective2Indicator = 431u;

        private const uint Mission = 1990u;

        /// <summary>The bridge deck the recruit crosses, from ArchForeanBridgeCeremonialV04.</summary>
        private const double DeckY = 131.3;

        private static readonly (uint ObjectiveId, uint IndicatorId, double X, double Z)[] Markers =
        {
            (1u, Objective1Indicator, 388.95, -26.28),
            (2u, Objective2Indicator, 387.83, 6.71)
        };

        private static readonly string[] Columns =
        {
            "mission_id", "objective_id", "indicator_index", "indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d"
        };

        private static readonly string[] ColumnTypes =
        {
            "INTEGER", "INTEGER", "INTEGER", "INTEGER", "REAL", "REAL", "REAL", "REAL", "INTEGER"
            // show_3d is a bool in the model
            
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var (objectiveId, indicatorId, x, z) in Markers)
                migrationBuilder.InsertData(
                    table: "npc_mission_objective_indicator",
                    columns: Columns,
                    columnTypes: ColumnTypes,
                    values: new object[] { Mission, objectiveId, (byte)0, indicatorId, x, DeckY, z, 0.0, false });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (objectiveId, _, _, _) in Markers)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective_indicator",
                    keyColumns: new[] { "mission_id", "objective_id", "indicator_index" },
                    keyValues: new object[] { Mission, objectiveId, 0u });
        }
    }
}
