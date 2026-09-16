using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Mires's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (30).
    /// </summary>
    public static class MiresConversationNpcRows
    {
        public const string Migration = "MiresConversationNpc";

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
                    { 199400u, "Sgt. Jeansonne", 3846u, 1u, 26u, 555u, 8668u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199401u, "Chakel", 3846u, 1u, 31u, 555u, 8706u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199402u, "Corporal Cooper", 3846u, 1u, 30u, 555u, 8687u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199403u, "Lt. Foushee", 3846u, 1u, 31u, 555u, 8662u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199404u, "Corporal Hairston", 3846u, 1u, 31u, 555u, 8678u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199405u, "Receptive Liaison Ridout", 3846u, 1u, 38u, 555u, 9939u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199400u, 1062u, "Sgt. Jeansonne (client conversation package)" },
                    { 199401u, 1070u, "Chakel (client conversation package)" },
                    { 199402u, 1082u, "Corporal Cooper (client conversation package)" },
                    { 199403u, 1041u, "Lt. Foushee (client conversation package)" },
                    { 199404u, 1057u, "Corporal Hairston (client conversation package)" },
                    { 199405u, 2029u, "Receptive Liaison Ridout (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199400u, 1759u, (byte)1, 199400u, 1062u, 0u, (byte)0, 556.0, 229.0, 800.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Sgt. Jeansonne (TaRapedia /loc)" },
                    { 199401u, 1759u, (byte)1, 199401u, 1070u, 0u, (byte)0, 576.0, 224.0, 359.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Chakel (TaRapedia /loc)" },
                    { 199402u, 1759u, (byte)1, 199402u, 1082u, 0u, (byte)0, 224.0, 230.0, 696.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Corporal Cooper (TaRapedia /loc)" },
                    { 199403u, 1759u, (byte)1, 199403u, 1041u, 0u, (byte)0, 618.0, 224.0, 345.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Lt. Foushee (TaRapedia /loc)" },
                    { 199404u, 1759u, (byte)1, 199404u, 1057u, 0u, (byte)0, 594.5, 222.0, 329.7, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Corporal Hairston (TaRapedia /loc)" },
                    { 199405u, 1497u, (byte)1, 199405u, 2029u, 0u, (byte)0, -62.0, 404.0, 950.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Ridout (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 940u, 199400u, 199400u, 30u, 1u, GeneralCategory, false, false, "Making Your Way (Mires)" },
                    { 969u, 199401u, 199401u, 30u, 1u, GeneralCategory, false, false, "Bring It On Home (Mires)" },
                    { 977u, 199402u, 199402u, 30u, 1u, GeneralCategory, false, false, "Find Me A Rock (Mires)" },
                    { 983u, 199403u, 199403u, 30u, 1u, GeneralCategory, false, false, "You're the Guy (Mires)" },
                    { 1041u, 199403u, 199403u, 30u, 1u, GeneralCategory, false, false, "Confidential Delivery (Mires)" },
                    { 1141u, 199404u, 199404u, 30u, 1u, GeneralCategory, false, false, "Find Corporal Hairston (Mires)" },
                    { 1748u, 199405u, 199405u, 30u, 1u, GeneralCategory, false, false, "Report to Liaison Ridout (Mires)" }
                });

            // 940 Making Your Way
            ReplaceObjectives(migrationBuilder, 940u, new[]
            {
                (1u, "Talk To Sgt. Ricardo", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (940u, ExperienceReward, 14500u), (940u, CreditsReward, 2700u) });

            // 969 Bring It On Home
            ReplaceObjectives(migrationBuilder, 969u, new[]
            {
                (1u, "Find Aswhon", true, 1u, true),
                (3u, "Find Field Sgt. Kalinowski", true, 2u, false),
                (4u, "Find Ashwon's Brother in the Brann LZ", true, 3u, false)
            });
            AddChain(migrationBuilder, 969u, new uint[] { 1u, 3u, 4u });
            AddRewards(migrationBuilder, new[] { (969u, ExperienceReward, 62000u), (969u, CreditsReward, 5600u) });

            // 977 Find Me A Rock
            ReplaceObjectives(migrationBuilder, 977u, new[]
            {
                (1u, "Ask Sirth about the Samples", true, 1u, true),
                (2u, "Ask Rohish about the Samples", true, 2u, false),
                (3u, "Ask Major Ston about the Samples", true, 3u, false)
            });
            AddChain(migrationBuilder, 977u, new uint[] { 1u, 2u, 3u });
            AddRewards(migrationBuilder, new[] { (977u, ExperienceReward, 31000u), (977u, CreditsReward, 4200u) });

            // 983 You're the Guy
            ReplaceObjectives(migrationBuilder, 983u, new[]
            {
                (1u, "Deliver the Report to Lt. Foushee", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (983u, ExperienceReward, 31000u), (983u, CreditsReward, 4200u) });

            // 1041 Confidential Delivery
            ReplaceObjectives(migrationBuilder, 1041u, new[]
            {
                (1u, "Deliver the Report to Lt. Foushee", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1041u, ExperienceReward, 31000u), (1041u, CreditsReward, 4200u) });

            // 1141 Find Corporal Hairston
            ReplaceObjectives(migrationBuilder, 1141u, new[]
            {
                (2u, "Talk to Corporal Hairston", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1141u, ExperienceReward, 14500u), (1141u, CreditsReward, 2700u) });

            // 1748 Report to Liaison Ridout
            ReplaceObjectives(migrationBuilder, 1748u, new[]
            {
                (8u, "Report to Receptive Liaison Ridout", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1748u, ExperienceReward, 17500u), (1748u, CreditsReward, 1500u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 940u);
            RemoveMission(migrationBuilder, 969u);
            RemoveMission(migrationBuilder, 977u);
            RemoveMission(migrationBuilder, 983u);
            RemoveMission(migrationBuilder, 1041u);
            RemoveMission(migrationBuilder, 1141u);
            RemoveMission(migrationBuilder, 1748u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199400u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199401u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199402u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199403u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199404u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199405u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199400u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199401u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199402u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199403u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199404u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199405u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199400u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199401u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199402u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199403u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199404u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199405u });
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
            { 940u, new[] { (1u, "Talk To Sgt. Ricardo") } },
            { 969u, new[] { (1u, "Find Aswhon"), (3u, "Find Field Sgt. Kalinowski"), (4u, "Find Ashwon's Brother in the Brann LZ") } },
            { 977u, new[] { (1u, "Ask Sirth about the Samples"), (2u, "Ask Rohish about the Samples"), (3u, "Ask Major Ston about the Samples") } },
            { 983u, new[] { (1u, "Deliver the Report to Lt. Foushee") } },
            { 1041u, new[] { (1u, "Deliver the Report to Lt. Foushee") } },
            { 1141u, new[] { (2u, "Talk to Corporal Hairston") } },
            { 1748u, new[] { (8u, "Report to Receptive Liaison Ridout") } }
        };
    }
}
