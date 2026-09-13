using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

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

        // Find also returns rows already staged for deletion in this unit of work; those no longer exist.
        private T Live<T>(T entry) where T : class
            => entry == null || _charContext.Entry(entry).State == EntityState.Deleted ? null : entry;

        public void UpdateState(uint characterId, uint missionId, uint missionState, uint changeTime)
        {
            var entry = Live(_charContext.CharacterMissionEntries.Find(characterId, missionId));

            if (entry == null)
                throw new KeyNotFoundException($"character_mission ({characterId}, {missionId}) does not exist");

            entry.MissionState = missionState;
            entry.ChangeTime = changeTime;
        }

        public void UpdateObjectiveStatus(uint characterId, uint missionId, uint objectiveId, uint status)
        {
            var entry = Live(_charContext.CharacterMissionObjectiveEntries.Find(characterId, missionId, objectiveId));

            if (entry == null)
                throw new KeyNotFoundException($"character_mission_objective ({characterId}, {missionId}, {objectiveId}) does not exist");

            entry.Status = status;
        }

        public void Delete(uint characterId, uint missionId)
        {
            // Stored rows plus rows staged earlier in this unit of work.
            var objectives = _charContext.CharacterMissionObjectiveEntries
                .Where(objective => objective.CharacterId == characterId && objective.MissionId == missionId)
                .ToList()
                .Union(_charContext.CharacterMissionObjectiveEntries.Local.Where(objective => objective.CharacterId == characterId && objective.MissionId == missionId))
                .ToList();

            _charContext.CharacterMissionObjectiveEntries.RemoveRange(objectives);

            var counters = _charContext.CharacterMissionObjectiveCounterEntries
                .Where(counter => counter.CharacterId == characterId && counter.MissionId == missionId)
                .ToList()
                .Union(_charContext.CharacterMissionObjectiveCounterEntries.Local.Where(counter => counter.CharacterId == characterId && counter.MissionId == missionId))
                .ToList();

            _charContext.CharacterMissionObjectiveCounterEntries.RemoveRange(counters);

            var mission = _charContext.CharacterMissionEntries.Find(characterId, missionId);

            if (mission != null)
                _charContext.CharacterMissionEntries.Remove(mission);
        }

        public List<CharacterMissionObjectiveCounterEntry> GetCounters(uint characterId)
        {
            return _charContext.CreateNoTrackingQuery(_charContext.CharacterMissionObjectiveCounterEntries)
                .Where(counter => counter.CharacterId == characterId)
                .ToList();
        }

        public void UpsertCounter(uint characterId, uint missionId, uint objectiveId, byte counterId, int value)
        {
            // A counter belongs to an objective row; none may outlive (or precede) it.
            if (Live(_charContext.CharacterMissionObjectiveEntries.Find(characterId, missionId, objectiveId)) == null)
                throw new KeyNotFoundException($"character_mission_objective ({characterId}, {missionId}, {objectiveId}) does not exist");

            var entry = _charContext.CharacterMissionObjectiveCounterEntries.Find(characterId, missionId, objectiveId, counterId);

            if (entry != null && _charContext.Entry(entry).State == EntityState.Deleted)
            {
                // Deleted with its mission and written again after the mission was re-added in this unit of work.
                _charContext.Entry(entry).State = EntityState.Modified;
            }
            else if (entry == null)
            {
                _charContext.CharacterMissionObjectiveCounterEntries.Add(new CharacterMissionObjectiveCounterEntry
                {
                    CharacterId = characterId,
                    MissionId = missionId,
                    ObjectiveId = objectiveId,
                    CounterId = counterId,
                    Value = value
                });
                return;
            }

            entry.Value = value;
        }

        public void SetObjectiveTimer(uint characterId, uint missionId, uint objectiveId, long? remainingMs, long? anchorMs, bool disarmed)
        {
            var entry = Live(_charContext.CharacterMissionObjectiveEntries.Find(characterId, missionId, objectiveId));

            if (entry == null)
                throw new KeyNotFoundException($"character_mission_objective ({characterId}, {missionId}, {objectiveId}) does not exist");

            entry.TimerRemainingMs = remainingMs;
            entry.TimerAnchorMs = anchorMs;
            entry.TimerDisarmed = disarmed;
        }
    }
}
