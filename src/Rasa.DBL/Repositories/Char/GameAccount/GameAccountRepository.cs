using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.GameAccount
{
    using Context.Char;
    using Structures.Char;

    public class GameAccountRepository : IGameAccountRepository
    {
        private readonly CharContext _charContext;

        public GameAccountRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void CreateOrUpdate(uint id, string name, string email)
        {
            var existingGameAccount = _charContext.GetWritable(_charContext.GameAccountEntries, id);
            if (existingGameAccount != null)
            {
                existingGameAccount.Name = name;
                existingGameAccount.Email = email;
            }
            else
            {
                var newEntry = new GameAccountEntry
                {
                    Id = id,
                    Name = name,
                    Email = email
                };
                _charContext.GameAccountEntries.Add(newEntry);
                _charContext.SaveChanges();
            }
        }

        public GameAccountEntry Get(uint id)
        {
            var query =_charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            query = query
                .Include(e => e.Characters);
            return _charContext.FindEnsuring(query, id);
        }

        public GameAccountEntry Get(string name)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            var entry = query.Where(e => e.FamilyName == name).FirstOrDefault();

            return entry;
        }

        public GameAccountEntry Find(uint id)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            query = query
                .Include(e => e.Characters);
            return _charContext.Find(query, id);
        }

        public GameAccountEntry FindByFamilyName(string familyName)
        {
            if (string.IsNullOrEmpty(familyName))
                return null;

            // Sqlite compares with BINARY collation, so a plain == is case-sensitive there
            // while MySQL's default collation is not. Lowering both sides behaves the same on
            // both providers; the exact pass first keeps "Bob" from resolving to "bob" when
            // both exist.
            var exact = Get(familyName);
            if (exact != null)
                return exact;

            var lowered = familyName.ToLower();
            var query = _charContext.CreateNoTrackingQuery(_charContext.GameAccountEntries);
            return query.Where(e => e.FamilyName.ToLower() == lowered).FirstOrDefault();
        }

        public bool CanChangeFamilyName(uint id, string newFamilyName)
        {
            // SQLite lower() is ASCII-only; use the same Unicode comparison for both providers.
            var hasOtherAccountWithName = _charContext.GameAccountEntries.Where(e => e.Id != id)
                .Select(e => e.FamilyName).AsEnumerable()
                .Any(existing => string.Equals(existing, newFamilyName, StringComparison.OrdinalIgnoreCase));
            return !hasOtherAccountWithName;
        }

        public void UpdateFamilyName(uint id, string newFamilyName)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.FamilyName = newFamilyName;
        }

        public void UpdateLoginData(uint id, IPAddress remoteAddress)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.LastIp = remoteAddress.ToString();
            entry.LastLogin = DateTime.UtcNow;
        }

        public void UpdateSelectedSlot(uint id, byte selectedSlot)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.SelectedSlot = selectedSlot;
        }

        public void StageCanSkipBootcamp(uint id, bool canSkip)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.CanSkipBootcamp = canSkip;
        }

        public IReadOnlyList<byte> GetUnlockedRaces(uint id)
            => _charContext.AccountRaceUnlockEntries.AsNoTracking().Where(e => e.AccountId == id)
                .Select(e => e.RaceId).ToList();

        public void StageRaceUnlock(uint id, byte raceId)
        {
            if (raceId < 2 || raceId > 4)
                throw new ArgumentOutOfRangeException(nameof(raceId));
            _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            if (_charContext.AccountRaceUnlockEntries.Find(id, raceId) == null)
                _charContext.AccountRaceUnlockEntries.Add(new AccountRaceUnlockEntry { AccountId = id, RaceId = raceId });
        }

        public void UpdateAccountLevel(uint id, byte level)
        {
            var entry = _charContext.GetWritableEnsuring(_charContext.GameAccountEntries, id);
            entry.Level = level;

            _charContext.SaveChanges();
        }
    }
}
