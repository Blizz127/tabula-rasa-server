using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 2: the Alia Das hub's conversation chain.
    ///
    /// These missions' client objectives and conversation bindings already sit in the world seed from the client
    /// skeleton (missionobjective + objectiveconversation rows, loaded with every definition column NULL). What they
    /// were missing is the definition: giver, receiver, objective ordering and flags, the transitions between the
    /// objectives, and the rewards. The client's own mission census
    /// (research/20260913-source-sweep/mission-tables/mission_census.tsv) fixes the identities - 1390 "Conscientious
    /// Objector" with 8 objectives, 1392 and 1393 "Conscientious Objector - Part Two" with 1 each, 1407 "Too Close
    /// For Comfort" with 2 - and the objective texts below are the client's own comments, carried over unchanged.
    ///
    /// <b>1390 Conscientious Objector</b> is the Ethical Parable. Solis hands it out; Forean Elder Quillas (creature
    /// 114, package 1646) is questioned and answers; Warrior Apirka (creature 43, package 112) receives Milpas and
    /// closes it. Its middle objectives 2 and 3 are the two mutually exclusive answers - the client binds both to
    /// package 1646 with convo_type 1, which is what makes either one completable - so both are optional here and
    /// both lead on, exactly as the two branches the sources describe. Objective 8, "Escort Milpas to Divide
    /// entrance.", is the other branch's step and is optional too: the escort itself needs the follow-a-player
    /// behaviour the emulator does not have yet, so the objective exists, does not block, and is recorded as a gap
    /// rather than faked. Objective 12 carries the client's own "no desc" and is optional for the same reason.
    ///
    /// <b>1392 and 1393 Conscientious Objector - Part Two</b> are the report to Outpost Commander Rogers (creature
    /// 100, package 116) that either branch leads to. Two ids because each branch has its own copy; both are seeded
    /// the same.
    ///
    /// <b>1407 Too Close For Comfort</b> is Council Elder Moawi (creature 38, package 113) asking the recruit to
    /// check on Council Elder Solis (creature 42, package 168) - the two objectives the client carries, in order.
    ///
    /// <b>Rewards.</b> Ellatha records 400 credits for 1390; its experience is not recorded in any source found and
    /// is left out rather than guessed. 1407's recorded reward is a choice between the Vitalius Leech Gun and the
    /// Vextronics Chaingun; neither template is resolved yet, so 1407 gets no reward rows and both item and credits
    /// stay gaps. 1392/1393 carry no recorded reward at all.
    ///
    /// <b>Prerequisites</b> are deliberately not seeded: the content loader rejects a prerequisite naming a mission
    /// without a definition, and this chain's earlier links (479 Forming Alliances, 1391 Part One) are not seeded
    /// yet. The intended gates are recorded in the manifest and land with those missions.
    /// </summary>
    public static class WildernessHubConscientiousObjectorRows
    {
        public const string Migration = "WildernessHubConscientiousObjector";

        public const uint ConscientiousObjector = 1390u;
        public const uint ConscientiousObjectorPartTwo = 1392u;
        public const uint ConscientiousObjectorPartTwoAlternate = 1393u;
        public const uint TooCloseForComfort = 1407u;

        private const uint Solis = 42u;
        private const uint Apirka = 43u;
        private const uint Moawi = 38u;
        private const uint Rogers = 100u;
        private const uint Quillas = 114u;

        private const uint ApirkaPackage = 112u;
        private const uint MoawiPackage = 113u;
        private const uint RogersPackage = 116u;
        private const uint QuillasPackage = 1646u;

        private const uint GeneralCategory = 10000001u;
        private const uint CreditsRewardType = 1u;
        private const uint ConscientiousCredits = 400u;

        /// <summary>The eight objectives of 1390 as the client carries them: id, text, required, ordinal, revealed.</summary>
        private static readonly (uint ObjectiveId, string Text, bool Required, uint Ordinal, bool Revealed)[] ConscientiousObjectives =
        {
            (1u, "Question Elder Quillas.", true, 1u, true),
            (2u, "Speak to Quillas again.", false, 2u, false),
            (3u, "Speak to Quillas again.", false, 2u, false),
            (4u, "Take Milpas to Apirka.", true, 3u, false),
            (8u, "Escort Milpas to Divide entrance.", false, 3u, false),
            (10u, "Speak to Apirka", true, 4u, false),
            (11u, "Return to Warrior Apirka.", false, 5u, false),
            (12u, "no desc", false, 6u, false)
        };

        private static readonly (uint Completed, uint Revealed)[] ConscientiousTransitions =
        {
            (1u, 2u), (1u, 3u), (2u, 4u), (3u, 4u), (3u, 8u), (4u, 10u), (8u, 10u)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── packages: the two elders' dialogue packages, neither bound before ──
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { Quillas, QuillasPackage, "Forean Elder Quillas" },
                    { Moawi, MoawiPackage, "Council Elder Moawi" }
                });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { ConscientiousObjector, Solis, Apirka, 5u, 1u, GeneralCategory, false, false, "Conscientious Objector (W3)" },
                    { ConscientiousObjectorPartTwo, Rogers, Rogers, 5u, 1u, GeneralCategory, false, false, "Conscientious Objector Part Two (W3)" },
                    { ConscientiousObjectorPartTwoAlternate, Rogers, Rogers, 5u, 1u, GeneralCategory, false, false, "Conscientious Objector Part Two, other branch (W3)" },
                    { TooCloseForComfort, Moawi, Solis, 5u, 1u, GeneralCategory, false, false, "Too Close For Comfort (W3)" }
                });

            // ── objectives: the client rows are replaced, since every flag on them is NULL ──
            ReplaceObjective(migrationBuilder, ConscientiousObjector, ConscientiousObjectives);
            ReplaceObjective(migrationBuilder, ConscientiousObjectorPartTwo,
                new[] { (1u, "Bring the report to Rogers.", true, 1u, true) });
            ReplaceObjective(migrationBuilder, ConscientiousObjectorPartTwoAlternate,
                new[] { (1u, "Bring the report to Rogers.", true, 1u, true) });
            ReplaceObjective(migrationBuilder, TooCloseForComfort, new[]
            {
                (1u, "Speak to Elder Moawi.", true, 1u, true),
                (10u, "Check on Elder Solis.", true, 2u, false)
            });

            // ── transitions: the fork, the join, and the two straight lines ──
            var transitions = new object[ConscientiousTransitions.Length + 1, 3];
            for (var i = 0; i < ConscientiousTransitions.Length; i++)
            {
                transitions[i, 0] = ConscientiousObjector;
                transitions[i, 1] = ConscientiousTransitions[i].Completed;
                transitions[i, 2] = ConscientiousTransitions[i].Revealed;
            }
            transitions[ConscientiousTransitions.Length, 0] = TooCloseForComfort;
            transitions[ConscientiousTransitions.Length, 1] = 1u;
            transitions[ConscientiousTransitions.Length, 2] = 10u;

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: transitions);

            // ── rewards ──
            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[] { ConscientiousObjector, CreditsRewardType, (int)ConscientiousCredits, 0u, 0u });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[] { ConscientiousObjector, CreditsRewardType, 0u });

            var transitions = new object[ConscientiousTransitions.Length + 1, 3];
            for (var i = 0; i < ConscientiousTransitions.Length; i++)
            {
                transitions[i, 0] = ConscientiousObjector;
                transitions[i, 1] = ConscientiousTransitions[i].Completed;
                transitions[i, 2] = ConscientiousTransitions[i].Revealed;
            }
            transitions[ConscientiousTransitions.Length, 0] = TooCloseForComfort;
            transitions[ConscientiousTransitions.Length, 1] = 1u;
            transitions[ConscientiousTransitions.Length, 2] = 10u;

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: transitions);

            // Put the client's own rows back: text kept, every other column NULL.
            var conscientiousTexts = new (uint ObjectiveId, string Text)[ConscientiousObjectives.Length];
            for (var i = 0; i < conscientiousTexts.Length; i++)
                conscientiousTexts[i] = (ConscientiousObjectives[i].ObjectiveId, ConscientiousObjectives[i].Text);
            RestoreObjective(migrationBuilder, ConscientiousObjector, conscientiousTexts);
            RestoreObjective(migrationBuilder, ConscientiousObjectorPartTwo, new[] { (1u, "Bring the report to Rogers.") });
            RestoreObjective(migrationBuilder, ConscientiousObjectorPartTwoAlternate, new[] { (1u, "Bring the report to Rogers.") });
            RestoreObjective(migrationBuilder, TooCloseForComfort, new[]
            {
                (1u, "Speak to Elder Moawi."), (10u, "Check on Elder Solis.")
            });

            migrationBuilder.DeleteData(table: "npc_mission", keyColumns: new[] { "id" },
                keyValues: new object[,]
                {
                    { ConscientiousObjector }, { ConscientiousObjectorPartTwo },
                    { ConscientiousObjectorPartTwoAlternate }, { TooCloseForComfort }
                });

            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" },
                keyValues: new object[,] { { Quillas }, { Moawi } });
        }

        private static void ReplaceObjective(MigrationBuilder migrationBuilder, uint missionId,
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

        private static void RestoreObjective(MigrationBuilder migrationBuilder, uint missionId,
            (uint ObjectiveId, string Text)[] objectives)
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
                values[i, 3] = null;
                values[i, 4] = null;
                values[i, 5] = null;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: values);
        }
    }
}
