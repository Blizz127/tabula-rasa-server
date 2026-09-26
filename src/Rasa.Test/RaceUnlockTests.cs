using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.Game.Server;
using Rasa.Repositories.Char.GameAccount;
using Rasa.Structures.Char;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class RaceUnlockTests
    {
        [TestMethod]
        public void MissionMappingMatchesTheEvidenceManifest()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("race-unlocks.json")));
            var rows = document.RootElement.GetProperty("unlocks").EnumerateArray().ToArray();
            Assert.AreEqual(3, rows.Length);
            foreach (var row in rows)
                Assert.AreEqual((Race)row.GetProperty("race_id").GetByte(),
                    RaceUnlocks.GrantedByMission(row.GetProperty("mission_id").GetUInt32()));
            Assert.IsNull(RaceUnlocks.GrantedByMission(1995));
        }

        [TestMethod]
        public void SelectionOnlyOffersHumanAndValidPersistedUnlocks()
        {
            CollectionAssert.AreEqual(new[] { Race.Human }, new BeginCharacterSelectionPacket("", false, 10).EnabledRaceList);
            CollectionAssert.AreEqual(new[] { Race.Human, Race.Forean, Race.Thrax },
                new BeginCharacterSelectionPacket("", true, 10, false, new byte[] { 4, 0, 2, 4, 255 }).EnabledRaceList);
            Assert.IsFalse(RaceUnlocks.IsEnabled((Race)255, new byte[] { 255 }));
        }

        [TestMethod]
        public void MigrationBackfillsOnlyCompletedMissionsAndUnlockSurvivesCharacterDeletion()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = WeaponReloadPersistenceTests.Context(connection);
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate("20260913180522_MissionContentRuntimeState");
            context.GameAccountEntries.AddRange(new GameAccountEntry { Id = 10, Name = "One", Email = "one@example.invalid" },
                new GameAccountEntry { Id = 20, Name = "Two", Email = "two@example.invalid" });
            context.CharacterEntries.AddRange(new CharacterEntry { Id = 101, AccountId = 10, Slot = 1, Name = "First" },
                new CharacterEntry { Id = 102, AccountId = 10, Slot = 2, Name = "Second" },
                new CharacterEntry { Id = 201, AccountId = 20, Slot = 1, Name = "Third", Race = 4 });
            context.CharacterMissionEntries.AddRange(new CharacterMissionEntry(101, 1861, 4), new CharacterMissionEntry(102, 1861, 4),
                new CharacterMissionEntry(101, 1851, 4), new CharacterMissionEntry(101, 1899, 1));
            context.SaveChanges();
            migrator.Migrate();
            var accounts = new GameAccountRepository(context);
            CollectionAssert.AreEquivalent(new byte[] { 2, 3 }, accounts.GetUnlockedRaces(10).ToArray());
            Assert.AreEqual(0, accounts.GetUnlockedRaces(20).Count);
            accounts.StageRaceUnlock(10, 2);
            accounts.StageRaceUnlock(10, 4);
            accounts.StageRaceUnlock(10, 4);
            context.SaveChanges();
            migrator.Migrate("20260922110000_AccountRaceUnlocks");
            migrator.Migrate();
            CollectionAssert.AreEquivalent(new byte[] { 2, 3, 4 }, accounts.GetUnlockedRaces(10).ToArray());
            context.CharacterEntries.RemoveRange(context.CharacterEntries.Where(c => c.AccountId == 10));
            context.SaveChanges();
            CollectionAssert.AreEquivalent(new byte[] { 2, 3, 4 }, accounts.GetUnlockedRaces(10).ToArray());
            migrator.Migrate("20260913180522_MissionContentRuntimeState");
            migrator.Migrate();
            Assert.AreEqual(0, accounts.GetUnlockedRaces(10).Count);
        }
    }
}
