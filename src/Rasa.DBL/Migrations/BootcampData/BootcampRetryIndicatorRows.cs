using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// The retry of "Calling for Reinforcements" (2005) gets the waypoints its original has.
    ///
    /// 1995 carries an indicator on objective 1 (the wreck, BootcampS5Reinforcements) and on objective 4 (checking
    /// in with Corporal Van Valkenberg, BootcampS6ExitToAliaDas). 2005 repeats those two objectives and was seeded
    /// with neither, so a player who takes the retry watches a 600 s timer run down with nothing on the map to walk
    /// towards - which is how it was found in play: "its counting down but no way to finish the quest" (2026-09-20),
    /// from a character standing at Youngblood's camp, 350 m from the wreck.
    ///
    /// The positions are 1995's own, measured; the indicator ids are inferred, as 1995's are.
    /// </summary>
    public static class BootcampRetryIndicatorRows
    {
        public const string Migration = "BootcampRetryIndicators";

        public const uint Retry = 2005u;

        public static void InsertData(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData(
                table: "npc_mission_objective_indicator",
                columns: new[] { "mission_id", "objective_id", "indicator_index", "indicator_id", "pos_x", "pos_y", "pos_z", "radius", "show_3d" },
                values: new object[,]
                {
                    { Retry, 1u, (byte)0, 432u, -223.26, 100.72, -65.7, 0.0, false },
                    { Retry, 4u, (byte)0, 438u, -225.53, 100.8, -69.81, 0.0, true }
                });

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var objectiveId in new uint[] { 1u, 4u })
                migrationBuilder.DeleteData(table: "npc_mission_objective_indicator",
                    keyColumns: new[] { "mission_id", "objective_id", "indicator_index" },
                    keyValues: new object[] { Retry, objectiveId, (byte)0 });
        }
    }
}
