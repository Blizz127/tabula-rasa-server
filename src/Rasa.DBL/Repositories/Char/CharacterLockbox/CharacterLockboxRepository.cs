using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Repositories.Char.CharacterLockbox
{
    using Context.Char;
    using Structures.Char;

    public class CharacterLockboxRepository : ICharacterLockboxRepository
    {
        private readonly CharContext _charContext;

        public CharacterLockboxRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public void Add(uint id)
        {
            _charContext.Add(new CharacterLockboxEntry(id, 0, 1));
            _charContext.SaveChanges();
        }

        public CharacterLockboxEntry Get(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var lockboxInfo = query.FirstOrDefault(e => e.AccountId == accountId);

            return lockboxInfo;
        }

        // A positive amount deposits; a negative amount withdraws. Compare both
        // balances so a stale session cannot overwrite another character's bank.
        public bool TryTransferCredits(uint accountId, uint characterId, int expectedWallet, int expectedLockbox, int amount)
        {
            var wallet = (long)expectedWallet - amount;
            var lockbox = (long)expectedLockbox + amount;
            if (accountId == 0 || characterId == 0 || amount == 0 || expectedWallet < 0 || expectedLockbox < 0 ||
                wallet < 0 || wallet > int.MaxValue || lockbox < 0 || lockbox > int.MaxValue)
                return false;

            using var transaction = _charContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            if (_charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character SET credit = {(int)wallet}
                WHERE id = {characterId} AND account_id = {accountId} AND credit = {expectedWallet}") != 1)
                return false;
            if (_charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character_lockbox SET credits = {(int)lockbox}
                WHERE account_id = {accountId} AND credits = {expectedLockbox}") != 1)
                return false;
            transaction.Commit();
            return true;
        }

        public bool TryPurchaseTab(uint accountId, uint characterId, int expectedWallet, int expectedTabs, int tabId, int price)
        {
            if (accountId == 0 || characterId == 0 || expectedTabs < 1 || expectedTabs >= 5 ||
                tabId != expectedTabs + 1 || price <= 0 || expectedWallet < price)
                return false;
            using var transaction = _charContext.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
            if (_charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character SET credit = {expectedWallet - price}
                WHERE id = {characterId} AND account_id = {accountId} AND credit = {expectedWallet}") != 1)
                return false;
            if (_charContext.Database.ExecuteSqlInterpolated($@"
                UPDATE character_lockbox SET purashed_tabs = {tabId}
                WHERE account_id = {accountId} AND purashed_tabs = {expectedTabs}") != 1)
                return false;
            transaction.Commit();
            return true;
        }

        public void UpdateCredits(uint accountId, int credits)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var entry = query.FirstOrDefault(e => e.AccountId == accountId);

            entry.Credits = credits;
            _charContext.CharacterLockboxEntries.Update(entry);
            _charContext.SaveChanges();
        }

        public void UpdatePurashedTabs(uint accountId, int purashedTabs)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.CharacterLockboxEntries);
            var entry = query.FirstOrDefault(e => e.AccountId == accountId);

            entry.PurashedTabs = purashedTabs;
            _charContext.CharacterLockboxEntries.Update(entry);
            _charContext.SaveChanges();
        }
    }
}
