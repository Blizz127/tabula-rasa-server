using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Plains's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (35).
    /// </summary>
    public static class PlainsConversationNpcRows
    {
        public const string Migration = "PlainsConversationNpc";

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
                    { 199500u, "Colonel Whitaker", 3846u, 1u, 25u, 555u, 8919u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199501u, "Xenori", 3846u, 1u, 24u, 555u, 8862u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199502u, "Captain Reyko", 3846u, 1u, 24u, 555u, 9007u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199503u, "Engineer Tralos", 3846u, 1u, 20u, 555u, 9011u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199504u, "Receptive Liaison Sage", 3846u, 1u, 32u, 555u, 9941u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199500u, 1095u, "Colonel Whitaker (client conversation package)" },
                    { 199501u, 1119u, "Xenori (client conversation package)" },
                    { 199502u, 1213u, "Captain Reyko (client conversation package)" },
                    { 199503u, 1204u, "Engineer Tralos (client conversation package)" },
                    { 199504u, 2027u, "Receptive Liaison Sage (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199500u, 1764u, (byte)1, 199500u, 1095u, 0u, (byte)0, 320.0, 499.0, -47.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Colonel Whitaker (TaRapedia /loc)" },
                    { 199501u, 1764u, (byte)1, 199501u, 1119u, 0u, (byte)0, 163.0, 417.0, -262.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Xenori (TaRapedia /loc)" },
                    { 199502u, 1764u, (byte)1, 199502u, 1213u, 0u, (byte)0, 324.0, 433.0, -59.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Captain Reyko (TaRapedia /loc)" },
                    { 199503u, 1764u, (byte)1, 199503u, 1204u, 0u, (byte)0, 265.0, 433.0, -234.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Engineer Tralos (TaRapedia /loc)" },
                    { 199504u, 1761u, (byte)1, 199504u, 2027u, 0u, (byte)0, 137.0, 237.0, -268.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Sage (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 640u, 199500u, 199500u, 35u, 1u, GeneralCategory, false, false, "Running Interference (Plains)" },
                    { 1063u, 199501u, 199501u, 35u, 1u, GeneralCategory, false, false, "Incriminating Evidence (Plains)" },
                    { 1112u, 199502u, 199502u, 35u, 1u, GeneralCategory, false, false, "Into The Hive (Plains)" },
                    { 1118u, 199503u, 199503u, 35u, 1u, GeneralCategory, false, false, "A Divided People (Plains)" },
                    { 1119u, 199503u, 199503u, 35u, 1u, GeneralCategory, false, false, "A Tale of Two Brothers (Plains)" },
                    { 1122u, 199503u, 199503u, 35u, 1u, GeneralCategory, false, false, "Key Information (Plains)" },
                    { 1125u, 199502u, 199502u, 35u, 1u, GeneralCategory, false, false, "Security Threat (Plains)" },
                    { 1183u, 199502u, 199502u, 35u, 1u, GeneralCategory, false, false, "Into the Facility (Plains)" },
                    { 1186u, 199502u, 199502u, 35u, 1u, GeneralCategory, false, false, "Underground Mysteries (Plains)" },
                    { 1310u, 199502u, 199502u, 35u, 1u, GeneralCategory, false, false, "Speak to Captain Reyko (Plains)" },
                    { 1746u, 199504u, 199504u, 35u, 1u, GeneralCategory, false, false, "Report to Liaison Sage (Plains)" }
                });

            // 640 Running Interference
            ReplaceObjectives(migrationBuilder, 640u, new[]
            {
                (1u, "Upgrade the Relay Station Computer", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (640u, ExperienceReward, 21000u), (640u, CreditsReward, 3150u) });

            // 1063 Incriminating Evidence
            ReplaceObjectives(migrationBuilder, 1063u, new[]
            {
                (1u, "Speak to Lt. Liu", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1063u, ExperienceReward, 23000u), (1063u, CreditsReward, 3450u) });

            // 1112 Into The Hive
            ReplaceObjectives(migrationBuilder, 1112u, new[]
            {
                (1u, "Find and report the electrical disturbance", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1112u, ExperienceReward, 50000u), (1112u, CreditsReward, 5000u) });

            // 1118 A Divided People
            ReplaceObjectives(migrationBuilder, 1118u, new[]
            {
                (1u, "Speak with Engineer Tralos", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1118u, ExperienceReward, 12500u), (1118u, CreditsReward, 2500u) });

            // 1119 A Tale of Two Brothers
            ReplaceObjectives(migrationBuilder, 1119u, new[]
            {
                (1u, "Find Surveyor Miras", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1119u, ExperienceReward, 50000u), (1119u, CreditsReward, 5000u) });

            // 1122 Key Information
            ReplaceObjectives(migrationBuilder, 1122u, new[]
            {
                (1u, "Deliver Miras' message to Tralos", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1122u, ExperienceReward, 25000u), (1122u, CreditsReward, 3750u) });

            // 1125 Security Threat
            ReplaceObjectives(migrationBuilder, 1125u, new[]
            {
                (1u, "Report to Captain Reyko", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1125u, ExperienceReward, 50000u), (1125u, CreditsReward, 5000u) });

            // 1183 Into the Facility
            ReplaceObjectives(migrationBuilder, 1183u, new[]
            {
                (1u, "Report to Sargeant Phenix", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1183u, ExperienceReward, 12500u), (1183u, CreditsReward, 2500u) });

            // 1186 Underground Mysteries
            ReplaceObjectives(migrationBuilder, 1186u, new[]
            {
                (1u, "Find Eloh Artifacts", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1186u, ExperienceReward, 46000u), (1186u, CreditsReward, 4600u) });

            // 1310 Speak to Captain Reyko
            ReplaceObjectives(migrationBuilder, 1310u, new[]
            {
                (1u, "Speak with Captain Reyko", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1310u, ExperienceReward, 11000u), (1310u, CreditsReward, 2200u) });

            // 1746 Report to Liaison Sage
            ReplaceObjectives(migrationBuilder, 1746u, new[]
            {
                (6u, "Report to Receptive Liaison Sage", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1746u, ExperienceReward, 12000u), (1746u, CreditsReward, 1200u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 640u);
            RemoveMission(migrationBuilder, 1063u);
            RemoveMission(migrationBuilder, 1112u);
            RemoveMission(migrationBuilder, 1118u);
            RemoveMission(migrationBuilder, 1119u);
            RemoveMission(migrationBuilder, 1122u);
            RemoveMission(migrationBuilder, 1125u);
            RemoveMission(migrationBuilder, 1183u);
            RemoveMission(migrationBuilder, 1186u);
            RemoveMission(migrationBuilder, 1310u);
            RemoveMission(migrationBuilder, 1746u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199500u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199501u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199502u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199503u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199504u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199500u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199501u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199502u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199503u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199504u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199500u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199501u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199502u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199503u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199504u });
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
            { 640u, new[] { (1u, "Upgrade the Relay Station Computer") } },
            { 1063u, new[] { (1u, "Speak to Lt. Liu") } },
            { 1112u, new[] { (1u, "Find and report the electrical disturbance") } },
            { 1118u, new[] { (1u, "Speak with Engineer Tralos") } },
            { 1119u, new[] { (1u, "Find Surveyor Miras") } },
            { 1122u, new[] { (1u, "Deliver Miras' message to Tralos") } },
            { 1125u, new[] { (1u, "Report to Captain Reyko") } },
            { 1183u, new[] { (1u, "Report to Sargeant Phenix") } },
            { 1186u, new[] { (1u, "Find Eloh Artifacts") } },
            { 1310u, new[] { (1u, "Speak with Captain Reyko") } },
            { 1746u, new[] { (6u, "Report to Receptive Liaison Sage") } }
        };
    }
}
