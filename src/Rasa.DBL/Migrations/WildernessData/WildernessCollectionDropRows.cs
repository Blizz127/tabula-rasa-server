using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 14: the Wilderness missions whose objectives ask for a creature drop.
    ///
    /// "Get 6 Miasma Goo Samples", "Acquire 6 Shield Drone Parts", "Collect 4 Xanx Pincers" - the client has no
    /// conversation for these, so every earlier batch had to skip them. What the client does carry is the item as its
    /// own entity class ("Miasma Goo" 11162, "Xanx Pincers" 11160), and the objective names the creature family the
    /// item comes from, which the world seed carries (Bane Miasma 88, Bane Shield Drone 85, Bane Xanx 87).
    ///
    /// No surviving source ties a mission item to an item template or a loot table, so the objective is bound as a
    /// **count of that creature's deaths**: the kill binding and counter the boot camp uses, with the target taken from
    /// the objective's own text. That is the same thing the objective means in play - you get the samples by killing
    /// what drops them - but it is a reconstruction, so it is recorded as OD-47 with the item class each counter
    /// stands for, and a later pass with the drop data would replace it with a real loot counter.
    /// </summary>
    public static class WildernessCollectionDropRows
    {
        public const string Migration = "WildernessCollectionDrop";

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
                    { 767u, 109u, 109u, 5u, 1u, GeneralCategory, false, false, "Mighty Miasma (Wilderness)" },
                    { 771u, 110u, 110u, 5u, 1u, GeneralCategory, false, false, "Droning On (Wilderness)" },
                    { 787u, 111u, 111u, 5u, 1u, GeneralCategory, false, false, "Xanx For the Help (Wilderness)" }
                });

            // 767 Mighty Miasma
            ReplaceObjectives(migrationBuilder, 767u, new[]
            {
                (2u, "Get 6 Miasma Goo Samples.", true, 1u, true)
            });
            AddKillBinding(migrationBuilder, 767u, 2u, 88u, 6u);
            AddRewards(migrationBuilder, new[] { (767u, ExperienceReward, 4000u), (767u, CreditsReward, 600u) });

            // 771 Droning On
            ReplaceObjectives(migrationBuilder, 771u, new[]
            {
                (2u, "Acquire 6 Shield Drone Parts.", true, 1u, true)
            });
            AddKillBinding(migrationBuilder, 771u, 2u, 85u, 6u);
            AddRewards(migrationBuilder, new[] { (771u, CreditsReward, 300u) });

            // 787 Xanx For the Help
            ReplaceObjectives(migrationBuilder, 787u, new[]
            {
                (3u, "Acquire 4 Xanx Pincers", true, 1u, true)
            });
            AddKillBinding(migrationBuilder, 787u, 3u, 87u, 4u);
            AddRewards(migrationBuilder, new[] { (787u, ExperienceReward, 4000u), (787u, CreditsReward, 600u) });

        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            RemoveKillBinding(migrationBuilder, 767u, new uint[] { 2u });
            RemoveKillBinding(migrationBuilder, 771u, new uint[] { 2u });
            RemoveKillBinding(migrationBuilder, 787u, new uint[] { 3u });
            RemoveMission(migrationBuilder, 767u);
            RemoveMission(migrationBuilder, 771u);
            RemoveMission(migrationBuilder, 787u);
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
            { 767u, new[] { (2u, "Get 6 Miasma Goo Samples.") } },
            { 771u, new[] { (2u, "Acquire 6 Shield Drone Parts.") } },
            { 787u, new[] { (3u, "Acquire 4 Xanx Pincers") } }
        };
    }
}
