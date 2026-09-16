using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 4: the hub's first real fights - two missions whose blocking objective is a kill, and whose target
    /// the world seed actually carries.
    ///
    /// The objective binding and its counter were already implemented (ObjectiveBindingKind.Kill, driven by the boot
    /// camp's 1994 through npc_mission_objective_counter), so these missions need only their definitions:
    ///
    /// <b>427 Lurking In The Shadows</b> - Quartermaster Caufield (creature 132, the same world-seed Caufield the
    /// class-gear slice uses) sends the recruit to Oliver, to kill the Lightbender Proctor Fulgor (creature 76,
    /// "Proctor Fulgor Lightbender Boss", level 15) and bring the ammunition shipment back. Objectives 1 and 7 are
    /// conversations the client carries; objective 6 is the kill and now binds to creature 76. TaRapedia records
    /// 4,000 experience and 400 credits.
    ///
    /// <b>682 Childhood's End</b> - Forean Council Advisor Todae (creature 91) asks the recruit to kill the Xanx
    /// (creature 77, "Arioch Xanx Boss"), with three conversations of his rangers around it in the client's data.
    /// TaRapedia records 10,000 experience and 1,500 credits.
    ///
    /// Kill targets were resolved by matching each blocked objective's text against the world seed's own creature
    /// names, longest name first, and only the matches that came out unique are seeded: "Kill Proctor Fulgor." found
    /// creature 76 and "Kill the Xanx!" found the boss 77. Everything else that a mission like this could need -
    /// collection counters (479, 758, 767...), destructible objects (430, 712), escorts (697) - stays blocked and
    /// recorded rather than approximated.
    ///
    /// Mission levels are not recoverable per mission; the Wilderness hub band is used, as every other hub slice
    /// does, and the two bosses' own levels (15) are recorded here so a later pass can reconsider the gate.
    /// </summary>
    public static class WildernessHubKillObjectiveRows
    {
        public const string Migration = "WildernessHubKillObjective";

        public const uint LurkingInTheShadows = 427u;
        public const uint ChildhoodsEnd = 682u;

        private const uint Caufield = 132u;
        private const uint Todae = 91u;
        private const uint ProctorFulgor = 76u;
        private const uint AriochXanx = 77u;

        private const uint HubLevel = 5u;
        private const uint GeneralCategory = 10000001u;
        private const uint ExperienceReward = 3u;
        private const uint CreditsReward = 1u;

        /// <summary>The kill binding kind: the objective completes when the creature dies (used since the boot camp).</summary>
        private const byte KillBinding = 6;

        /// <summary>A binding with no counter completes on the first qualifying kill.</summary>
        private const byte NoCounter = 255;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { LurkingInTheShadows, Caufield, Caufield, HubLevel, 1u, GeneralCategory, false, false, "Lurking In The Shadows (W3)" },
                    { ChildhoodsEnd, Todae, Todae, HubLevel, 1u, GeneralCategory, false, false, "Childhood's End (W3)" }
                });

            // Objectives 1, 6 and 7 of 427: two client conversations and the kill.
            ReplaceObjectives(migrationBuilder, LurkingInTheShadows, new[]
            {
                (1u, "Speak to Oliver.", true, 1u, true),
                (6u, "Kill Proctor Fulgor.", true, 2u, false),
                (7u, "Return to Caufield.", true, 3u, false)
            });
            AddChain(migrationBuilder, LurkingInTheShadows, new uint[] { 1u, 6u, 7u });

            // Objectives 2 to 6 of 682: the Xanx between the rangers' conversations.
            ReplaceObjectives(migrationBuilder, ChildhoodsEnd, new[]
            {
                (2u, "Speak to Ranger Anjuhi.", true, 1u, true),
                (3u, "Kill the Xanx!", true, 2u, false),
                (4u, "Speak to Ranger Tirna.", true, 3u, false),
                (5u, "Speak to Ranger Anjuhi.", true, 4u, false),
                (6u, "Return to Luminary Doyen.", true, 5u, false)
            });
            AddChain(migrationBuilder, ChildhoodsEnd, new uint[] { 2u, 3u, 4u, 5u, 6u });

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
                    { LurkingInTheShadows, 6u, 0u, KillBinding, 0u, 0u, ProctorFulgor, 0u, false, 0u, 0u, 0u, 0u, NoCounter,
                      "427/6 Proctor Fulgor (creature 76)" },
                    { ChildhoodsEnd, 3u, 0u, KillBinding, 0u, 0u, AriochXanx, 0u, false, 0u, 0u, 0u, 0u, NoCounter,
                      "682/3 Arioch Xanx (creature 77)" }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { LurkingInTheShadows, ExperienceReward, 4000, 0u, 0u },
                    { LurkingInTheShadows, CreditsReward, 400, 0u, 0u },
                    { ChildhoodsEnd, ExperienceReward, 10000, 0u, 0u },
                    { ChildhoodsEnd, CreditsReward, 1500, 0u, 0u }
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[,]
                {
                    { LurkingInTheShadows, ExperienceReward, 0u },
                    { LurkingInTheShadows, CreditsReward, 0u },
                    { ChildhoodsEnd, ExperienceReward, 0u },
                    { ChildhoodsEnd, CreditsReward, 0u }
                });

            migrationBuilder.DeleteData(table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[,]
                {
                    { LurkingInTheShadows, 6u, 0u },
                    { ChildhoodsEnd, 3u, 0u }
                });

            RemoveMission(migrationBuilder, LurkingInTheShadows, new uint[] { 1u, 6u, 7u });
            RemoveMission(migrationBuilder, ChildhoodsEnd, new uint[] { 2u, 3u, 4u, 5u, 6u });
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

        private static void RemoveMission(MigrationBuilder migrationBuilder, uint missionId, uint[] objectiveIds)
        {
            var transitions = new object[objectiveIds.Length - 1, 3];
            for (var i = 0; i < objectiveIds.Length - 1; i++)
            {
                transitions[i, 0] = missionId;
                transitions[i, 1] = objectiveIds[i];
                transitions[i, 2] = objectiveIds[i + 1];
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: transitions);

            var keys = new object[objectiveIds.Length, 2];
            for (var i = 0; i < objectiveIds.Length; i++)
            {
                keys[i, 0] = missionId;
                keys[i, 1] = objectiveIds[i];
            }

            migrationBuilder.DeleteData(table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: keys);

            migrationBuilder.DeleteData(table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { missionId });
        }
    }
}
