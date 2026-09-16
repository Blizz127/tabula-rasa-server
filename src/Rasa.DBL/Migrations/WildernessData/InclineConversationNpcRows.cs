using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: the Incline's conversation missions and the giver NPCs they needed.
    ///
    /// Same pipeline as DivideConversationNpc, and the answer to the blocker that keeps most missions
    /// unseeded: the giver is not in the world seed. Each NPC is created from named evidence - the client's
    /// own creaturenamelanguage for the name id (original), TaRapedia's page for the level, zone and /loc
    /// (community, dated, so inferred), and the appearance of an existing world-seed NPC of the same faction
    /// as an analogue under OD-45. Each mission below has every objective already bound to a client
    /// conversation, so nothing is invented to complete it; the rewards are TaRapedia's recorded values and
    /// the level is the zone band (35).
    /// </summary>
    public static class InclineConversationNpcRows
    {
        public const string Migration = "InclineConversationNpc";

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
                    { 199600u, "Receptive Liaison Maddox", 3846u, 1u, 35u, 555u, 9942u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u },
                    { 199601u, "Supply Sergeant Otto", 3846u, 1u, 25u, 555u, 10272u, 9u, 5u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u }
                });

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { 199600u, 2028u, "Receptive Liaison Maddox (client conversation package)" },
                    { 199601u, 2329u, "Supply Sergeant Otto (client conversation package)" }
                });

            migrationBuilder.InsertData(
                table: "content_placement",
                columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id", "usable_kind",
                    "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state", "alternate_state_condition_id",
                    "windup_ms", "name_override_id", "hit_points", "restore_ms", "fuse_ms", "loot_item_set_id", "respawn_ms",
                    "present_condition_id", "usable_condition_id", "comment" },
                values: new object[,]
                {
                    { 199600u, 1759u, (byte)1, 199600u, 2028u, 0u, (byte)0, 584.0, 222.0, 329.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Receptive Liaison Maddox (TaRapedia /loc)" },
                    { 199601u, 1761u, (byte)1, 199601u, 2329u, 0u, (byte)0, 326.0, 260.0, 17.0, 0.0, (byte)1, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, "Supply Sergeant Otto (TaRapedia /loc)" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 1747u, 199600u, 199600u, 35u, 1u, GeneralCategory, false, false, "Report to Liaison Maddox (Incline)" },
                    { 1826u, 199601u, 199601u, 35u, 1u, GeneralCategory, false, false, "Helping Parsons (Incline)" }
                });

            // 1747 Report to Liaison Maddox
            ReplaceObjectives(migrationBuilder, 1747u, new[]
            {
                (7u, "Report to Receptive Liaison Maddox", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (1747u, ExperienceReward, 14500u), (1747u, CreditsReward, 1350u) });

            // 1826 Helping Parsons
            ReplaceObjectives(migrationBuilder, 1826u, new[]
            {
                (1u, "Get the supplies.", true, 1u, true),
                (2u, "Pick up the supply boxes.", true, 2u, false),
                (3u, "Deliver the Supplies to Plains Post.", true, 3u, false),
                (4u, "Deliver the supply box to Mires Pass.", true, 4u, false),
                (5u, "Return with signed paperwork.", true, 5u, false)
            });
            AddChain(migrationBuilder, 1826u, new uint[] { 1u, 2u, 3u, 4u, 5u });
            AddRewards(migrationBuilder, new[] { (1826u, ExperienceReward, 12500u), (1826u, CreditsReward, 2500u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 1747u);
            RemoveMission(migrationBuilder, 1826u);

            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199600u });
            migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { 199601u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199600u });
            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { 199601u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199600u });
            migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { 199601u });
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
            { 1747u, new[] { (7u, "Report to Receptive Liaison Maddox") } },
            { 1826u, new[] { (1u, "Get the supplies."), (2u, "Pick up the supply boxes."), (3u, "Deliver the Supplies to Plains Post."), (4u, "Deliver the supply box to Mires Pass."), (5u, "Return with signed paperwork.") } }
        };
    }
}
