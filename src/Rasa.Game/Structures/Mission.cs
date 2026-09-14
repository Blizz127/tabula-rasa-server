using System.Collections.Generic;
using System.Linq;

namespace Rasa.Structures
{
    using Data;
    using Structures.World;

    /// <summary>
    /// Immutable mission definition loaded from the world database. Per-character
    /// progress lives in <see cref="PlayerMission"/>; never mutate a definition
    /// for one player.
    /// </summary>
    public class Mission : MissionInfo
    {
        public uint MissionId { get; set; }
        public uint MissionGiver { get; set; }
        public uint MissionReciver { get; set; }
        public Dictionary<uint, MissionObjectiveDefinition> Objectives { get; } = new();
        public List<MissionObjectiveConversation> ObjectiveConversations { get; } = new();
        // Live content bindings (MissionContentManager.Load): objective completion by area, use, kill and so on.
        public List<NpcMissionObjectiveBindingEntry> Bindings { get; } = new();
        public Dictionary<uint, List<uint>> Transitions { get; } = new();
        public List<NpcMissionPrerequisiteEntry> Prerequisites { get; } = new();
        public List<NpcMissionRewardEntry> Rewards { get; } = new();

        // Normalized once by MissionManager.BuildRewardInfo; the client's reward
        // display and the turn-in payout both use exactly these values.
        public long RewardCredits { get; set; }
        public long RewardPrestige { get; set; }
        public long RewardExperience { get; set; }
        public List<NpcMissionRewardEntry> OfferedFixedItems { get; } = new();
        // Same order as RewardInfo.SelectableReward, which the client's selectionIdx indexes.
        public List<NpcMissionRewardEntry> OfferedSelectableRewards { get; } = new();
        public List<string> RewardGaps { get; } = new();
        // Withheld reconstructed-content rows of this mission (MissionContentManager.Load).
        public List<string> ContentGaps { get; } = new();

        public Mission(NpcMissionEntry mission)
        {
            MissionId = mission.Id;
            MissionGiver = mission.GiverId;
            MissionReciver = mission.ReciverId;
            MissionConstantData.Level = mission.Level;
            MissionConstantData.GroupType = mission.GroupType;
            MissionConstantData.CategoryId = mission.CategoryId;
            MissionConstantData.Shareable = mission.Shareable;
            MissionConstantData.RadioCompletable = mission.RadioCompleteable;
        }

        public IEnumerable<MissionObjectiveDefinition> ObjectivesInOrder =>
            Objectives.Values.OrderBy(objective => objective.Ordinal).ThenBy(objective => objective.ObjectiveId);
        public bool HasObjectiveConversation(uint objectiveId, uint npcPackageId, uint playerFlagId)
        {
            return ObjectiveConversations.Any(conversation =>
                conversation.ObjectiveId == objectiveId &&
                conversation.NpcPackageId == npcPackageId &&
                conversation.PlayerFlagId == playerFlagId);
        }

        public bool IsRelatedTo(Creature creature)
        {
            if (creature?.Npc == null)
                return false;

            return MissionGiver == creature.DbId || MissionReciver == creature.DbId ||
                   ObjectiveConversations.Any(conversation => conversation.NpcPackageId == creature.Npc.NpcPackageId);
        }

