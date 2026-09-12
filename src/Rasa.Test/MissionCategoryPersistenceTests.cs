using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Services.DbContext;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class MissionCategoryPersistenceTests
    {
        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteWorldContext Context(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        [TestMethod]
        public void ExistingCategoriesAndWideIdsSurviveReload()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                // SQLite already stores categories as INTEGER. Its generated
                // widening migration changes only the EF model snapshot.
                context.Database.EnsureCreated();
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) " +
                    "VALUES (900000, 0, 0, 0, 0, 255, 0, 0, 'Test legacy byte category')");
                // Synthetic records exercise storage width; they are not quest content.
                context.NpcMissionEntries.AddRange(
                    new NpcMissionEntry { Id = 900001, CategoryId = 10000044, Comment = "Test category width" },
                    new NpcMissionEntry { Id = 900002, CategoryId = uint.MaxValue, Comment = "Test unsigned boundary" });
                context.SaveChanges();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(255u, reloaded.NpcMissionEntries.Find(900000u).CategoryId);
            Assert.AreEqual(10000044u, reloaded.NpcMissionEntries.Find(900001u).CategoryId);
            Assert.AreEqual(uint.MaxValue, reloaded.NpcMissionEntries.Find(900002u).CategoryId);
            Assert.AreEqual(3, reloaded.NpcMissionEntries.Count());
        }
    }
}
