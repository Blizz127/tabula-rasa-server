using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterContentFact
{
    using Context.Char;
    using Structures.Char;

    /// <summary>
    /// Per-character content facts. Writes are staged; the caller commits them with the
    /// rest of the triggering change through the unit of work.
    /// </summary>
    public interface ICharacterContentFactRepository
    {
        List<CharacterContentFactEntry> Get(uint characterId);
        void Set(uint characterId, uint mapContextId, string factKey, int value, uint changeTime);
        void Clear(uint characterId, uint mapContextId, string factKey);
    }

    public class CharacterContentFactRepository : ICharacterContentFactRepository
    {
        private readonly CharContext _charContext;

        public CharacterContentFactRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterContentFactEntry> Get(uint characterId)
        {
            return _charContext.CreateNoTrackingQuery(_charContext.CharacterContentFactEntries)
                .Where(fact => fact.CharacterId == characterId)
                .ToList();
        }

        public void Set(uint characterId, uint mapContextId, string factKey, int value, uint changeTime)
        {
            var entry = _charContext.CharacterContentFactEntries.Find(characterId, mapContextId, factKey);

            if (entry != null && _charContext.Entry(entry).State == EntityState.Deleted)
            {
                // Cleared and set again in one unit of work: the fact stays, with the new value.
                _charContext.Entry(entry).State = EntityState.Modified;
            }
            else if (entry == null)
            {
                _charContext.CharacterContentFactEntries.Add(new CharacterContentFactEntry
                {
                    CharacterId = characterId,
                    MapContextId = mapContextId,
                    FactKey = factKey,
                    Value = value,
                    ChangeTime = changeTime
                });
                return;
            }

            entry.Value = value;
            entry.ChangeTime = changeTime;
        }

        public void Clear(uint characterId, uint mapContextId, string factKey)
        {
            var entry = _charContext.CharacterContentFactEntries.Find(characterId, mapContextId, factKey);

            if (entry != null)
                _charContext.CharacterContentFactEntries.Remove(entry);
        }
    }
}
