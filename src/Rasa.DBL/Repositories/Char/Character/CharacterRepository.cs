using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Character
{
    using Context.Char;
    using Structures.Char;

    public class CharacterRepository : ICharacterRepository
    {
        private const uint DefaultMapContextId = 1220;
        private const double DefaultCoordX = 894.9d;
        private const double DefaultCoordY = 307.9d;
        private const double DefaultCoordZ = 347.1d;
        private const byte DefaultRunState = 1;

        private readonly CharContext _charContext;

        public CharacterRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public CharacterEntry Create(GameAccountEntry account, byte slot, string characterName, byte race, double scale, byte gender)
        {
            var entry = new CharacterEntry
            {
                AccountId = account.Id,
                Slot = slot,
                Name = characterName,
                Race = race,
                Scale = scale,
                Gender = gender,
                Class = 1,
                MapContextId = DefaultMapContextId,
                RunState = DefaultRunState,
                CoordX = DefaultCoordX,
                CoordY = DefaultCoordY,
                CoordZ = DefaultCoordZ,
                Rotation = 0
            };

            try
            {
                _charContext.CharacterEntries.Add(entry);
                _charContext.SaveChanges();
                return Get(entry.Id);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error creating character:");
                Logger.WriteLog(LogType.Error, e);
                return null;
            }
        }

        public CharacterEntry Get(uint id)
        {
            var query = CreateCharacterQuery();
            return _charContext.FindEnsuring(query, id);
        }

        public IDictionary<byte, CharacterEntry> GetByAccountId(uint accountEntryId)
        {
            var query = CreateCharacterQuery();
            var characters = query.Where(e => e.AccountId == accountEntryId).OrderBy(e => e.Id);

            // Not ToDictionary: that throws on a duplicate slot, and this runs inside the login
            // handler, so one bad row used to disconnect the account at every login. Rows written
            // before the slot check and the unique index can still be duplicated; the oldest
            // character keeps the pod and the rest are reported so they can be moved by hand.
            var bySlot = new Dictionary<byte, CharacterEntry>();

            foreach (var character in characters)
            {
                if (bySlot.TryAdd(character.Slot, character))
                    continue;

                Logger.WriteLog(LogType.Error,
                    $"Account {accountEntryId} has more than one character in slot {character.Slot}; "
                    + $"character {character.Id} ({character.Name}) is hidden behind {bySlot[character.Slot].Id}. "
                    + "Move it to a free slot: UPDATE `character` SET slot = <n> WHERE id = " + character.Id + ";");
            }

            return bySlot;
        }

        /// <summary>
        /// The character in one of an account's pods, or null when the pod is empty. An empty
        /// pod is an ordinary answer - an account with no character in its selected slot, a
        /// switch to a slot nothing was created in - so this does not throw; it used to, and
        /// every caller checked for null instead, so the check never ran and the throw took
        /// the connection down.
        /// </summary>
        public CharacterEntry GetByAccountId(uint accountEntryId, byte slot)
        {
            var query = CreateCharacterQuery();

            return query.FirstOrDefault(e => e.AccountId == accountEntryId && e.Slot == slot);
        }

        private IQueryable<CharacterEntry> CreateCharacterQuery()
        {

            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            query = query
                .Include(e => e.GameAccount)
                .Include(e => e.CharacterAppearance)
                .Include(e => e.MemberOfClan)
                .ThenInclude(e => e.Clan);
            return query;
        }

        public void Delete(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            _charContext.Remove(entry);
        }

        public void UpdateLoginData(uint id)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, id);
            entry.LastLogin = DateTime.UtcNow;
        }

        public void SaveCharacter(ICharacterChange characterChange)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.CharacterEntries, characterChange.Id);
            entry.RunState = characterChange.IsRunning ? (byte)1 : (byte)0;
            entry.CrouchState = characterChange.IsCrouching ? (byte)1 : (byte)0;

            entry.CoordX = characterChange.Position.X;
            entry.CoordY = characterChange.Position.Y;
            entry.CoordZ = characterChange.Position.Z;
            entry.Rotation = characterChange.Rotation;
            entry.MapContextId= characterChange.MapContextId;
        }

        public void UpdateCharacterAttributes(uint id, int spentBody, int spentMind, int spentSpirit)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Body = spentBody;
            entry.Mind = spentMind;
            entry.Spirit = spentSpirit;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterClass(uint id, uint classId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Class = classId;
 
            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterCloneCredits(uint id, uint cloneCredits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.CloneCredits = cloneCredits;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterCredits(uint id, int credits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Credit = credits;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterExpirience(uint id, uint experience)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Experience = experience;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterLevel(uint id, byte level)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.Level = level;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterLogin(uint id, uint totalTimePlayed, uint numLogins)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.LastLogin = DateTime.UtcNow;
            entry.TotalTimePlayed = totalTimePlayed;
            entry.NumLogins = numLogins;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterPosition(uint id, double x, double y, double z, double rotation, uint mapContextId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.CoordX = x;
            entry.CoordY = y;
            entry.CoordZ = z;
            entry.Rotation = rotation;
            entry.MapContextId = mapContextId;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterActiveWeapon(uint id, byte activeWeapon)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            entry.ActiveWeapon = activeWeapon;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdateCharacterName(uint id, string name)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterEntries);
            var entry = query.Where(e => e.Id == id).FirstOrDefault();

            if (entry == null)
                return;

            entry.Name = name;

            _charContext.CharacterEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public bool IsCharacterNameTaken(string name, uint exceptCharacterId)
        {
            // ToLower on both sides: SQLite compares strings with BINARY collation, so = is
            // case-sensitive there while MySQL's default collation is not.
            var lowered = name.ToLower();

            return _charContext.CharacterEntries.Any(e => e.Id != exceptCharacterId && e.Name.ToLower() == lowered);
        }
    }
}