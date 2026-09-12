using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Repositories.Char.CharacterSkills;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    [TestClass]
    public class CharacterSkillPersistenceTests
    {
        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteCharContext Context(SqliteConnection connection)
        {
            return new SqliteCharContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());
        }

        [TestMethod]
        public void BatchInsertsAndUpdatesSurviveReloadWithoutChangingOtherCharacters()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                var repository = new CharacterSkillsRepository(context);
                repository.AddOrUpdate(new[] { new CharacterSkillsEntry(1, 1, -1, 1), new CharacterSkillsEntry(2, 1, -1, 4) });
                repository.AddOrUpdate(new[] { new CharacterSkillsEntry(1, 1, -1, 2), new CharacterSkillsEntry(1, 49, 194, 1) });
            }

            using var reloaded = Context(connection);
            var skills = new CharacterSkillsRepository(reloaded).GetCharacterSkills(1);
            Assert.AreEqual(2, skills.Count);
            Assert.AreEqual(2, skills.Single(s => s.SkillId == 1).SkillLevel);
            Assert.AreEqual(194, skills.Single(s => s.SkillId == 49).AbilityId);
            Assert.AreEqual(4, reloaded.CharacterSkillsEntries.Single(s => s.CharacterId == 2).SkillLevel);
        }

        [TestMethod]
        public void FailedSaveRollsBackEntireTrainingBatch()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                var repository = new CharacterSkillsRepository(context);
                repository.AddOrUpdate(new[] { new CharacterSkillsEntry(1, 1, -1, 1), new CharacterSkillsEntry(1, 8, -1, 1) });
                // Fail the second update at the database boundary, after the first
                // update has been attempted, to exercise transaction rollback.
                context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_test_skill BEFORE UPDATE ON character_skills WHEN NEW.skill_id = 8 BEGIN SELECT RAISE(ABORT, 'test failure'); END;");
                Assert.ThrowsException<DbUpdateException>(() => repository.AddOrUpdate(new[]
                {
                    new CharacterSkillsEntry(1, 1, -1, 2),
                    new CharacterSkillsEntry(1, 8, -1, 2)
                }));
            }

            using var reloaded = Context(connection);
            var skills = new CharacterSkillsRepository(reloaded).GetCharacterSkills(1);
            Assert.AreEqual(2, skills.Count);
            Assert.IsTrue(skills.All(s => s.SkillLevel == 1));
        }
    }
}
