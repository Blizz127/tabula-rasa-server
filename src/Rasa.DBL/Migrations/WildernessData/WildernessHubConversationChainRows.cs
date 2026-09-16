using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 3: the Wilderness hub missions whose objectives are all conversation-bound.
    ///
    /// Selected the same way every slice is, but mechanically so the rule is visible: the mission is in the
    /// Wilderness, the mission catalog calls it ready, its giver name resolves to exactly one creature in the world
    /// seed, and **every** objective already carries at least one client conversation row. That last condition is
    /// what makes a mission completable without inventing anything - the client itself says which conversation
    /// finishes which objective.
    ///
    /// Taken from TaRapedia's infobox per mission, each value with the date it was set: the experience and credits
    /// below are those recorded values (the catalog keeps the timestamps). No reward is invented; a mission whose
    /// experience was not recorded gets credits only.
    ///
    /// Ordinals and the objective chain are inferred: the client seeds objectives with all three flags NULL and
    /// carries no transitions, so the order here is the client's own objective-id order and the chain is a line,
    /// which is what a conversation chain is. Missions whose shape is not a line (branches, escorts, loot counters)
    /// are not in this batch - 1390 went in by hand, and 751 Boargar Acquisition was held back because its objective
    /// collects eight samples and a conversation alone would let the collection be skipped (GAP-W3-COUNTER-OBJECTIVES).
    ///
    /// Mission levels are not recoverable per mission; the Wilderness hub band (5) is used, as every other hub slice
    /// does.
    /// </summary>
    public static class WildernessHubConversationChainRows
    {
        public const string Migration = "WildernessHubConversationChain";


        /// <summary>5 missions: 431 Distress On The River, 442 Quarantine, 444 Unity Among Men, 549 Failure to Launch, 836 Incoming!.</summary>
        public static readonly uint[] Missions = { 431u, 442u, 444u, 549u, 836u };

        private const uint HubLevel = 5u;
        private const uint GeneralCategory = 10000001u;
        private const uint ExperienceReward = 3u;
        private const uint CreditsReward = 1u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { 431u, 101u, 101u, HubLevel, 1u, GeneralCategory, false, false, "Distress On The River (W3)" },
                    { 442u, 140u, 140u, HubLevel, 1u, GeneralCategory, false, false, "Quarantine (W3)" },
                    { 444u, 140u, 140u, HubLevel, 1u, GeneralCategory, false, false, "Unity Among Men (W3)" },
                    { 549u, 104u, 104u, HubLevel, 1u, GeneralCategory, false, false, "Failure to Launch (W3)" },
                    { 836u, 137u, 137u, HubLevel, 1u, GeneralCategory, false, false, "Incoming! (W3)" },
                });

            // 431 Distress On The River - 3 objective(s)
            ReplaceObjectives(migrationBuilder, 431u, new[]
            {
                (1u, "Get a field report from Surveyor Hugh Corman", true, 1u, true),
                (2u, "Get a field report from Tribal Leader Oingin", true, 2u, false),
                (3u, "Get a field report from Lieutenant Wood", true, 3u, false)
            });
            AddChain(migrationBuilder, 431u, new uint[] { 1u, 2u, 3u });
            AddRewards(migrationBuilder, new[] { (431u, ExperienceReward, 2500u), (431u, CreditsReward, 500u) });

            // 442 Quarantine - 2 objective(s)
            ReplaceObjectives(migrationBuilder, 442u, new[]
            {
                (1u, "Speak to Duncan.", true, 1u, true),
                (2u, "Analyze the Blood Sample.", true, 2u, false)
            });
            AddChain(migrationBuilder, 442u, new uint[] { 1u, 2u });
            AddRewards(migrationBuilder, new[] { (442u, ExperienceReward, 3500u), (442u, CreditsReward, 700u) });

            // 444 Unity Among Men - 1 objective(s)
            ReplaceObjectives(migrationBuilder, 444u, new[]
            {
                (1u, "Take test results to Eleanor.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (444u, CreditsReward, 1200u) });

            // 549 Failure to Launch - 1 objective(s)
            ReplaceObjectives(migrationBuilder, 549u, new[]
            {
                (1u, "Get a new catalyzer.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (549u, ExperienceReward, 3000u), (549u, CreditsReward, 600u) });

            // 836 Incoming! - 1 objective(s)
            ReplaceObjectives(migrationBuilder, 836u, new[]
            {
                (1u, "Deliver the data.", true, 1u, true)
            });
            AddRewards(migrationBuilder, new[] { (836u, ExperienceReward, 11000u), (836u, CreditsReward, 1650u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveMission(migrationBuilder, 431u);
            RemoveMission(migrationBuilder, 442u);
            RemoveMission(migrationBuilder, 444u);
            RemoveMission(migrationBuilder, 549u);
            RemoveMission(migrationBuilder, 836u);
        }

        /// <summary>Replaces the client's objective rows (all flags NULL) with an ordered, required chain.</summary>
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

        /// <summary>Reveals each objective when the one before it completes.</summary>
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

        private static void AddRewards(MigrationBuilder migrationBuilder, (uint MissionId, uint Type, uint Value)[] rewards)
        {
            var values = new object[rewards.Length, 5];
            for (var i = 0; i < rewards.Length; i++)
            {
                values[i, 0] = rewards[i].MissionId;
                values[i, 1] = rewards[i].Type;
                // The amount lives in the `credits` column for either type: an experience reward of 4000 is
                // stored as (type 3, credits 4000).
                values[i, 2] = (int)rewards[i].Value;
                values[i, 3] = 0u;
                values[i, 4] = 0u;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: values);
        }

        /// <summary>Takes a mission back out with typed operations only: rewards, transitions, objectives, row.</summary>
        private static void RemoveMission(MigrationBuilder migrationBuilder, uint missionId)
        {
            if (!ClientObjectives.TryGetValue(missionId, out var objectives))
                return;

            var rewardKeys = new object[2, 3];
            rewardKeys[0, 0] = missionId; rewardKeys[0, 1] = ExperienceReward; rewardKeys[0, 2] = 0u;
            rewardKeys[1, 0] = missionId; rewardKeys[1, 1] = CreditsReward; rewardKeys[1, 2] = 0u;
            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: rewardKeys);

            if (objectives.Length > 1)
            {
                var transitions = new object[objectives.Length - 1, 3];
                for (var i = 0; i < objectives.Length - 1; i++)
                {
                    transitions[i, 0] = missionId;
                    transitions[i, 1] = objectives[i].Item1;
                    transitions[i, 2] = objectives[i + 1].Item1;
                }

                migrationBuilder.DeleteData(
                    table: "npc_mission_objective_transition",
                    keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                    keyValues: transitions);
            }

            var objectiveKeys = new object[objectives.Length, 2];
            for (var i = 0; i < objectives.Length; i++)
            {
                objectiveKeys[i, 0] = missionId;
                objectiveKeys[i, 1] = objectives[i].Item1;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: objectiveKeys);

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { missionId });

            RestoreClientObjectives(migrationBuilder, missionId, objectives);
        }

        /// <summary>Puts the client's rows back with their text and NULL flags.</summary>
        private static void RestoreClientObjectives(MigrationBuilder migrationBuilder, uint missionId,
            (uint, string)[] objectives)
        {
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

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: values);
        }

        /// <summary>The client's own objective texts per mission, for the rollback.</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, (uint, string)[]> ClientObjectives = new()
        {
            { 431u, new[] { (1u, "Get a field report from Surveyor Hugh Corman"), (2u, "Get a field report from Tribal Leader Oingin"), (3u, "Get a field report from Lieutenant Wood") } },
            { 442u, new[] { (1u, "Speak to Duncan."), (2u, "Analyze the Blood Sample.") } },
            { 444u, new[] { (1u, "Take test results to Eleanor.") } },
            { 549u, new[] { (1u, "Get a new catalyzer.") } },
            { 836u, new[] { (1u, "Deliver the data.") } }
        };
    }
}
