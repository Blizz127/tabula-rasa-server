using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Marshes's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (25).
    /// </summary>
    public static class MarshesConversationNpcRows
    {
        public const string Migration = "MarshesConversationNpc";

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
                    { 199300u, "Lieutenant Morrison", 3846u, 1u, 40u, 555u, 3113u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199301u, "Retread Jeska", 3846u, 1u, 36u, 555u, 10321u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199302u, "Retread Lou", 3846u, 1u, 36u, 555u, 10323u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199303u, "Retread Duvall", 3846u, 1u, 36u, 555u, 8551u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199300u, 299u, "Lieutenant Morrison (client conversation package)" },
                    { 199301u, 2367u, "Retread Jeska (client conversation package)" },
                    { 199302u, 2356u, "Retread Lou (client conversation package)" },
                    { 199303u, 950u, "Retread Duvall (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199300u, 1454u, (byte)1, 199300u, 299u, 0u, (byte)0, -205.0, 219.0, 600.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Lieutenant Morrison (TaRapedia /loc)" },
                    { 199301u, 1454u, (byte)1, 199301u, 2367u, 0u, (byte)0, -777.0, 270.0, -552.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Retread Jeska (TaRapedia /loc)" },
                    { 199302u, 1454u, (byte)1, 199302u, 2356u, 0u, (byte)0, -776.0, 241.0, -542.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Retread Lou (TaRapedia /loc)" },
                    { 199303u, 1454u, (byte)1, 199303u, 950u, 0u, (byte)0, -593.0, 216.0, -438.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Retread Duvall (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 1673u, 199300u, 199300u, 25u, 1u, GeneralCategory, false, false, "It Lies in Ruins (Marshes)" },
                    { 1863u, 199301u, 199301u, 25u, 1u, GeneralCategory, false, false, "Missing in Action (Marshes)" },
                    { 1868u, 199302u, 199302u, 25u, 1u, GeneralCategory, false, false, "Gun Control - Part IV (Marshes)" },
                    { 1904u, 199303u, 199303u, 25u, 1u, GeneralCategory, false, false, "Gun Control - Part IV (Marshes)" }
                });

            // 1673 It Lies in Ruins
            ReplaceObjectives(migrationBuilder, 1673u, new[]
            {
                (1u, "Report in to Lieutenant Morrison in Falcon Hold", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1673u, ExperienceReward, 75000u), (1673u, CreditsReward, 3800u) });

            // 1863 Missing in Action
            ReplaceObjectives(migrationBuilder, 1863u, new[]
            {
                (1u, "Locate Lt. Koffman to the east of High Point Retreat.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1863u, ExperienceReward, 65000u), (1863u, CreditsReward, 5400u) });

            // 1868 Gun Control - Part IV
            ReplaceObjectives(migrationBuilder, 1868u, new[]
            {
                (1u, "Return to Retread Lou at High Point Retreat.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1868u, ExperienceReward, 32500u), (1868u, CreditsReward, 3600u) });

            // 1904 Gun Control - Part IV
            ReplaceObjectives(migrationBuilder, 1904u, new[]
            {
                (1u, "Return to Retread Duval at the forest north of high point retreat.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1904u, ExperienceReward, 32500u), (1904u, CreditsReward, 3600u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 1673u);
            RemoveMission(migrationBuilder, 1863u);
            RemoveMission(migrationBuilder, 1868u);
            RemoveMission(migrationBuilder, 1904u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199300u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199301u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199302u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199303u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199300u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199301u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199302u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199303u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199300u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199301u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199302u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199303u });
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
            { 1673u, new[] { (1u, "Report in to Lieutenant Morrison in Falcon Hold") } },
            { 1863u, new[] { (1u, "Locate Lt. Koffman to the east of High Point Retreat.") } },
            { 1868u, new[] { (1u, "Return to Retread Lou at High Point Retreat.") } },
            { 1904u, new[] { (1u, "Return to Retread Duval at the forest north of high point retreat.") } }
        };
    }
}
