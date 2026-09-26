using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3 batch 1: mission 1069 "Receptive Reception", the first mission of the Alia Das hub chain.
    ///
    /// Client (original): three objectives - "Locate the Logos shrine." (package-bound by the shrine, see
    /// below), "Return to Solis" (objective conversation package 168) and "Speak to Warrior Apirka."
    /// (package 112).
    /// TaRapedia (inferred, pre-D11): giver Council Elder Solis, requirement "Too Close For Comfort"
    /// (mission 1407), 4,000 experience and 600 credits.
    /// Ellatha/DaOpa (inferred): briefing "…Near the waterfall is the entrance to Alia Caverns. Inside there
    /// is a shrine of sorts that contains an element of Logos information…", rewards Titan or Prodigy Motor
    /// Assist Armor Boots (the item selections are a gap until the reward item-name table is resolved).
    /// World seed (original): creature 42 is "Council Elder Solis"; the `logos` table places the Enhance
    /// shrine (class 7364 UsableItemDispElohLogosEnhanceV01) at 832.0/161.838/960.0 on map context 1220.
    ///
    /// The mission is offered by Solis (giver) and turned in at Apirka (its last objective).
    /// WildernessHubReceptiveGate adds the requirement after 1407's definition is seeded.
    /// </summary>
    public static class WildernessHubReceptiveReceptionRows
    {
        public const string Migration = "WildernessHubReceptiveReception";

        public const uint Mission = 1069u;

        /// <summary>Council Elder Solis, the giver (world-seed creature 42).</summary>
        public const uint Solis = 42u;

        /// <summary>Warrior Apirka, whose conversation ends the mission (world-seed creature 43).</summary>
        public const uint Apirka = 43u;

        public const uint SolisPackage = 168u;
        public const uint ApirkaPackage = 112u;

        /// <summary>"Too Close For Comfort": TaRapedia lists it as 1069's requirement.</summary>
        public const uint PrerequisiteMission = 1407u;

        /// <summary>The `logos` row for the Enhance shrine that objective 1 waits on.</summary>
        public const uint EnhanceShrine = 10u;

        private const uint Xp = 4000u;
        private const uint Credits = 600u;

        private const byte RewardCredits = 1;
        private const byte RewardExperience = 3;
        private const byte LogosRecoveredBinding = 8;
        private const byte NoCounter = 255;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── npc_package ──
            // The world seed's hub NPCs carry no dialogue package; without it the client has nothing to open
            // when the recruit talks to them. 42 -> 168 (Solis) and 43 -> 112 (Apirka) are the packages the
            // client's own objective conversations name (same defect class as Caufield in W2).
            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[,]
                {
                    { Solis, SolisPackage, "Council Elder Solis (1069)" },
                    { Apirka, ApirkaPackage, "Warrior Apirka (1069)" }
                });

            // ── npc_mission ──
            // Initial level 5 was inferred. WildernessHubReceptiveLevel later corrects it to
            // the dated TaRapedia level 4; retain this historical row for migration order.
            // category 10000001: the Wilderness hub category the Training Day row uses.
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[] { Mission, Solis, Apirka, 5u, 1u, 10000001u, false, false, "Receptive Reception (W3)" });

            // ── npc_mission_objective ──
            // The client skeleton holds the three rows with NULL flags; they are replaced with the ordinals,
            // the required flag and the reveal rule. Ordinals and the reveal transitions are inferred: the
            // client does not carry them (same as every other slice).
            foreach (var objectiveId in new[] { 1u, 2u, 3u })
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { Mission, objectiveId });

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: new object[,]
                {
                    { Mission, 1u, "Locate the Logos shrine.", true, 1u, true },
                    { Mission, 2u, "Return to Solis", true, 2u, false },
                    { Mission, 3u, "Speak to Warrior Apirka.", true, 3u, false }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective_transition",
                columns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                values: new object[,]
                {
                    { Mission, 1u, 2u },
                    { Mission, 2u, 3u }
                });

            // ── the shrine objective ──
            // A LogosRecovered binding whose placement_id carries the `logos` row id: the shrine is a Logos
            // dynamic object, not a content placement, and DynamicObjectManager.LogosRecovery fires it.
            migrationBuilder.InsertData(
                table: "npc_mission_objective_binding",
                columns: new[]
                {
                    "mission_id", "objective_id", "binding_id", "kind", "area_id", "placement_id", "creature_id",
                    "action_id", "destroying_hit_only", "equip_match", "item_template_id", "item_set_id",
                    "target_state", "counter_id", "comment"
                },
                values: new object[]
                {
                    Mission, 1u, 0u, LogosRecoveredBinding, 0u, EnhanceShrine, 0u, 0u, false, 0u, 0u, 0u, 0u, NoCounter,
                    "1069/1 the Enhance shrine in Alia Caverns"
                });

            // The historical prerequisite is added in WildernessHubReceptiveGate, after 1407 exists.

            // ── rewards ──
            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: new object[,]
                {
                    { Mission, RewardExperience, Xp, 0u, 0u },
                    { Mission, RewardCredits, Credits, 0u, 0u }
                });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: new object[,]
                {
                    { Mission, RewardExperience, 0u },
                    { Mission, RewardCredits, 0u }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_binding",
                keyColumns: new[] { "mission_id", "objective_id", "binding_id" },
                keyValues: new object[] { Mission, 1u, 0u });

            migrationBuilder.DeleteData(
                table: "npc_mission_objective_transition",
                keyColumns: new[] { "mission_id", "completed_objective_id", "revealed_objective_id" },
                keyValues: new object[,]
                {
                    { Mission, 1u, 2u },
                    { Mission, 2u, 3u }
                });

            // Restore the NULL-flag skeleton rows the migration replaced, with the client's own comments.
            migrationBuilder.DeleteData(
                table: "npc_mission_objective",
                keyColumns: new[] { "mission_id", "objective_id" },
                keyValues: new object[,]
                {
                    { Mission, 1u }, { Mission, 2u }, { Mission, 3u }
                });

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: new object[,]
                {
                    { Mission, 1u, "Locate the Logos shrine.", null, null, null },
                    { Mission, 2u, "Return to Solis", null, null, null },
                    { Mission, 3u, "Speak to Warrior Apirka.", null, null, null }
                });

            migrationBuilder.DeleteData(
                table: "npc_mission",
                keyColumns: new[] { "id" },
                keyValues: new object[] { Mission });

            migrationBuilder.DeleteData(
                table: "npc_package",
                keyColumns: new[] { "id" },
                keyValues: new object[,] { { Solis }, { Apirka } });
        }
    }
}
