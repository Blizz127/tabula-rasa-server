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
            // Later compatible migrations may already have been reverted before the
            // key conflict stops the downgrade; re-applying them must not lose rows.
            reloaded.Database.Migrate();
            Assert.AreEqual(2, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(3u, reloaded.CharacterMissionEntries.Find(101u, 429u).MissionState);
            Assert.AreEqual(7u, reloaded.CharacterMissionEntries.Find(101u, 321u).MissionState);
            Assert.AreEqual(0, reloaded.Database.GetPendingMigrations().Count());
        }

        private const string CompositeKeyMigration = "20260912172745_CharacterMissionCompositeKey";

        [TestMethod]
        public void ObjectiveProgressMigrationKeepsExistingMissionRows()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.GetService<IMigrator>().Migrate(CompositeKeyMigration);
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission (character_id, mission_id, mission_state) VALUES (101, 429, 4294967295), (101, 321, 0), (201, 429, 4)");
                context.Database.Migrate();
            }

            using (var context = Context(connection))
            {
                Assert.AreEqual(0, context.Database.GetPendingMigrations().Count());
                Assert.AreEqual(3, context.CharacterMissionEntries.Count());
                Assert.AreEqual(uint.MaxValue, context.CharacterMissionEntries.Find(101u, 429u).MissionState);
                Assert.AreEqual(4u, context.CharacterMissionEntries.Find(201u, 429u).MissionState);
                Assert.IsTrue(context.CharacterMissionEntries.All(m => m.ChangeTime == 0));
                Assert.AreEqual(0, context.CharacterMissionObjectiveEntries.Count());

                context.CharacterMissionObjectiveEntries.AddRange(
                    new CharacterMissionObjectiveEntry(101, 429, 5, 2),
                    new CharacterMissionObjectiveEntry(101, 429, 4, 1),
                    new CharacterMissionObjectiveEntry(201, 429, 5, 1));
                context.SaveChanges();
            }

            using (var context = Context(connection))
            {
                context.CharacterMissionObjectiveEntries.Add(new CharacterMissionObjectiveEntry(101, 429, 5, 1));
                Assert.ThrowsException<DbUpdateException>(() => context.SaveChanges());
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(2u, reloaded.CharacterMissionObjectiveEntries.Find(101u, 429u, 5u).Status);
        }

        [TestMethod]
        public void ObjectiveProgressDowngradeKeepsMissionRowsForThePreviousServer()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.Migrate();
                context.CharacterMissionEntries.AddRange(
                    new CharacterMissionEntry(101, 429, 0, 1700000000),
                    new CharacterMissionEntry(101, 321, 4, 1700000100));
                context.CharacterMissionObjectiveEntries.Add(new CharacterMissionObjectiveEntry(101, 429, 5, 1));
                context.SaveChanges();

                context.GetService<IMigrator>().Migrate(CompositeKeyMigration);
                Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM character_mission"));
                Assert.AreEqual(4L, Scalar(connection, "SELECT mission_state FROM character_mission WHERE mission_id = 321"));
                Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM sqlite_master WHERE name = 'character_mission_objective'"));

                // The previous server inserts rows without the new column; after upgrade they default to 0.
                context.Database.ExecuteSqlRaw("INSERT INTO character_mission (character_id, mission_id, mission_state) VALUES (102, 429, 0)");
                context.Database.Migrate();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(3, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(0u, reloaded.CharacterMissionEntries.Find(102u, 429u).ChangeTime);
            Assert.AreEqual(0, reloaded.Database.GetPendingMigrations().Count());
        }

        [TestMethod]
        public void ObjectiveReadsAreScopedToTheCharacterAndDeletesToTheMission()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                var repository = new CharacterMissionRepository(context);
                repository.Add(new CharacterMissionEntry(101, 429, 0, 10), new[]
                    { new CharacterMissionObjectiveEntry(101, 429, 5, 2), new CharacterMissionObjectiveEntry(101, 429, 4, 1) });
                repository.Add(new CharacterMissionEntry(101, 321, 0, 11), new[] { new CharacterMissionObjectiveEntry(101, 321, 1, 1) });
                repository.Add(new CharacterMissionEntry(201, 429, 0, 12), new[] { new CharacterMissionObjectiveEntry(201, 429, 5, 1) });
                context.SaveChanges();
            }

            using (var context = Context(connection))
            {
                var repository = new CharacterMissionRepository(context);
                CollectionAssert.AreEquivalent(new uint[] { 5, 4, 1 }, repository.GetObjectives(101).Select(o => o.ObjectiveId).ToArray());
                Assert.AreEqual(0, context.ChangeTracker.Entries().Count());
                repository.UpdateObjectiveStatus(201, 429, 5, 2);
                repository.UpdateState(201, 429, 4, 99);
                repository.Delete(101, 429);
                context.SaveChanges();
                Assert.ThrowsException<System.Collections.Generic.KeyNotFoundException>(() => repository.UpdateState(999, 429, 1, 1));
            }

            using var reloaded = Context(connection);
            CollectionAssert.AreEquivalent(new uint[] { 321, 429 }, reloaded.CharacterMissionEntries.Select(m => m.MissionId).ToArray());
            Assert.IsNull(reloaded.CharacterMissionEntries.Find(101u, 429u));
            Assert.AreEqual(1u, reloaded.CharacterMissionObjectiveEntries.Single(o => o.CharacterId == 101).ObjectiveId);
            Assert.AreEqual(2u, reloaded.CharacterMissionObjectiveEntries.Find(201u, 429u, 5u).Status);
            Assert.AreEqual((4u, 99u), (reloaded.CharacterMissionEntries.Find(201u, 429u).MissionState, reloaded.CharacterMissionEntries.Find(201u, 429u).ChangeTime));
        }

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }
    }
}
