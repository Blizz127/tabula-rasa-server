using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Routes 1390 Conscientious Objector along the path the client's own data can carry.
    ///
    /// WildernessHubConscientiousObjector seeded the mission with the client's eight objectives, but the content
    /// loader refused it: "objective 4 has no completion binding; objective 8 has no completion binding; objective
    /// 12 has no completion binding". The client skeleton holds a conversation binding for objectives 1, 2, 3, 10
    /// and 11 and none for 4 ("Take Milpas to Apirka."), 8 ("Escort Milpas to Divide entrance.") or 12 (the client's
    /// own "no desc"). Those three are the escort and technical steps, whose real completion is not a conversation
    /// and therefore is not in the skeleton at all - an evidence gap, not something to invent.
    ///
    /// So the mission completes through the conversations that do exist: question Elder Quillas (1), answer him
    /// (2 or 3), and speak to Warrior Apirka (10). Objectives 4, 8 and 12 become optional - they stay in the
    /// mission exactly as the client carries them, so a future slice with the escort behaviour can bind them,
    /// while today they neither block the mission nor pretend to work. The transitions now reveal objective 10 from
    /// either answer, since the escort step in between cannot be completed yet.
    ///
    /// Recorded as GAP-W3-1390-ESCORT.
    /// </summary>
    public static class WildernessHubConscientiousObjectorPathRows
    {
        public const string Migration = "WildernessHubConscientiousObjectorPath";

        public const uint ConscientiousObjector = 1390u;

        /// <summary>The escort and technical objectives: no completion binding in the client, so optional.</summary>
        public static readonly uint[] OptionalObjectives = { 4u, 8u, 12u };

        /// <summary>The transitions that make the conversation path complete without the escort step.</summary>
        public static readonly (uint Completed, uint Revealed)[] AddedTransitions = { (2u, 10u), (3u, 10u) };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var objectiveId in OptionalObjectives)
                migrationBuilder.UpdateData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { ConscientiousObjector, objectiveId },
                    column: "is_required",
                    value: false);

            foreach (var (completed, revealed) in AddedTransitions)
                migrationBuilder.InsertData(
                    table: "npc_mission_objective_transition",
                    columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                    values: new object[] { ConscientiousObjector, completed, revealed });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var (completed, revealed) in AddedTransitions)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective_transition",
                    keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                    keyValues: new object[] { ConscientiousObjector, completed, revealed });

            foreach (var objectiveId in OptionalObjectives)
                migrationBuilder.UpdateData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { ConscientiousObjector, objectiveId },
                    column: "is_required",
                    value: true);
        }
    }
}
