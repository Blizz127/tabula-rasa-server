using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// Client <c>objectiveconversation</c> rows that are stored as COMPLETION (type 1) on the mission giver's own
    /// package, but whose text sends the player on to the NPC that actually finishes the objective. The server
    /// completes an objective through any loaded row of the speaker's package whatever its type
    /// (<c>MissionManager.CompleteNpcObjective</c> -> <c>Mission.HasObjectiveConversation</c>), so loading these rows
    /// would let the giver mark the objective done the moment the mission is accepted.
    /// docs/river-recon-client-evidence.md: a completion-type key alone does not prove that talking to that NPC
    /// completes the objective, and a redirect row "must not be turned into an invented alternate
    /// objective-completion route". <c>MissionManager.LoadMissions</c> therefore leaves these rows out of the
    /// definitions; the rows themselves stay in the client-skeleton table as the client ships them.
    ///
    /// Each entry is one (mission, objective, package) whose rows (completion and the identical reminder) redirect:
    /// <list type="bullet">
    /// <item>434 Rendezvous At The LZ, objective 1, package 208 (Field Sgt. Witherspoon, the giver): text 1057/2768
    /// "Listen, I know you helped up here but really, we're good from this point. You need to do your magic down
    /// south..." (objectiveconversation.pyo store offsets 5969/5996). The objective ("Deliver the field report to
    /// Commander Randolph", text 6730) completes on Randolph's package 212 (text 1056 "Who's this, then? Speak up!").</item>
    /// <item>441 In Short Supply, objective 1, package 212 (Outpost Commander Randolph, the giver): text 1066/2705
    /// "Did you get in touch with the Twin Pillars infirmary yet?..." (store offsets 6050/6077). The objective
    /// ("Inform Medical Assistant Duncan...", text 6804) completes on Duncan's package 218 (text 1114).</item>
    /// <item>429 River Recon, objective 4, package 116 (Outpost Commander Rogers): text 2263/2767 redirects the player
    /// to Witherspoon with the Bane sighting; the objective completes on Witherspoon's package 208 (text 2039).
    /// docs/river-recon-client-evidence.md names this exact row as the one that must not become a completion route.
    /// Until 2026-09-26 it stayed loaded, so Rogers could close objective 4 himself.</item>
    /// </list>
    /// What the original server did with such rows is not recorded (GAP-READY-REDIRECT-COMPLETION).
    /// </summary>
    public static class MissionRedirectConversations
    {
        private static readonly HashSet<(uint MissionId, uint ObjectiveId, uint NpcPackageId)> Redirects = new()
        {
            (429u, 4u, 116u),
            (434u, 1u, 208u),
            (441u, 1u, 212u)
        };

        /// <summary>
        /// Rows on an NPC the player meets before the objective is done, whose objective the server completes through a
        /// content binding instead. Loaded, the row would finish the objective from the first conversation.
        /// <list type="bullet">
        /// <item>575 The Means of Production, objectives 1 and 2, package 420 (Ranger Nylla, the giver): completion
        /// texts 1866/1868 and reminders 2745/2746, all "Human, have you freed my people from this terror?"
        /// (objectiveconversation.pyo). Nylla gives 575 at her camp, so the row would close "Find the source of the
        /// Forean Machina" and "Destroy Fueling Capsule" on acceptance. They complete on entering the Production
        /// Chamber (content_area 1430500) and on destroying the Production Fueling Capsule (placement 1430120),
        /// PravusResearchInstance, GAP-PRAVUS-OBJ1-TRIGGER.</item>
        /// <item>323 Pirate Radio, objectives 309, 310 and 311, package 106 (Information Spec. Johnson, the giver):
        /// reminder rows only (texts 20000052, 20000055, 20000058, "So, what did you manage to find?"). The objectives
        /// count the destroyed Living Infestations on each dish (PravusResearchInstance).</item>
        /// </list>
        /// </summary>
        private static readonly HashSet<(uint MissionId, uint ObjectiveId, uint NpcPackageId)> ContentBound = new()
        {
            (575u, 1u, 420u),
            (575u, 2u, 420u),
            (323u, 309u, 106u),
            (323u, 310u, 106u),
            (323u, 311u, 106u)
        };

        public static bool IsRedirect(uint missionId, uint objectiveId, uint npcPackageId)
            => Redirects.Contains((missionId, objectiveId, npcPackageId));

        /// <summary>A row MissionManager.LoadMissions leaves out: a redirect, or an objective a content binding completes.</summary>
        public static bool IsWithheld(uint missionId, uint objectiveId, uint npcPackageId)
            => Redirects.Contains((missionId, objectiveId, npcPackageId)) || ContentBound.Contains((missionId, objectiveId, npcPackageId));

        public static IReadOnlyCollection<(uint MissionId, uint ObjectiveId, uint NpcPackageId)> All => Redirects;

        public static IReadOnlyCollection<(uint MissionId, uint ObjectiveId, uint NpcPackageId)> BoundElsewhere => ContentBound;
    }
}
