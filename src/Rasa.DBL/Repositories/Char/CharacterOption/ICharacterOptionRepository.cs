using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterOption
{
    using Structures.Char;

    public interface ICharacterOptionRepository
    {
        void AddOrUpdate(uint accountId, uint optionId, string value);
        void Replace(uint characterId, IReadOnlyDictionary<uint, string> values);
        List<CharacterOptionEntry> Get(uint id);
    }
}
