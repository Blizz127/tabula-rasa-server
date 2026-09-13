using System.Collections.Generic;

namespace Rasa.Repositories.Char.CharacterLogos
{
    public interface ICharacterLogosRepository
    {
        List<uint> GetLogos(uint characterId);
        void SetLogos(uint characterId, uint logosId);
        // Staged: committed by the unit of work; failures propagate.
        void Stage(uint characterId, uint logosId);
    }
}
