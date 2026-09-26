using Rasa.Structures;

namespace Rasa.Data
{
    /// <summary>
    /// Recovered client branch for Conscientious Objector. Choice and objective IDs
    /// are original client data; their server linkage is reconstructed in
    /// docs/evidence/wilderness-conscientious-branches.json.
    /// </summary>
    public static class MissionBranchRules
    {
        public const uint Mission = 1390;
        public const uint ArrestReport = 1392;
        public const uint ReleaseReport = 1393;

        public static bool TryChoiceOutcome(uint missionId, uint objectiveId, int choiceIndex, out uint outcome)
        {
            outcome = 0;
            if (missionId != Mission || objectiveId != 1)
                return false;
            outcome = choiceIndex switch { 1 => 2u, 2 => 3u, _ => 0u };
            return outcome != 0;
        }

        public static bool IsAlternativeBlocked(PlayerMission progress, uint missionId, uint objectiveId)
        {
            if (missionId != Mission)
                return false;
            return objectiveId switch
            {
                2 or 8 or 11 => Completed(progress, 3),
                3 or 4 or 10 => Completed(progress, 2),
                _ => false
            };
        }

        public static bool IsCompleteable(PlayerMission progress)
            => progress.State == MissionState.Active && Completed(progress, 1) &&
               ((Completed(progress, 2) && Completed(progress, 8) && Completed(progress, 11)) ||
                (Completed(progress, 3) && Completed(progress, 4) && Completed(progress, 10)));

        public static bool PartTwoAvailable(Manifestation player, uint missionId)
        {
            if (missionId != ArrestReport && missionId != ReleaseReport)
                return true;
            if (!player.Missions.TryGetValue(Mission, out var progress) || progress.State != MissionState.Completed)
                return false;
            return missionId == ArrestReport
                ? Completed(progress, 3) && Completed(progress, 4) && Completed(progress, 10)
                : Completed(progress, 2) && Completed(progress, 8) && Completed(progress, 11);
        }

        private static bool Completed(PlayerMission progress, uint objectiveId)
            => progress.Objectives.TryGetValue(objectiveId, out var state) && state == MissionObjectiveState.Completed;
    }
}
