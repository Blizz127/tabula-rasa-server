using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterMission
{
    using Context.Char;
    using Structures.Char;

    public class CharacterMissionRepository : ICharacterMissionRepository
    {
        private readonly CharContext _charContext;

        public CharacterMissionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterMissionEntry> Get(uint accountId, uint characterSlot)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionEntries);
            var missions = query.Where(mission => _charContext.CharacterEntries.Any(character =>
                character.Id == mission.CharacterId &&
                character.AccountId == accountId &&
                character.Slot == characterSlot)).ToList();

            return missions;
        }

        public List<CharacterMissionObjectiveEntry> GetObjectives(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionObjectiveEntries);

            return query.Where(objective => objective.CharacterId == characterId).ToList();
        }

        // Writes are staged on the context; the caller commits them with Complete()
        // so a mission row and its objectives reach the database together.
        public void Add(CharacterMissionEntry mission, IEnumerable<CharacterMissionObjectiveEntry> objectives)
        {
            _charContext.CharacterMissionEntries.Add(mission);
            _charContext.CharacterMissionObjectiveEntries.AddRange(objectives);
        }

        public void AddObjective(CharacterMissionObjectiveEntry objective)
        {
            _charContext.CharacterMissionObjectiveEntries.Add(objective);
        }

        public void UpdateState(uint characterId, uint missionId, uint missionState, uint changeTime)
        {
            var entry = _charContext.CharacterMissionEntries.Find(characterId, missionId);

            if (entry == null)
                throw new KeyNotFoundException($"character_mission ({characterId}, {missionId}) does not exist");

            entry.MissionState = missionState;
            entry.ChangeTime = changeTime;
        }

        public void UpdateObjectiveStatus(uint characterId, uint missionId, uint objectiveId, uint status)
        {
            var entry = _charContext.CharacterMissionObjectiveEntries.Find(characterId, missionId, objectiveId);

            if (entry == null)
                throw new KeyNotFoundException($"character_mission_objective ({characterId}, {missionId}, {objectiveId}) does not exist");

            entry.Status = status;
        }

        public void Delete(uint characterId, uint missionId)
        {
            var objectives = _charContext.CharacterMissionObjectiveEntries
                .Where(objective => objective.CharacterId == characterId && objective.MissionId == missionId)
                .ToList();

            _charContext.CharacterMissionObjectiveEntries.RemoveRange(objectives);

            var mission = _charContext.CharacterMissionEntries.Find(characterId, missionId);

            if (mission != null)
                _charContext.CharacterMissionEntries.Remove(mission);
        }
    }
}
