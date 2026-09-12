namespace Rasa.Repositories.Char.CharacterLockbox
{
    using Structures.Char;

    public interface ICharacterLockboxRepository
    {
        void Add(uint id);
        CharacterLockboxEntry Get(uint accountId);
        bool TryTransferCredits(uint accountId, uint characterId, int expectedWallet, int expectedLockbox, int amount);
        void UpdateCredits(uint id, int withdraw);
        void UpdatePurashedTabs(uint id, int tabId);
    }
}
