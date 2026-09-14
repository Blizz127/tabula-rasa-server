using System.Collections.Generic;

namespace Rasa.Repositories.Char.Character
{
    using Structures.Char;
    using Structures.World;

    public interface ICharacterRepository
    {
        // start: where the character first enters the world; null keeps the default Wilderness start.
        CharacterEntry Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender, ContentLocationEntry start = null);
        CharacterEntry Get(uint id);
        IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId);
        CharacterEntry GetByAccountId(uint accountEntryId, byte slot);
        void Delete(uint id);
        void UpdateLoginData(uint id);
        void SaveCharacter(ICharacterChange characterChange);
        void UpdateCharacterAttributes(uint id, int spentBody, int spentMind, int spentSpirit);
        void UpdateCharacterClass(uint id, uint classId);
        void UpdateCharacterCloneCredits(uint id, uint cloneCredits);
        void UpdateCharacterCredits(uint id, int credits);
        void UpdateCharacterPrestige(uint id, int prestige);
        void UpdateCharacterExpirience(uint id, uint experience);
        void UpdateCharacterRewards(uint id, int credits, int prestige, uint experience);
        void UpdateCharacterLevel(uint id, byte level);
        // Staged: committed by the unit of work.
        void StageLevel(uint id, byte level);
        /// <summary>Adds a content reward to the tracked row, on top of anything already staged in this unit of work.</summary>
        void StageRewardGrant(uint id, int credits, uint experience);
        /// <summary>Moves the tracked row to a map and position without saving (a content transfer commits it with its trigger).</summary>
        void StagePosition(uint id, double x, double y, double z, double rotation, uint mapContextId);
        void UpdateCharacterLogin(uint id, uint totalTimePlayed, uint numLogins);
        void UpdateCharacterPosition(uint id, double x, double y, double z, double rotation, uint mapContextId);
        void UpdateCharacterActiveWeapon(uint id, byte activeWeapon);
        void UpdateCharacterName(uint id, string name);

        /// <summary>Whether another character already has this name, matched case-insensitively.</summary>
        bool IsCharacterNameTaken(string name, uint exceptCharacterId);
    }
}