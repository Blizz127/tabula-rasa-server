using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.CharacterSkills
{
    using Context.Char;
    using Structures.Char;


    public class CharacterSkillsRepository : ICharacterSkillsRepository
    {
        private readonly CharContext _charContext;

        public CharacterSkillsRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<CharacterSkillsEntry> GetCharacterSkills(uint characterId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterSkillsEntries);
            var characterSkills = query.Where(e => e.CharacterId == characterId).ToList();

            return characterSkills;
        }

        public void AddOrUpdate(uint characterId, uint skillId, int abilityId, int skillLevel)
        {
            AddOrUpdate(new[] { new CharacterSkillsEntry(characterId, skillId, abilityId, skillLevel) });
        }

        public void AddOrUpdate(IReadOnlyCollection<CharacterSkillsEntry> skills)
        {
            foreach (var skill in skills)
            {
                var existingSkill = _charContext.CharacterSkillsEntries.Find(skill.CharacterId, skill.SkillId);
                if (existingSkill != null)
                {
                    existingSkill.AbilityId = skill.AbilityId;
                    existingSkill.SkillLevel = skill.SkillLevel;
                }
                else
                    _charContext.CharacterSkillsEntries.Add(skill);
            }

            // EF saves the whole training batch in one transaction.
            _charContext.SaveChanges();
        }
    }
}
