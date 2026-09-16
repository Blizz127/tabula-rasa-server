using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 15: the Wilderness missions whose giver the mission sources name without the world seed's rank prefix.
    ///
    /// These are conversation missions that every earlier batch skipped for a naming reason, not a missing one:
    /// TaRapedia says "Lt. Saviours" where the world seed has "AFS Officer Lt. Saviours" (creature 120), "Council Elder
    /// Nula" for 93, "Elder Gadfly" for 113. The giver match now accepts a name that is a suffix of a world-seed
    /// creature's, so these four - 421 A Father's Goodbye, 422 Miner Difficulties, 451 A Visit To The Elders and
    /// 698 Elixir Vitae - are seeded with the objectives and conversations the client already carries.
    ///
    /// 751 Boargar Acquisition stays out on purpose: its objective collects eight Boargar samples and its only client
    /// binding is a conversation, so seeding it as it stands would let the collection be skipped
    /// (GAP-W3-COUNTER-OBJECTIVES).
    /// </summary>
    public static class WildernessGiverFixRows
    {
        public const string Migration = "WildernessGiverFix";

        private const uint GeneralCategory = 10000001u;
        private const uint ExperienceReward = 3u;
        private const uint CreditsReward = 1u;
        private const byte KillBinding = 6;
        private const byte NoCounter = 255;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 421u, 120u, 120u, 5u, 1u, GeneralCategory, false, false, "A Father's Goodbye (Wilderness)" },
                    { 422u, 100u, 100u, 5u, 1u, GeneralCategory, false, false, "Miner Difficulties (Wilderness)" },
                    { 451u, 93u, 93u, 5u, 1u, GeneralCategory, false, false, "A Visit To The Elders (Wilderness)" },
                    { 698u, 113u, 113u, 5u, 1u, GeneralCategory, false, false, "Elixir Vitae (Wilderness)" }
                });

            // 421 A Father's Goodbye
            ReplaceObjectives(migrationBuilder, 421u, new[]
            {
                (3u, "Deliver Saviours' dogtags.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (421u, ExperienceReward, 6000u), (421u, CreditsReward, 900u) });

            // 422 Miner Difficulties
            ReplaceObjectives(migrationBuilder, 422u, new[]
            {
                (1u, "Resupply the mining team.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (422u, ExperienceReward, 5000u), (422u, CreditsReward, 215u) });

            // 451 A Visit To The Elders
            ReplaceObjectives(migrationBuilder, 451u, new[]
            {
                (1u, "Speak to Doyan.", true, 1u, true),
                (2u, "Speak to Todae.", true, 2u, false),
                (3u, "A Visit To The Elders", true, 3u, false)
            });
            AddChain(migrationBuilder, 451u, new uint[] { 1u, 2u, 3u });
            AddRewards(migrationBuilder, new[] { (451u, ExperienceReward, 10000u), (451u, CreditsReward, 1500u) });

            // 698 Elixir Vitae
            ReplaceObjectives(migrationBuilder, 698u, new[]
            {
                (1u, "Deliver the Tinctu Essence.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (698u, ExperienceReward, 11000u), (698u, CreditsReward, 550u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 421u);
            RemoveMission(migrationBuilder, 422u);
            RemoveMission(migrationBuilder, 451u);
            RemoveMission(migrationBuilder, 698u);
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


        /// <summary>Binds an objective to a creature's death, with a counter when the text asks for several.</summary>
        private static void AddKillBinding(MigrationBuilder migrationBuilder, uint missionId, uint objectiveId,
            uint creatureId, uint target)
        {
            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[]
                {
                    "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id",
                    "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id",
                    "target_state", "counter_id", "comment"
                },
                values: new object[,]
                {
                    { missionId, objectiveId, 0u, KillBinding, 0u, 0u, creatureId, 0u, false, 0u, 0u, 0u, 0u,
                      target > 1 ? (byte)0 : NoCounter, $"{missionId}/{objectiveId} kill creature {creatureId}" }
                });

            if (target > 1)
                migrationBuilder.InsertData(
                    table: "npc_mission_objective_counter",
                    columns: new[] { "mission_id", "objective_id", "counter_id", "initial_value", "target_value" },
                    values: new object[,] { { missionId, objectiveId, 0u, 0u, target } });
        }

        /// <summary>Removes the binding and its counter.</summary>
        private static void RemoveKillBinding(MigrationBuilder migrationBuilder, uint missionId, uint[] objectiveIds)
        {
            var counters = new object[objectiveIds.Length, 3];
            var bindings = new object[objectiveIds.Length, 3];
            for (var i = 0; i < objectiveIds.Length; i++)
            {
                counters[i, 0] = missionId; counters[i, 1] = objectiveIds[i]; counters[i, 2] = 0u;
                bindings[i, 0] = missionId; bindings[i, 1] = objectiveIds[i]; bindings[i, 2] = 0u;
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective_counter",
                keyColumns: new[] { "mission_id", "objective_id", "counter_id" }, keyValues: counters);
            migrationBuilder.DeleteData(table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" }, keyValues: bindings);
        }


        /// <summary>The client's own objective texts per mission, for the rollback.</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, (uint, string)[]> ClientObjectives = new()
        {
            { 421u, new[] { (3u, "Deliver Saviours' dogtags.") } },
            { 422u, new[] { (1u, "Resupply the mining team.") } },
            { 451u, new[] { (1u, "Speak to Doyan."), (2u, "Speak to Todae."), (3u, "A Visit To The Elders") } },
            { 698u, new[] { (1u, "Deliver the Tinctu Essence.") } }
        };
    }
}
