using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 5: the Divide's conversation missions, with the giver NPCs they need.
    ///
    /// Those NPCs are not in the world seed at all, so each one is created from evidence rather than
    /// assumed:
    ///
    /// * the name id is the client's own - generated/client/language/english/creaturenamelanguage, an
    ///   original final-live table, looked up by the exact name the mission sources use;
    /// * level, zone and position come from TaRapedia's NPC page for that name, with the revision date the
    ///   value was set on - community transcription, so inferred;
    /// * the appearance (entity class) has no per-NPC source anywhere: it is an analogue of an existing
    ///   world-seed NPC of the same faction, recorded as such under OD-45.
    ///
    /// The missions themselves follow the same rule as the Wilderness batch: every objective already carries a
    /// client conversation row, so the mission is completable through conversations the client itself defines.
    /// Level is the zone band, as everywhere else, and the rewards are TaRapedia's recorded values.
    /// </summary>
    public static class DivideConversationNpcRows
    {
        public const string Migration = "DivideConversationNpc";

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
                    { 199000u, "Lt. Sebastian", 3846u, 1u, 20u, 555u, 125u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199001u, "Shaman Horea", 22636u, 1u, 13u, 750u, 3000u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199002u, "Field Dr. Dawson", 3846u, 1u, 14u, 555u, 126u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199003u, "Receptive Liaison Brice", 3846u, 1u, 20u, 555u, 10012u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199000u, 1148u, (byte)1, 199000u, 74u, 0u, (byte)0, -48.0, 119.0, 434.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Lt. Sebastian (TaRapedia /loc)" },
                    { 199001u, 1148u, (byte)1, 199001u, 67u, 0u, (byte)0, 284.2, 170.0, 1094.3, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Shaman Horea (TaRapedia /loc)" },
                    { 199002u, 1220u, (byte)1, 199002u, 732u, 0u, (byte)0, 19.0, 116.9, 524.9, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Dr. Dawson (TaRapedia /loc)" },
                    { 199003u, 1220u, (byte)1, 199003u, 2051u, 0u, (byte)0, -773.7, 179.3, 615.9, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Brice (TaRapedia /loc)" }
                });

            // ── the dialogue packages each NPC needs to converse ──
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199000u, 74u, "Lt. Sebastian (client conversation package)" },
                    { 199001u, 67u, "Shaman Horea (client conversation package)" },
                    { 199002u, 732u, "Field Dr. Dawson (client conversation package)" },
                    { 199003u, 2051u, "Receptive Liaison Brice (client conversation package)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 332u, 199000u, 199000u, 10u, 1u, GeneralCategory, false, false, "Ammo Express (Divide)" },
                    { 347u, 199001u, 199001u, 10u, 1u, GeneralCategory, false, false, "Cleansing the Toxins: Part II (Divide)" },
                    { 382u, 199000u, 199000u, 10u, 1u, GeneralCategory, false, false, "Retrieval for Recon (Divide)" },
                    { 796u, 199002u, 199002u, 10u, 1u, GeneralCategory, false, false, "Behind Closed Doors (Divide)" },
                    { 1743u, 199003u, 199003u, 10u, 1u, GeneralCategory, false, false, "Report to Liaison Noonan (Divide)" }
                });

            // 332 Ammo Express
            ReplaceObjectives(migrationBuilder, 332u, new[]
            {
                (1u, "Report to Field Sergeant Hayes at Crossroads Waypoint", true, 1u, true),
                (2u, "Deliver Ammo to Line Captain Dobbs in Thoria Das", true, 2u, false),
                (3u, "Deliver Ammo to Field Lieutenant McMurray along the Foreas Base Trench", true, 3u, false)
            });
            AddChain(migrationBuilder, 332u, new uint[] { 1u, 2u, 3u });
            AddRewards(migrationBuilder, new[] { (332u, ExperienceReward, 11000u) });

            // 347 Cleansing the Toxins: Part II
            ReplaceObjectives(migrationBuilder, 347u, new[]
            {
                (1u, "Deliver the Toxin", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (347u, ExperienceReward, 14000u), (347u, CreditsReward, 2100u) });

            // 382 Retrieval for Recon
            ReplaceObjectives(migrationBuilder, 382u, new[]
            {
                (1u, "Find Recon Officer Tyler", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (382u, ExperienceReward, 17000u), (382u, CreditsReward, 2550u) });

            // 796 Behind Closed Doors
            ReplaceObjectives(migrationBuilder, 796u, new[]
            {
                (1u, "Talk to Agent Franz", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (796u, ExperienceReward, 5500u), (796u, CreditsReward, 1100u) });

            // 1743 Report to Liaison Noonan
            ReplaceObjectives(migrationBuilder, 1743u, new[]
            {
                (3u, "Report to Receptive Liaison Noonan", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1743u, ExperienceReward, 6500u), (1743u, CreditsReward, 700u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 332u);
            RemoveMission(migrationBuilder, 347u);
            RemoveMission(migrationBuilder, 382u);
            RemoveMission(migrationBuilder, 796u);
            RemoveMission(migrationBuilder, 1743u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199000u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199001u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199002u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199003u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199000u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199001u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199002u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199003u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199000u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199001u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199002u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199003u });
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
            { 332u, new[] { (1u, "Report to Field Sergeant Hayes at Crossroads Waypoint"), (2u, "Deliver Ammo to Line Captain Dobbs in Thoria Das"), (3u, "Deliver Ammo to Field Lieutenant McMurray along the Foreas Base Trench") } },
            { 347u, new[] { (1u, "Deliver the Toxin") } },
            { 382u, new[] { (1u, "Find Recon Officer Tyler") } },
            { 796u, new[] { (1u, "Talk to Agent Franz") } },
            { 1743u, new[] { (3u, "Report to Receptive Liaison Noonan") } }
        };
    }
}
