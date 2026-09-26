using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// W3: four conversation missions whose giver and receiver already stand in the world, from the 2026-09-26
    /// "ready but not seeded" dossiers (research/20260926-ready-missions/dossiers.json, which carries each field's
    /// tier, confidence and citations).
    ///
    /// * <b>1742 Report to Liaison Brice</b>: Receptive Liaison Standley (134, Twin Pillars) sends the player to
    ///   Receptive Liaison Brice (199003), who already carries the only completion package, 2050. The client has
    ///   objective 2 only.
    /// * <b>441 In Short Supply</b>: Outpost Commander Randolph (130) - the client's own log names her - sends the
    ///   player to Medical Assistant Duncan (125, package 218). TaRapedia gates it on 549 Failure to Launch.
    /// * <b>434 Rendezvous At The LZ</b>: Field Sgt. Witherspoon (101) - log, opening text, Ellatha and her post-D11
    ///   TaRapedia page - sends the field report to Randolph, whose completion package 212 no creature carried.
    ///   Package 212 is used only by 434 and 441 and both name Randolph, so creature 130 is bound to it (inferred).
    /// * <b>408 Revealing Treeback Experimentation</b>: Lt. Jamison (199089, Cumbria) sends the data to Ranger Jorai
    ///   (510133, package 47).
    ///
    /// Giver and receiver are inferred (the client records neither); objectives are the client's own rows with the
    /// single-objective flags inferred; experience and credits are TaRapedia's (2007 revisions, credits corroborated
    /// by Ellatha for 434/441), inferred. Levels are the zone band (GAP-MISSION-LEVEL); group, category, share and
    /// radio flags are the W3 convention. Left out and recorded instead: the unseeded prerequisites 432 (434) and
    /// 406 (408), which the loader would reject; the reward items, whose only lists predate update 1.4; and the
    /// field report / data pack the player carries, whose client item class is ambiguous (GAP-READY-*).
    ///
    /// Randolph and Standley stand on the plain Redshirt class 29423, which has no client NPC augmentation (52), so
    /// the client cannot converse with them (docs/evidence/mission-speaker-dialogue-classes.json). As with the twelve
    /// speakers of MissionSpeakerDialogueClasses they take an original NPC swapset class and an outfit copied from a
    /// world NPC, both analogues (OD-45): Standley the male class 3846 and the Hacienda outfit that migration uses;
    /// Randolph the female class 3848, because the client's own texts call her "she" and "a busy woman", with the
    /// female AFS officer outfit the world's Major Nicholson (510069) wears.
    ///
    /// The giver-package completion rows (434,1,208) and (441,1,212) are redirects and are kept out of completion by
    /// Rasa.Data.MissionRedirectConversations, not here: the client-skeleton rows stay as the client ships them.
    /// </summary>
    public static class EarlyReadyMissionsRows
    {
        public const string Migration = "EarlyReadyMissions";

        public const uint ReportToLiaisonBrice = 1742u;
        public const uint InShortSupply = 441u;
        public const uint RendezvousAtTheLz = 434u;
        public const uint RevealingTreebackExperimentation = 408u;
        public const uint FailureToLaunch = 549u;

        public const uint Witherspoon = 101u;
        public const uint Duncan = 125u;
        public const uint Randolph = 130u;
        public const uint Standley = 134u;
        public const uint Brice = 199003u;
        public const uint Jamison = 199089u;
        public const uint Jorai = 510133u;

        /// <summary>Randolph's client conversation package: 434 objective 1 completes on it.</summary>
        public const uint RandolphPackage = 212u;

        private const uint RedshirtClass = 29423u;
        private const uint HumanSwapsetMale = 3846u;
        private const uint HumanSwapsetFemale = 3848u;

        private const uint WildernessBand = 5u;
        private const uint PalisadesBand = 15u;
        private const uint GeneralCategory = 10000001u;
        private const uint ExperienceReward = 3u;
        private const uint CreditsReward = 1u;
        private const byte Completed = 4;

        /// <summary>creature_appearance[510094] (Senior Quartermaster Hacienda), as MissionSpeakerDialogueClasses copies it.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] MaleOfficerOutfit =
        {
            (2u, 4021u, 4294934528u), (13u, 6271u, 1u), (14u, 9781u, 4278655809u),
            (15u, 4023u, 4294934528u), (16u, 4022u, 4294934528u), (17u, 24008u, 4286690539u)
        };

        /// <summary>creature_appearance[510069] (Major Nicholson): NPC_Clothing_Officer_1 boots, torso and legs, hair, face.</summary>
        private static readonly (uint Slot, uint Class, uint Color)[] FemaleOfficerOutfit =
        {
            (2u, 4021u, 22120u), (14u, 3672u, 1u), (15u, 4023u, 13933202u),
            (16u, 4022u, 13933202u), (17u, 20824u, 4286886614u)
        };

        private static readonly (uint MissionId, uint ObjectiveId, string Text)[] Objectives =
        {
            (ReportToLiaisonBrice, 2u, "Report to Liaison Brice"),
            (InShortSupply, 1u, "Inform Duncan."),
            (RendezvousAtTheLz, 1u, "Deliver the field report."),
            (RevealingTreebackExperimentation, 1u, "Return to Jorai")
        };

        private static readonly (uint MissionId, uint Experience, uint Credits)[] Rewards =
        {
            (ReportToLiaisonBrice, 6000u, 600u),
            (InShortSupply, 3500u, 700u),
            (RendezvousAtTheLz, 3000u, 600u),
            (RevealingTreebackExperimentation, 10000u, 2000u)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // ── the speakers the client could not talk to ──
            SetSpeaker(migrationBuilder, Standley, HumanSwapsetMale, MaleOfficerOutfit);
            SetSpeaker(migrationBuilder, Randolph, HumanSwapsetFemale, FemaleOfficerOutfit);

            migrationBuilder.InsertData(
                table: "npc_package",
                columns: new[] { "id", "package_id", "comment" },
                values: new object[] { Randolph, RandolphPackage, "Outpost Commander Randolph (client package 212)" });

            // ── the missions ──
            migrationBuilder.InsertData(
                table: "npc_mission",
                columns: new[] { "id", "giver_id", "reciver_id", "level", "group_type", "category_id", "shareable", "radio_completeable", "comment" },
                values: new object[,]
                {
                    { ReportToLiaisonBrice, Standley, Brice, WildernessBand, 1u, GeneralCategory, false, false, "Report to Liaison Brice (W3)" },
                    { InShortSupply, Randolph, Duncan, WildernessBand, 1u, GeneralCategory, false, false, "In Short Supply (W3)" },
                    { RendezvousAtTheLz, Witherspoon, Randolph, WildernessBand, 1u, GeneralCategory, false, false, "Rendezvous At The LZ (W3)" },
                    { RevealingTreebackExperimentation, Jamison, Jorai, PalisadesBand, 1u, GeneralCategory, false, false, "Revealing Treeback Experimentation (Palisades)" }
                });

            // Each mission has a single client objective: required, first, revealed on acceptance.
            foreach (var (missionId, objectiveId, _) in Objectives)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { missionId, objectiveId });

            var objectives = new object[Objectives.Length, 6];
            for (var i = 0; i < Objectives.Length; i++)
            {
                objectives[i, 0] = Objectives[i].MissionId;
                objectives[i, 1] = Objectives[i].ObjectiveId;
                objectives[i, 2] = Objectives[i].Text;
                objectives[i, 3] = true;
                objectives[i, 4] = 1u;
                objectives[i, 5] = true;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: objectives);

            // The amount lives in the `credits` column for either type: an experience reward of 6000 is (type 3, 6000).
            var rewards = new object[Rewards.Length * 2, 5];
            for (var i = 0; i < Rewards.Length; i++)
            {
                rewards[2 * i, 0] = Rewards[i].MissionId;
                rewards[2 * i, 1] = ExperienceReward;
                rewards[2 * i, 2] = (int)Rewards[i].Experience;
                rewards[2 * i, 3] = 0u;
                rewards[2 * i, 4] = 0u;
                rewards[2 * i + 1, 0] = Rewards[i].MissionId;
                rewards[2 * i + 1, 1] = CreditsReward;
                rewards[2 * i + 1, 2] = (int)Rewards[i].Credits;
                rewards[2 * i + 1, 3] = 0u;
                rewards[2 * i + 1, 4] = 0u;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_reward",
                columns: new[] { "id", "type", "credits", "item_template_id", "quantity" },
                values: rewards);

            // TaRapedia's In Short Supply page: Requirement = Failure to Launch, which is seeded.
            migrationBuilder.InsertData(
                table: "npc_mission_prerequisite",
                columns: new[] { "mission_id", "or_group", "required_mission_id", "required_state", "comment" },
                values: new object[] { InShortSupply, (byte)0, FailureToLaunch, Completed, "Failure to Launch" });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "npc_mission_prerequisite",
                keyColumns: new[] { "mission_id", "or_group", "required_mission_id" },
                keyValues: new object[] { InShortSupply, (byte)0, FailureToLaunch });

            var rewardKeys = new object[Rewards.Length * 2, 3];
            for (var i = 0; i < Rewards.Length; i++)
            {
                rewardKeys[2 * i, 0] = Rewards[i].MissionId;
                rewardKeys[2 * i, 1] = ExperienceReward;
                rewardKeys[2 * i, 2] = 0u;
                rewardKeys[2 * i + 1, 0] = Rewards[i].MissionId;
                rewardKeys[2 * i + 1, 1] = CreditsReward;
                rewardKeys[2 * i + 1, 2] = 0u;
            }

            migrationBuilder.DeleteData(
                table: "npc_mission_reward",
                keyColumns: new[] { "id", "type", "item_template_id" },
                keyValues: rewardKeys);

            // The client's objective rows go back with their text and NULL flags.
            foreach (var (missionId, objectiveId, _) in Objectives)
                migrationBuilder.DeleteData(
                    table: "npc_mission_objective",
                    keyColumns: new[] { "mission_id", "objective_id" },
                    keyValues: new object[] { missionId, objectiveId });

            var skeleton = new object[Objectives.Length, 6];
            for (var i = 0; i < Objectives.Length; i++)
            {
                skeleton[i, 0] = Objectives[i].MissionId;
                skeleton[i, 1] = Objectives[i].ObjectiveId;
                skeleton[i, 2] = Objectives[i].Text;
                skeleton[i, 3] = null;
                skeleton[i, 4] = null;
                skeleton[i, 5] = null;
            }

            migrationBuilder.InsertData(
                table: "npc_mission_objective",
                columns: new[] { "mission_id", "objective_id", "comment", "is_required", "ordinal", "revealed_on_accept" },
                values: skeleton);

            foreach (var (missionId, _, _) in Objectives)
                migrationBuilder.DeleteData(
                    table: "npc_mission",
                    keyColumns: new[] { "id" },
                    keyValues: new object[] { missionId });

            migrationBuilder.DeleteData(table: "npc_package", keyColumns: new[] { "id" }, keyValues: new object[] { Randolph });

            RestoreSpeaker(migrationBuilder, Randolph, FemaleOfficerOutfit);
            RestoreSpeaker(migrationBuilder, Standley, MaleOfficerOutfit);
        }

        private static void SetSpeaker(MigrationBuilder migrationBuilder, uint creatureId, uint npcClass,
            IReadOnlyList<(uint Slot, uint Class, uint Color)> outfit)
        {
            migrationBuilder.UpdateData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { creatureId },
                columns: new[] { "class_id" }, values: new object[] { npcClass });

            foreach (var (slot, itemClass, color) in outfit)
                migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                    values: new object[] { creatureId, slot, itemClass, color });
        }

        private static void RestoreSpeaker(MigrationBuilder migrationBuilder, uint creatureId,
            IReadOnlyList<(uint Slot, uint Class, uint Color)> outfit)
        {
            foreach (var (slot, _, _) in outfit)
                migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                    keyValues: new object[] { creatureId, slot });

            migrationBuilder.UpdateData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { creatureId },
                columns: new[] { "class_id" }, values: new object[] { RedshirtClass });
        }
    }
}
