using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterMission
{
    using Structures.Char;

    public interface ICharacterMissionRepository
    {
        List<CharacterMissionEntry> Get(uint accountId, uint characterSlot);
        List<CharacterMissionObjectiveEntry> GetObjectives(uint characterId);
        void Add(CharacterMissionEntry mission, IEnumerable<CharacterMissionObjectiveEntry> objectives);
        void AddObjective(CharacterMissionObjectiveEntry objective);
        void UpdateState(uint characterId, uint missionId, uint missionState, uint changeTime);
        void UpdateObjectiveStatus(uint characterId, uint missionId, uint objectiveId, uint status);
        void Delete(uint characterId, uint missionId);
        List<CharacterMissionObjectiveCounterEntry> GetCounters(uint characterId);
        void UpsertCounter(uint characterId, uint missionId, uint objectiveId, byte counterId, int value);
        void SetObjectiveTimer(uint characterId, uint missionId, uint objectiveId, long? remainingMs, long? anchorMs, bool disarmed);
    }
}
