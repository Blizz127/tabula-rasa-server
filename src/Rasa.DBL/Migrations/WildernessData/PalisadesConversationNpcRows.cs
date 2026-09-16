using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Palisades's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (15).
    /// </summary>
    public static class PalisadesConversationNpcRows
    {
        public const string Migration = "PalisadesConversationNpc";

        private const uint GeneralCategory = 10000001u;
        private const uint ExperienceReward = 3u;
        private const uint CreditsReward = 1u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the NPCs ──
            migrationBuilder.InsertData(
                table: "creature",
                columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed", "walk_speed",
                    "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                values: new object[,]
                {
                    { 199100u, "Ranger Kogari", 3846u, 1u, 20u, 555u, 2955u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199101u, "Ranger Urialia", 3846u, 1u, 20u, 555u, 2961u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199102u, "Warden Brocail", 3846u, 1u, 15u, 555u, 2947u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199103u, "Warden Lagori", 3846u, 1u, 15u, 555u, 2948u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199104u, "Warden Kahlee", 3846u, 1u, 15u, 555u, 2949u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199105u, "Field Lt. Brody", 3846u, 1u, 15u, 555u, 2946u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199106u, "Field Lt. Bagby", 3846u, 1u, 20u, 555u, 2964u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199107u, "Lt. Galloway", 3846u, 1u, 23u, 555u, 6705u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199100u, 44u, "Ranger Kogari (client conversation package)" },
                    { 199101u, 154u, "Ranger Urialia (client conversation package)" },
                    { 199102u, 40u, "Warden Brocail (client conversation package)" },
                    { 199103u, 42u, "Warden Lagori (client conversation package)" },
                    { 199104u, 41u, "Warden Kahlee (client conversation package)" },
                    { 199105u, 550u, "Field Lt. Brody (client conversation package)" },
                    { 199106u, 46u, "Field Lt. Bagby (client conversation package)" },
                    { 199107u, 565u, "Lt. Galloway (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199100u, 1244u, (byte)1, 199100u, 44u, 0u, (byte)0, -42.0, 153.0, 42.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Ranger Kogari (TaRapedia /loc)" },
                    { 199101u, 1244u, (byte)1, 199101u, 154u, 0u, (byte)0, -230.0, 166.0, -462.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Ranger Urialia (TaRapedia /loc)" },
                    { 199102u, 1244u, (byte)1, 199102u, 40u, 0u, (byte)0, -531.0, 186.0, -41.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Warden Brocail (TaRapedia /loc)" },
                    { 199103u, 1244u, (byte)1, 199103u, 42u, 0u, (byte)0, -439.0, 166.0, 37.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Warden Lagori (TaRapedia /loc)" },
                    { 199104u, 1244u, (byte)1, 199104u, 41u, 0u, (byte)0, -694.0, 192.0, -91.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Warden Kahlee (TaRapedia /loc)" },
                    { 199105u, 1244u, (byte)1, 199105u, 550u, 0u, (byte)0, -570.0, 186.0, 170.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Lt. Brody (TaRapedia /loc)" },
                    { 199106u, 1244u, (byte)1, 199106u, 46u, 0u, (byte)0, -337.2, 103.6, 353.7, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Lt. Bagby (TaRapedia /loc)" },
                    { 199107u, 1244u, (byte)1, 199107u, 565u, 0u, (byte)0, -122.2, 100.3, 128.2, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Lt. Galloway (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 366u, 199100u, 199100u, 15u, 1u, GeneralCategory, false, false, "Proving Justice (Palisades)" },
                    { 367u, 199101u, 199101u, 15u, 1u, GeneralCategory, false, false, "Proving Conservation (Palisades)" },
                    { 368u, 199100u, 199100u, 15u, 1u, GeneralCategory, false, false, "Searching for Acceptance (Palisades)" },
                    { 411u, 199102u, 199102u, 15u, 1u, GeneralCategory, false, false, "Proven Commitment (Palisades)" },
                    { 412u, 199103u, 199103u, 15u, 1u, GeneralCategory, false, false, "Proven Justice (Palisades)" },
                    { 413u, 199104u, 199104u, 15u, 1u, GeneralCategory, false, false, "Proven Conservation (Palisades)" },
                    { 670u, 199105u, 199105u, 15u, 1u, GeneralCategory, false, false, "Missing Strike Team (Palisades)" },
                    { 1788u, 199106u, 199106u, 15u, 1u, GeneralCategory, false, false, "A Bottle of the Good Stuff (Palisades)" },
                    { 1789u, 199107u, 199107u, 15u, 1u, GeneralCategory, false, false, "Treeback Field Report (Palisades)" }
                });

            // 366 Proving Justice
            ReplaceObjectives(migrationBuilder, 366u, new[]
            {
                (1u, "Meet With Ranger Kogari", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (366u, ExperienceReward, 9500u), (366u, CreditsReward, 1000u) });

            // 367 Proving Conservation
            ReplaceObjectives(migrationBuilder, 367u, new[]
            {
                (1u, "Speak with Ranger Urialia", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (367u, ExperienceReward, 9500u), (367u, CreditsReward, 1000u) });

            // 368 Searching for Acceptance
            ReplaceObjectives(migrationBuilder, 368u, new[]
            {
                (1u, "Kill Warnets", true, 1u, true),
                (2u, "Searching For Acceptance", true, 2u, false)
            });
            AddChain(migrationBuilder, 368u, new uint[] { 1u, 2u });
            AddRewards(migrationBuilder, new[] { (368u, ExperienceReward, 19000u), (368u, CreditsReward, 2850u) });

            // 411 Proven Commitment
            ReplaceObjectives(migrationBuilder, 411u, new[]
            {
                (1u, "Return to Warden Brocail at the Temple of the Bowed Patriarch", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (411u, ExperienceReward, 9000u), (411u, CreditsReward, 900u) });

            // 412 Proven Justice
            ReplaceObjectives(migrationBuilder, 412u, new[]
            {
                (1u, "Return to Warden Lagori at the Temple of the Raging Patriarch", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (412u, ExperienceReward, 9500u), (412u, CreditsReward, 1000u) });

            // 413 Proven Conservation
            ReplaceObjectives(migrationBuilder, 413u, new[]
            {
                (1u, "Return to Warden Kahlee at the Temple of the Proud Patriarch", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (413u, ExperienceReward, 20000u), (413u, CreditsReward, 2000u) });

            // 670 Missing Strike Team
            ReplaceObjectives(migrationBuilder, 670u, new[]
            {
                (1u, "Find the missing strike team.", true, 1u, true),
                (2u, "Return to Field Lt. Brody with information about the first Strike Team", true, 2u, false)
            });
            AddChain(migrationBuilder, 670u, new uint[] { 1u, 2u });
            AddRewards(migrationBuilder, new[] { (670u, ExperienceReward, 40000u) });

            // 1788 A Bottle of the Good Stuff
            ReplaceObjectives(migrationBuilder, 1788u, new[]
            {
                (1u, "Find Lt. Bagby in the Treeback Camp", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1788u, ExperienceReward, 9000u), (1788u, CreditsReward, 1800u) });

            // 1789 Treeback Field Report
            ReplaceObjectives(migrationBuilder, 1789u, new[]
            {
                (1u, "Deliver the Report to Galloway", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1789u, ExperienceReward, 9000u), (1789u, CreditsReward, 1800u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 366u);
            RemoveMission(migrationBuilder, 367u);
            RemoveMission(migrationBuilder, 368u);
            RemoveMission(migrationBuilder, 411u);
            RemoveMission(migrationBuilder, 412u);
            RemoveMission(migrationBuilder, 413u);
            RemoveMission(migrationBuilder, 670u);
            RemoveMission(migrationBuilder, 1788u);
            RemoveMission(migrationBuilder, 1789u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199100u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199101u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199102u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199103u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199104u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199105u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199106u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199107u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199100u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199101u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199102u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199103u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199104u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199105u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199106u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199107u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199100u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199101u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199102u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199103u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199104u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199105u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199106u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199107u });
        }

        private static void ReplaceObjectives(MigrationBuilder migrationBuilder, uint missionId,
            (uint ObjectiveId, string Text, bool Required, uint Ordinal, bool Revealed)[] objectives)
        {
            foreach (var objective in objectives)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { missionId, objective.ObjectiveId });

            var values = new object[objectives.Length, 6];
            for (var i = 0; i < objectives.Length; i++)
            {
                values[i, 0] = missionId;
                values[i, 1] = objectives[i].ObjectiveId;
                values[i, 2] = objectives[i].Text;
                values[i, 3] = objectives[i].Required;
                values[i, 4] = objectives[i].Ordinal;
                values[i, 5] = objectives[i].Revealed;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: values);
        }

        private static void AddChain(MigrationBuilder migrationBuilder, uint missionId, uint[] objectiveIds)
        {
            var values = new object[objectiveIds.Length - 1, 3];
            for (var i = 0; i < objectiveIds.Length - 1; i++)
            {
                values[i, 0] = missionId;
                values[i, 1] = objectiveIds[i];
                values[i, 2] = objectiveIds[i + 1];
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: values);
        }

        /// <summary>Adds the recorded rewards; both kinds carry their amount in the `credits` column.</summary>
        private static void AddRewards(MigrationBuilder migrationBuilder, (uint MissionId, uint Type, uint Value)[] rewards)
        {
            var values = new object[rewards.Length, 5];
            for (var i = 0; i < rewards.Length; i++)
            {
                values[i, 0] = rewards[i].MissionId;
                values[i, 1] = rewards[i].Type;
                values[i, 2] = (int)rewards[i].Value;
                values[i, 3] = 0u;
                values[i, 4] = 0u;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: values);
        }

        private static void RemoveMission(MigrationBuilder migrationBuilder, uint missionId)
        {
            if (!ClientObjectives.TryGetValue(missionId, out var objectives))
                return;

            var rewardKeys = new object[2, 3];
            rewardKeys[0, 0] = missionId; rewardKeys[0, 1] = ExperienceReward; rewardKeys[0, 2] = 0u;
            rewardKeys[1, 0] = missionId; rewardKeys[1, 1] = CreditsReward; rewardKeys[1, 2] = 0u;
            migrationBuilder.DeleteData(table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" }, keyValues: rewardKeys);

            if (objectives.Length > 1)
            {
                var transitions = new object[objectives.Length - 1, 3];
                for (var i = 0; i < objectives.Length - 1; i++)
                {
                    transitions[i, 0] = missionId;
                    transitions[i, 1] = objectives[i].Item1;
                    transitions[i, 2] = objectives[i + 1].Item1;
                }

                migrationBuilder.DeleteData(table: "npc_mission_objective_transition",
                    keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                    keyValues: transitions);
            }

            var keys = new object[objectives.Length, 2];
            for (var i = 0; i < objectives.Length; i++)
            {
                keys[i, 0] = missionId;
                keys[i, 1] = objectives[i].Item1;
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" }, keyValues: keys);

            migrationBuilder.DeleteData(table: "npc_mission", keyColumns: new[] { "id" }, keyValues: new object[] { missionId });

            var values = new object[objectives.Length, 6];
            for (var i = 0; i < objectives.Length; i++)
            {
                values[i, 0] = missionId;
                values[i, 1] = objectives[i].Item1;
                values[i, 2] = objectives[i].Item2;
                values[i, 3] = null;
                values[i, 4] = null;
                values[i, 5] = null;
            }

            migrationBuilder.InsertData(table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: values);
        }


        /// <summary>The client's own objective texts per mission, for the rollback.</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, (uint, string)[]> ClientObjectives = new()
        {
            { 366u, new[] { (1u, "Meet With Ranger Kogari") } },
            { 367u, new[] { (1u, "Speak with Ranger Urialia") } },
            { 368u, new[] { (1u, "Kill Warnets"), (2u, "Searching For Acceptance") } },
            { 411u, new[] { (1u, "Return to Warden Brocail at the Temple of the Bowed Patriarch") } },
            { 412u, new[] { (1u, "Return to Warden Lagori at the Temple of the Raging Patriarch") } },
            { 413u, new[] { (1u, "Return to Warden Kahlee at the Temple of the Proud Patriarch") } },
            { 670u, new[] { (1u, "Find the missing strike team."), (2u, "Return to Field Lt. Brody with information about the first Strike Team") } },
            { 1788u, new[] { (1u, "Find Lt. Bagby in the Treeback Camp") } },
            { 1789u, new[] { (1u, "Deliver the Report to Galloway") } }
        };
    }
}
