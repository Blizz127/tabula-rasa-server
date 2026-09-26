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

        public static bool IsRedirect(uint missionId, uint objectiveId, uint npcPackageId)
            => Redirects.Contains((missionId, objectiveId, npcPackageId));

        public static IReadOnlyCollection<(uint MissionId, uint ObjectiveId, uint NpcPackageId)> All => Redirects;
    }
}
