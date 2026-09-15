using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.BootcampData
{
    /// <summary>
    /// W3 mechanism: the camp's scripted NPC walks, which the content layer could not express before
    /// (ContentRuleAction.MoveCreatureToLocation).
    ///
    /// Two of the recorded gaps are these moves:
    ///
    /// * <b>GAP-ESCORT</b> - "McAllister does not walk off with the recruit". At the 1990 turn-in he says
    ///   "Follow me over to the ..." (footage A2-044, 285.333) and walks to the S2 gear area, where the crate
    ///   sits at (397.3, 114.0, 173.7) with Delessio and Hartmann. The destination is inferred: the crate
    ///   placement's own position minus a few metres of approach, rated +/-5 m.
    /// * <b>GAP-S5-REINFORCEMENT-MOVE</b> - "The reinforcements walk north-east off the pad by 98.0". The pad
    ///   is area 198603 at (-225.35, 99.6, -70.52); the destination is 15 m north-east of it, inferred
    ///   +/-5 m, matching the recorded direction.
    ///
    /// The triggers use mechanisms already in the content layer: mission 1990 turned in (the same event that
    /// offers 1992 at McAllister) and 1995 objective 1 completed (the wreck destroyed, which is when the squad
    /// has beamed in and moves up).
    /// </summary>
    public static class BootcampScriptedMoveRows
    {
        public const string Migration = "BootcampScriptedMoves";

        public const uint McAllisterEscortDestination = 19853u;
        public const uint ReinforcementWalkOffDestination = 19854u;

        public const uint McAllisterEscortRule = 1985014u;
        public const uint ReinforcementWalkOffRule = 1985015u;

        private const uint McAllisterPlacement = 198650u;
        private const uint InfantrymanL2Placement = 198681u;
        private const uint InfantrymanL3Placement = 198682u;
        private const uint ForeanGunnerPlacement = 198680u;

        private const uint Mission1990 = 1990u;
        private const uint Mission1995 = 1995u;

        private const byte ScriptedMoveDestination = 3;
        private const byte MissionTurnedIn = 6;
        private const byte ObjectiveCompleted = 3;
        private const byte MoveCreatureToLocation = 14;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "content_location",
                columns: new[] { "id", "purpose", "map_context_id", "pos_x", "pos_y", "pos_z", "rotation", "comment" },
                values: new object[,]
                {
                    // Inferred: the approach to the S2 gear crate (397.3, 114.0, 173.7).
                    { McAllisterEscortDestination, ScriptedMoveDestination, 1985, 390.0, 114.0, 172.0, 0.0,
                      "McAllister escort destination (inferred)" },
                    // Inferred: 15 m north-east of the dropship pad (-225.35, 99.6, -70.52).
                    { ReinforcementWalkOffDestination, ScriptedMoveDestination, 1985, -215.0, 100.0, -62.0, 0.0,
                      "reinforcement walk-off destination (inferred)" }
                });

            migrationBuilder.InsertData(
                table: "content_rule",
                columns: new[] { "id", "map_context_id", "event", "mission_id", "objective_id", "area_id", "placement_id", "state_id", "condition_id", "comment" },
                values: new object[,]
                {
                    { McAllisterEscortRule, 1985, MissionTurnedIn, Mission1990, 0u, 0u, 0u, 0u, 0u, "1990 turned in -> McAllister walks to the gear" },
                    { ReinforcementWalkOffRule, 1985, ObjectiveCompleted, Mission1995, 1u, 0u, 0u, 0u, 0u, "1995/1 -> reinforcements leave the pad" }
                });

            var move = new[]
            {
                "rule_id", "sequence", "action", "mission_id", "forced", "greeting_id", "npc_name_id", "tutorial_id",
                "logos_id", "logos_protocol", "experience", "credits", "item_set_id", "placement_id", "state_id",
                "fact_key", "fact_value", "location_id", "audio_set_id", "comment"
            };

            migrationBuilder.InsertData(
                table: "content_rule_action",
                columns: move,
                values: new object[]
                {
                    McAllisterEscortRule, (byte)0, MoveCreatureToLocation, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0, 0u,
                    McAllisterPlacement, 0u, string.Empty, 0, McAllisterEscortDestination, 0u, "McAllister walks to the gear"
                });

            // One action per reinforcement; the sequence column is the action index within the rule.
            var sequence = 0;
            foreach (var placement in new[] { InfantrymanL2Placement, InfantrymanL3Placement, ForeanGunnerPlacement })
                migrationBuilder.InsertData(
                    table: "content_rule_action",
                    columns: move,
                    values: new object[]
                    {
                        ReinforcementWalkOffRule, (byte)sequence++, MoveCreatureToLocation, 0u, false, 0u, 0u, 0u, 0u, (byte)0, 0u, 0,
                        0u, placement, 0u, string.Empty, 0, ReinforcementWalkOffDestination, 0u, "reinforcement leaves the pad"
                    });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "content_rule_action",
                keyColumns: new[] { "rule_id", "sequence" },
                keyValues: new object[,]
                {
                    { McAllisterEscortRule, (byte)0 },
                    { ReinforcementWalkOffRule, (byte)0 },
                    { ReinforcementWalkOffRule, (byte)1 },
                    { ReinforcementWalkOffRule, (byte)2 }
                });

            migrationBuilder.DeleteData(
                table: "content_rule",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { McAllisterEscortRule }, { ReinforcementWalkOffRule } });

            migrationBuilder.DeleteData(
                table: "content_location",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { McAllisterEscortDestination }, { ReinforcementWalkOffDestination } });
        }
    }
}
