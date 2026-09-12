using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterMissionPersistenceTests
    {
        private const string PreviousMigration = "20230202081214_edited_character_teleporter";

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteCharContext Context(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static void AddCharacters(SqliteCharContext context)
        {
            context.GameAccountEntries.AddRange(
                new GameAccountEntry { Id = 10, Email = "one@example.invalid", Name = "One" },
                new GameAccountEntry { Id = 20, Email = "two@example.invalid", Name = "Two" });
            context.CharacterEntries.AddRange(
                new CharacterEntry { Id = 101, AccountId = 10, Slot = 0, Name = "First" },
                new CharacterEntry { Id = 102, AccountId = 10, Slot = 1, Name = "Second" },
                new CharacterEntry { Id = 201, AccountId = 20, Slot = 0, Name = "Third" });
            context.SaveChanges();
        }

        [TestMethod]
        public void ReadsOnlyTheRequestedAccountAndSlotWithoutTracking()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                AddCharacters(context);
                context.CharacterMissionEntries.AddRange(
                    new CharacterMissionEntry(101, 429, 1),
                    new CharacterMissionEntry(101, 321, 2),
                    new CharacterMissionEntry(102, 429, 3),
                    new CharacterMissionEntry(201, 429, 4),
                    new CharacterMissionEntry(999, 429, 5));
                context.SaveChanges();
            }

            using var reloaded = Context(connection);
            var repository = new CharacterMissionRepository(reloaded);
            var missions = repository.Get(10, 0);
            CollectionAssert.AreEquivalent(new uint[] { 429, 321 }, missions.Select(m => m.MissionId).ToArray());
            Assert.IsTrue(missions.All(m => m.CharacterId == 101));
            Assert.AreEqual(3u, repository.Get(10, 1).Single().MissionState);
            Assert.AreEqual(4u, repository.Get(20, 0).Single().MissionState);
            Assert.AreEqual(0, repository.Get(20, 1).Count);
            Assert.AreEqual(0, repository.Get(99, 0).Count);
            Assert.AreEqual(0, reloaded.ChangeTracker.Entries().Count());

            missions.Single(m => m.MissionId == 429).MissionState = 123;
            reloaded.SaveChanges();
            Assert.AreEqual(1u, repository.Get(10, 0).Single(m => m.MissionId == 429).MissionState);
        }

        [TestMethod]
        public void UpdatingOneMissionPreservesOtherMissionsAndOtherCharacters()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                context.CharacterMissionEntries.AddRange(
                    new CharacterMissionEntry(101, 429, 1),
                    new CharacterMissionEntry(101, 321, 2),
                    new CharacterMissionEntry(201, 429, 3));
                context.SaveChanges();
            }

            using (var context = Context(connection))
            {
                context.CharacterMissionEntries.Find(101u, 429u).MissionState = 4;
                context.SaveChanges();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(3, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(4u, reloaded.CharacterMissionEntries.Find(101u, 429u).MissionState);
            Assert.AreEqual(2u, reloaded.CharacterMissionEntries.Find(101u, 321u).MissionState);
            Assert.AreEqual(3u, reloaded.CharacterMissionEntries.Find(201u, 429u).MissionState);
        }

        [TestMethod]
        public void DatabaseRejectsDuplicateCharacterAndMissionIdentity()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(101, 429, 1));
                context.SaveChanges();
            }

            using (var context = Context(connection))
            {
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(101, 429, 2));
                Assert.ThrowsException<DbUpdateException>(() => context.SaveChanges());
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(1u, reloaded.CharacterMissionEntries.Single().MissionState);
        }

        [TestMethod]
        public void MigrationPreservesLegacyRowsAndAllowsAdditionalMissions()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.GetService<IMigrator>().Migrate(PreviousMigration);
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission (character_id, mission_id, mission_state) VALUES (101, 429, 4294967295), (201, 321, 7)");
                context.Database.Migrate();
                Assert.AreEqual(uint.MaxValue, context.CharacterMissionEntries.Find(101u, 429u).MissionState);
                Assert.AreEqual(7u, context.CharacterMissionEntries.Find(201u, 321u).MissionState);
                context.CharacterMissionEntries.Add(new CharacterMissionEntry(101, 321, 2));
                context.SaveChanges();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(3, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(uint.MaxValue, reloaded.CharacterMissionEntries.Find(101u, 429u).MissionState);
            Assert.AreEqual(2u, reloaded.CharacterMissionEntries.Find(101u, 321u).MissionState);
            Assert.AreEqual(7u, reloaded.CharacterMissionEntries.Find(201u, 321u).MissionState);
            Assert.AreEqual(0, reloaded.Database.GetPendingMigrations().Count());
        }

        [TestMethod]
        public void CompatibleDowngradePreservesLegacyMissionRows()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            context.Database.Migrate();
            context.Database.ExecuteSqlRaw(
                "INSERT INTO character_mission (character_id, mission_id, mission_state) VALUES (101, 429, 3), (201, 321, 7)");
            context.GetService<IMigrator>().Migrate(PreviousMigration);
            context.Database.Migrate();
            Assert.AreEqual(2, context.CharacterMissionEntries.Count());
            Assert.AreEqual(3u, context.CharacterMissionEntries.Find(101u, 429u).MissionState);
            Assert.AreEqual(7u, context.CharacterMissionEntries.Find(201u, 321u).MissionState);
        }

        [TestMethod]
        public void IncompatibleSqliteDowngradeFailsWithoutDiscardingMissions()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.Migrate();
                context.CharacterMissionEntries.AddRange(
                    new CharacterMissionEntry(101, 429, 3),
                    new CharacterMissionEntry(101, 321, 7));
                context.SaveChanges();
                Assert.ThrowsException<SqliteException>(() =>
                    context.GetService<IMigrator>().Migrate(PreviousMigration));
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(2, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(3u, reloaded.CharacterMissionEntries.Find(101u, 429u).MissionState);
            Assert.AreEqual(7u, reloaded.CharacterMissionEntries.Find(101u, 321u).MissionState);
            Assert.AreEqual(0, reloaded.Database.GetPendingMigrations().Count());
        }
    }
}
