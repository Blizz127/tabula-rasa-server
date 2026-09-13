using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// Loads mission definitions from the objective tables through the real
    /// world repositories. All rows are synthetic storage fixtures.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionDefinitionLoadingTests
    {
        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        public class WorldProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_NpcMissions": return new NpcMissionRepository(Context);
                    case "get_NpcMissionObjectives": return new NpcMissionObjectiveRepository(Context);
                    case "get_NpcMissionRewards": return new NpcMissionRewardRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
                ((WorldProxy)(object)unit).Context = Context(_connection);
                return unit;
            }
        }

        private static SqliteWorldContext Context(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()),
                new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        [TestMethod]
        public void DefinitionsAssembleObjectivesBindingsTransitionsAndCurrencyRewards()
        {
            var oldLogger = Logger.Config;
            if (oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            try
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    context.Database.EnsureCreated();
                    context.NpcMissionEntries.AddRange(
                        new NpcMissionEntry { Id = 900100, GiverId = 1, ReciverId = 2, Level = 3, GroupType = 1, CategoryId = 10000044, Comment = "complete fixture" },
                        new NpcMissionEntry { Id = 900200, GiverId = 1, ReciverId = 2, Level = 5, GroupType = 1, CategoryId = 1, Comment = "no objectives" });
                    context.NpcMissionObjectiveEntries.AddRange(
                        new NpcMissionObjectiveEntry { MissionId = 900100, ObjectiveId = 5, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "" },
                        new NpcMissionObjectiveEntry { MissionId = 900100, ObjectiveId = 4, Ordinal = 2, IsRequired = true, RevealedOnAccept = false, Comment = "" },
                        new NpcMissionObjectiveEntry { MissionId = 999999, ObjectiveId = 1, Ordinal = 1, IsRequired = true, RevealedOnAccept = true, Comment = "orphan" });
                    context.NpcMissionObjectiveConversationEntries.AddRange(
                        new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 5, NpcPackageId = 2584, PlayerFlagId = 1 },
                        new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 4, NpcPackageId = 116, PlayerFlagId = 1 },
                        new NpcMissionObjectiveConversationEntry { MissionId = 900100, ObjectiveId = 9, NpcPackageId = 116, PlayerFlagId = 1 });
                    context.NpcMissionObjectiveTransitionEntries.AddRange(
                        new NpcMissionObjectiveTransitionEntry { MissionId = 900100, CompletedObjectiveId = 5, RevealedObjectiveId = 4 },
                        new NpcMissionObjectiveTransitionEntry { MissionId = 900100, CompletedObjectiveId = 5, RevealedObjectiveId = 8 });
                    context.SaveChanges();
                    context.Database.ExecuteSqlRaw(
                        "INSERT INTO npc_mission_reward (id, type, credits, item_template_id, quantity) VALUES (900100, 1, 250, 0, 0), (900100, 2, 15, 0, 0), (900100, 3, 1000, 0, 0)");
                }

                var manager = new MissionManager(new Factory(connection));
                manager.LoadMissions();

                Assert.AreEqual(2, manager.LoadedMissions.Count);
                var mission = manager.LoadedMissions[900100];
                CollectionAssert.AreEqual(new uint[] { 5, 4 }, mission.ObjectivesInOrder.Select(o => o.ObjectiveId).ToArray());
                Assert.AreEqual(2, mission.ObjectiveConversations.Count);          // binding to unknown objective 9 is ignored
                Assert.IsTrue(mission.HasObjectiveConversation(5, 2584, 1));
                Assert.IsFalse(mission.HasObjectiveConversation(5, 2584, 2));
                CollectionAssert.AreEqual(new uint[] { 4 }, mission.Transitions[5]);  // transition to unknown objective 8 is ignored
                Assert.AreEqual(3, mission.Rewards.Count);
                Assert.AreEqual(250u, mission.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Credits]);
                Assert.AreEqual(15u, mission.MissionConstantData.RewardInfo.FixedReward.Credits[CurencyType.Prestige]);
                Assert.AreEqual(0, mission.MissionConstantData.RewardInfo.FixedReward.FixedItems.Count);
                Assert.AreEqual(5u, mission.ObjectivesList.Single().ObjectiveId);    // dispense list: revealed on acceptance only
                Assert.AreEqual(1u, mission.ObjectivesList.Single().Ordinal);
                Assert.IsTrue(mission.IsDispensable);

                Assert.IsFalse(manager.LoadedMissions[900200].IsDispensable);
                CollectionAssert.AreEqual(new[] { "no objectives" }, manager.LoadedMissions[900200].DefinitionGaps());
            }
            finally
            {
                if (oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            }
        }
    }
}
