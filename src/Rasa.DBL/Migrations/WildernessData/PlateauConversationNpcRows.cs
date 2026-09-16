using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Plateau's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (20).
    /// </summary>
    public static class PlateauConversationNpcRows
    {
        public const string Migration = "PlateauConversationNpc";

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
                    { 199200u, "Field Lt. Peterson", 3846u, 1u, 8u, 555u, 4662u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199201u, "Field Sgt. Garde", 3846u, 1u, 32u, 555u, 4665u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199202u, "Alpha Squad Commander Carvelle", 3846u, 1u, 35u, 555u, 8847u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199203u, "Colonel 'Snake' Washington", 3846u, 1u, 50u, 555u, 4372u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199204u, "Amee Corman", 3846u, 1u, 50u, 555u, 4370u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199205u, "General Thaddeus T. Bailey", 3846u, 1u, 50u, 555u, 6769u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199200u, 385u, "Field Lt. Peterson (client conversation package)" },
                    { 199201u, 387u, "Field Sgt. Garde (client conversation package)" },
                    { 199202u, 1116u, "Alpha Squad Commander Carvelle (client conversation package)" },
                    { 199203u, 331u, "Colonel 'Snake' Washington (client conversation package)" },
                    { 199204u, 329u, "Amee Corman (client conversation package)" },
                    { 199205u, 636u, "General Thaddeus T. Bailey (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199200u, 1497u, (byte)1, 199200u, 385u, 0u, (byte)0, -66.0, 401.0, 931.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Lt. Peterson (TaRapedia /loc)" },
                    { 199201u, 1497u, (byte)1, 199201u, 387u, 0u, (byte)0, -103.0, 269.0, 183.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Field Sgt. Garde (TaRapedia /loc)" },
                    { 199202u, 1497u, (byte)1, 199202u, 1116u, 0u, (byte)0, -432.0, 320.0, -797.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Alpha Squad Commander Carvelle (TaRapedia /loc)" },
                    { 199203u, 1304u, (byte)1, 199203u, 331u, 0u, (byte)0, -981.0, 915.0, 413.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Colonel 'Snake' Washington (TaRapedia /loc)" },
                    { 199204u, 1304u, (byte)1, 199204u, 329u, 0u, (byte)0, -150.0, 861.0, 539.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Amee Corman (TaRapedia /loc)" },
                    { 199205u, 1497u, (byte)1, 199205u, 636u, 0u, (byte)0, -68.0, 404.0, 949.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "General Thaddeus T. Bailey (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 887u, 199200u, 199200u, 20u, 1u, GeneralCategory, false, false, "The Defiant Ones (Plateau)" },
                    { 970u, 199201u, 199201u, 20u, 1u, GeneralCategory, false, false, "Lookout Down Below (Plateau)" },
                    { 1040u, 199202u, 199202u, 20u, 1u, GeneralCategory, false, false, "Incommunicado (Plateau)" },
                    { 1068u, 199203u, 199203u, 20u, 1u, GeneralCategory, false, false, "South Of The Border (Plateau)" },
                    { 1541u, 199204u, 199204u, 20u, 1u, GeneralCategory, false, false, "New Orders From The General (Plateau)" },
                    { 2016u, 199205u, 199205u, 20u, 1u, GeneralCategory, false, false, "A Mystery Unearthed (Plateau)" }
                });

            // 887 The Defiant Ones
            ReplaceObjectives(migrationBuilder, 887u, new[]
            {
                (1u, "Report to Field Lt. Peterson.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (887u, ExperienceReward, 22500u), (887u, CreditsReward, 3200u) });

            // 970 Lookout Down Below
            ReplaceObjectives(migrationBuilder, 970u, new[]
            {
                (1u, "Rendezvous with Sgt. Garde.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (970u, ExperienceReward, 20000u), (970u, CreditsReward, 3100u) });

            // 1040 Incommunicado
            ReplaceObjectives(migrationBuilder, 1040u, new[]
            {
                (1u, "Deliver a comm unit to Carville.", true, 1u, true),
                (2u, "Deliver a comm unit to Petrie.", true, 2u, false),
                (3u, "Deliver a comm unit to Locke.", true, 3u, false)
            });
            AddChain(migrationBuilder, 1040u, new uint[] { 1u, 2u, 3u });
            AddRewards(migrationBuilder, new[] { (1040u, ExperienceReward, 100000u), (1040u, CreditsReward, 6600u) });

            // 1068 South Of The Border
            ReplaceObjectives(migrationBuilder, 1068u, new[]
            {
                (1u, "Deliver supplies to \"Snake.\"", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1068u, ExperienceReward, 25000u), (1068u, CreditsReward, 3300u) });

            // 1541 New Orders From The General
            ReplaceObjectives(migrationBuilder, 1541u, new[]
            {
                (1u, "Question Amee Corman.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1541u, ExperienceReward, 55000u), (1541u, CreditsReward, 5100u) });

            // 2016 A Mystery Unearthed
            ReplaceObjectives(migrationBuilder, 2016u, new[]
            {
                (1u, "Report to General Thadeus T. Bailey (Plateau - Fort Defiance)", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (2016u, ExperienceReward, 7500u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 887u);
            RemoveMission(migrationBuilder, 970u);
            RemoveMission(migrationBuilder, 1040u);
            RemoveMission(migrationBuilder, 1068u);
            RemoveMission(migrationBuilder, 1541u);
            RemoveMission(migrationBuilder, 2016u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199200u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199201u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199202u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199203u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199204u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199205u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199200u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199201u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199202u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199203u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199204u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199205u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199200u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199201u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199202u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199203u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199204u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199205u });
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
            { 887u, new[] { (1u, "Report to Field Lt. Peterson.") } },
            { 970u, new[] { (1u, "Rendezvous with Sgt. Garde.") } },
            { 1040u, new[] { (1u, "Deliver a comm unit to Carville."), (2u, "Deliver a comm unit to Petrie."), (3u, "Deliver a comm unit to Locke.") } },
            { 1068u, new[] { (1u, "Deliver supplies to \"Snake.\"") } },
            { 1541u, new[] { (1u, "Question Amee Corman.") } },
            { 2016u, new[] { (1u, "Report to General Thadeus T. Bailey (Plateau - Fort Defiance)") } }
        };
    }
}