        /// <summary>
        /// Reasons this definition cannot be offered. The server progresses
        /// objectives only through recorded completion bindings, so accepting a
        /// mission with an unbound or unreachable objective would strand the
        /// character, and a mission without objectives would complete at once.
        /// </summary>
        public List<string> DefinitionGaps()
        {
            var gaps = new List<string>();

            if (Objectives.Count == 0)
                gaps.Add("no objectives");

            // ordinal, is_required and revealed_on_accept are server-authoritative
            // with no surviving source; the client's missionobjective table seeds
            // objectives with all three NULL. Unknown values must keep the mission
            // unoffered instead of asserting guessed gameplay.
            foreach (var objective in ObjectivesInOrder)
            {
                if (objective.Ordinal is null)
                    gaps.Add($"objective {objective.ObjectiveId} has unknown ordinal");
                if (objective.IsRequired is null)
                    gaps.Add($"objective {objective.ObjectiveId} has unknown required flag");
                if (objective.RevealedOnAccept is null)
                    gaps.Add($"objective {objective.ObjectiveId} has unknown revealed flag");
            }

            if (Objectives.Count > 0 && !Objectives.Values.Any(objective => objective.RevealedOnAccept == true))
                gaps.Add("no objective is revealed on acceptance");

            if (Objectives.Count > 0 && !Objectives.Values.Any(objective => objective.IsRequired == true))
                gaps.Add("no required objective");

            foreach (var objective in ObjectivesInOrder)
                if (!ObjectiveConversations.Any(conversation => conversation.ObjectiveId == objective.ObjectiveId) &&
                    !Bindings.Any(binding => binding.ObjectiveId == objective.ObjectiveId))
                    gaps.Add($"objective {objective.ObjectiveId} has no completion binding");

            var reachable = new HashSet<uint>(Objectives.Values.Where(objective => objective.RevealedOnAccept == true).Select(objective => objective.ObjectiveId));
            var pending = new Queue<uint>(reachable);

            while (pending.Count > 0)
                if (Transitions.TryGetValue(pending.Dequeue(), out var revealed))
                    foreach (var next in revealed)
                        if (reachable.Add(next))
                            pending.Enqueue(next);

            foreach (var objective in ObjectivesInOrder)
                if (objective.IsRequired == true && !reachable.Contains(objective.ObjectiveId))
                    gaps.Add($"required objective {objective.ObjectiveId} is never revealed");

            // Prerequisites are per-player state (the required mission's state in THIS
            // character's log), so they cannot make the definition unofferable in general;
            // they gate each player's offer at dispense time (MissionManager).
            // The client shows Radio/Share buttons for these flags; their server
            // requests are not implemented, so such definitions stay unoffered.
            if (MissionConstantData.RadioCompletable)
                gaps.Add("radio completion is not implemented");

            if (MissionConstantData.Shareable)
                gaps.Add("mission sharing is not implemented");

            gaps.AddRange(RewardGaps);
            gaps.AddRange(ContentGaps);

            return gaps;
        }

        public bool IsDispensable => DefinitionGaps().Count == 0;

        /// <summary>
        /// Objective rows offered/shown for this definition: the objectives revealed
        /// on acceptance, in ordinal order. Also refreshes the dispense list.
        /// </summary>
        public void RefreshDispenseObjectives()
        {
            ObjectivesList.Clear();
            foreach (var objective in ObjectivesInOrder.Where(objective => objective.RevealedOnAccept == true))
                ObjectivesList.Add(new MissionObjective
                {
                    ObjectiveId = objective.ObjectiveId,
                    ObjectiveStatus = (uint)MissionObjectiveState.Incomplete,
                    // An unknown ordinal cannot be sent; such definitions are
                    // unoffered (DefinitionGaps), so this value is never written.
                    Ordinal = objective.Ordinal ?? uint.MaxValue,
                    IsRequired = objective.IsRequired == true
                });
        }
    }

    public class MissionObjectiveDefinition
    {
        public uint ObjectiveId { get; set; }
        public uint? Ordinal { get; set; }
        public bool? IsRequired { get; set; }
        public bool? RevealedOnAccept { get; set; }

        public MissionObjectiveDefinition()
        {
        }

        public MissionObjectiveDefinition(NpcMissionObjectiveEntry entry)
        {
            ObjectiveId = entry.ObjectiveId;
            Ordinal = entry.Ordinal;
            IsRequired = entry.IsRequired;
            RevealedOnAccept = entry.RevealedOnAccept;
        }
    }

    public class MissionObjectiveConversation
    {
        public uint ObjectiveId { get; set; }
        public uint NpcPackageId { get; set; }
        public uint PlayerFlagId { get; set; }
        // COMPLETION 1, REMINDER 2, CHOICEBODY 3, CHOICE1/2/3 4/5/6 (client's
        // objectiveconversation key[4]). The same (objective, package, flag)
        // group can carry several types.
        public uint ConvoType { get; set; }
    }
}
