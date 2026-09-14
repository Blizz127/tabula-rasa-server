using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Repositories;
using Rasa.Repositories.Char;
using Rasa.Repositories.Char.Character;
using Rasa.Repositories.Char.CharacterContentFact;
using Rasa.Repositories.Char.CharacterInventory;
using Rasa.Repositories.Char.CharacterLogos;
using Rasa.Repositories.Char.CharacterMission;
using Rasa.Repositories.Char.GameAccount;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// Staged character writes reach the database only through the unit of work's Complete(),
    /// so one trigger's objective, counter, fact, Logos, item, level and account changes commit together.
    /// </summary>
    [TestClass]
    public class StagedRepositoryTests
    {
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

        private static CharUnitOfWork Unit(SqliteCharContext context)
            => new CharUnitOfWork(context,
                gameAccounts: new GameAccountRepository(context),
                censoredWords: null,
                characters: new CharacterRepository(context),
                characterAbilityDrawers: null,
                characterAppearances: null,
                characterInventories: new CharacterInventoryRepository(context),
                characterLockboxes: null,
                characterLogoses: new CharacterLogosRepository(context),
                characterMissions: new CharacterMissionRepository(context),
                characterContentFacts: new CharacterContentFactRepository(context),
                characterOptions: null,
                characterSkills: null,
                characterTeleporters: null,
                characterTitles: null,
                clans: null,
                clanInventories: null,
                clanMembers: null,
                friends: null,
                ignoreds: null,
                items: null,
                petitions: null,
                userOptions: null);

        private static SqliteConnection Database()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            context.Database.Migrate();
            context.GameAccountEntries.Add(new GameAccountEntry { Id = 10, Email = "one@example.invalid", Name = "One" });
            context.CharacterEntries.Add(new CharacterEntry { Id = 101, AccountId = 10, Slot = 1, Name = "First", Level = 1 });
            context.CharacterMissionEntries.Add(new CharacterMissionEntry(101, 900100, 0));
            context.CharacterMissionObjectiveEntries.Add(new CharacterMissionObjectiveEntry { CharacterId = 101, MissionId = 900100, ObjectiveId = 1, Status = 1 });
            context.SaveChanges();
            return connection;
        }

        private static void AssertNothingStaged(SqliteConnection connection)
        {
            using var check = Context(connection);
            Assert.AreEqual(0, check.CharacterMissionObjectiveCounterEntries.Count());
            var objective = check.CharacterMissionObjectiveEntries.Single();
            Assert.IsNull(objective.TimerRemainingMs);
            Assert.IsNull(objective.TimerAnchorMs);
            Assert.IsFalse(objective.TimerDisarmed);
            Assert.AreEqual(0, check.CharacterContentFactEntries.Count());
            Assert.AreEqual(0, check.CharacterLogosEntries.Count());
            Assert.AreEqual(0, check.CharacterInventoryEntries.Count());
            Assert.AreEqual(1, check.CharacterEntries.Single().Level);
            Assert.IsFalse(check.GameAccountEntries.Single().CanSkipBootcamp);
        }

        [TestMethod]
        public void StagedWritesArePersistedOnlyByComplete()
        {
            using var connection = Database();
            using var context = Context(connection);
            var unit = Unit(context);

            unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 1);
            unit.CharacterMissions.SetObjectiveTimer(101, 900100, 1, 126000, 1757800000000, true);
            unit.CharacterContentFacts.Set(101, 1985, "bootcamp.bomb_planted", 1, 77);
            unit.CharacterLogoses.Stage(101, 23);
            unit.CharacterInventories.StageInvItem(10, 101, 1, 0, 555);
            unit.Characters.StageLevel(101, 2);
            unit.Characters.StagePosition(101, 884.1, 294.2, 347.8, 5.55, 1220);
            unit.GameAccounts.StageCanSkipBootcamp(10, true);

            AssertNothingStaged(connection);

            unit.Complete();

            using var reloaded = Context(connection);
            Assert.AreEqual(1, reloaded.CharacterMissionObjectiveCounterEntries.Single().Value);
            var objective = reloaded.CharacterMissionObjectiveEntries.Single();
            Assert.AreEqual(126000L, objective.TimerRemainingMs);
            Assert.AreEqual(1757800000000L, objective.TimerAnchorMs);
            Assert.IsTrue(objective.TimerDisarmed);
            var fact = reloaded.CharacterContentFactEntries.Single();
            Assert.AreEqual(("bootcamp.bomb_planted", 1, 77u, 1985u), (fact.FactKey, fact.Value, fact.ChangeTime, fact.MapContextId));
            Assert.AreEqual(23u, reloaded.CharacterLogosEntries.Single().LogosId);
            Assert.AreEqual(555u, reloaded.CharacterInventoryEntries.Single().ItemId);
            Assert.AreEqual(2, reloaded.CharacterEntries.Single().Level);
            var moved = reloaded.CharacterEntries.Single();
            Assert.AreEqual((884.1, 1220u), (moved.CoordX, moved.MapContextId));
            Assert.IsTrue(reloaded.GameAccountEntries.Single().CanSkipBootcamp);
        }

        [TestMethod]
        public void RewardGrantsAddToRewardsAlreadyStagedInTheSameUnitAndRefuseOverflow()
        {
            using var connection = Database();
            using var context = Context(connection);
            var unit = Unit(context);

            // A mission turn-in stages its absolute balances; a content grant in the same unit adds on top.
            unit.Characters.UpdateCharacterRewards(101, 200, 0, 1250);
            unit.Characters.StageRewardGrant(101, 50, 500);
            unit.Characters.StageRewardGrant(101, 0, 500);
            unit.Complete();

            using (var reloaded = Context(connection))
            {
                var character = reloaded.CharacterEntries.Single();
                Assert.AreEqual((250, 2250u), (character.Credit, character.Experience));
            }

            using var second = Context(connection);
            var overflow = Unit(second);
            Assert.ThrowsException<InvalidOperationException>(() => overflow.Characters.StageRewardGrant(101, int.MaxValue, 0));
            Assert.ThrowsException<KeyNotFoundException>(() => overflow.Characters.StageRewardGrant(999, 1, 1));
        }

        [TestMethod]
        public void RepeatedStagingUpdatesOneRowAndAbandonKeepsFacts()
        {
            using var connection = Database();
            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 1);
                unit.CharacterContentFacts.Set(101, 1985, "bootcamp.dropship_destroyed", 1, 10);
                unit.Complete();
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 2);
                unit.CharacterContentFacts.Set(101, 1985, "bootcamp.dropship_destroyed", 3, 11);
                unit.CharacterContentFacts.Set(101, 1985, "bootcamp.bomb_planted", 1, 11);
                unit.CharacterContentFacts.Clear(101, 1985, "bootcamp.bomb_planted");
                unit.Complete();
            }

            using (var check = Context(connection))
            {
                Assert.AreEqual(2, check.CharacterMissionObjectiveCounterEntries.Single().Value);
                var fact = check.CharacterContentFactEntries.Single();
                Assert.AreEqual(("bootcamp.dropship_destroyed", 3, 11u), (fact.FactKey, fact.Value, fact.ChangeTime));
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.Delete(101, 900100);
                using (var check = Context(connection))
                    Assert.AreEqual(1, check.CharacterMissionEntries.Count());
                unit.Complete();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(0, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveCounterEntries.Count());
            Assert.AreEqual(1, reloaded.CharacterContentFactEntries.Count());
        }

        [TestMethod]
        public void StagingFailuresPropagateAndACommitIsAllOrNothing()
        {
            using var connection = Database();
            using (var context = Context(connection))
            {
                var unit = Unit(context);
                Assert.ThrowsException<KeyNotFoundException>(() => unit.CharacterMissions.SetObjectiveTimer(101, 900100, 9, 1, 1, false));
                Assert.ThrowsException<KeyNotFoundException>(() => unit.Characters.StageLevel(999, 2));
                Assert.ThrowsException<EntityNotFoundException>(() => unit.GameAccounts.StageCanSkipBootcamp(999, true));
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterLogoses.Stage(101, 23);
                unit.Complete();
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 1);
                unit.Characters.StageLevel(101, 2);
                unit.CharacterLogoses.Stage(101, 23);                     // duplicate key: the whole commit fails
                Assert.ThrowsException<DbUpdateException>(() => unit.Complete());
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveCounterEntries.Count());
            Assert.AreEqual(1, reloaded.CharacterEntries.Single().Level);
            Assert.AreEqual(1, reloaded.CharacterLogosEntries.Count());
        }

        [TestMethod]
        public void RowsDeletedEarlierInTheSameUnitAreNotSilentlyWrittenOrLeftBehind()
        {
            using var connection = Database();
            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 1);
                unit.CharacterContentFacts.Set(101, 1985, "bootcamp.bomb_planted", 1, 5);
                unit.Complete();
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.AddObjective(new CharacterMissionObjectiveEntry { CharacterId = 101, MissionId = 900100, ObjectiveId = 2, Status = 1 });
                unit.CharacterMissions.UpsertCounter(101, 900100, 2, 0, 1);      // staged only, like objective 2
                unit.CharacterMissions.Delete(101, 900100);                     // removes stored and staged rows
                Assert.ThrowsException<KeyNotFoundException>(() => unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 3));
                Assert.ThrowsException<KeyNotFoundException>(() => unit.CharacterMissions.SetObjectiveTimer(101, 900100, 1, 1, 1, false));
                Assert.ThrowsException<KeyNotFoundException>(() => unit.CharacterMissions.UpdateObjectiveStatus(101, 900100, 1, 2));
                Assert.ThrowsException<KeyNotFoundException>(() => unit.CharacterMissions.UpdateState(101, 900100, 4, 9));

                unit.CharacterContentFacts.Clear(101, 1985, "bootcamp.bomb_planted");
                unit.CharacterContentFacts.Set(101, 1985, "bootcamp.bomb_planted", 2, 6);  // cleared and set again: kept with the new value
                unit.Complete();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(0, reloaded.CharacterMissionEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveCounterEntries.Count());
            var fact = reloaded.CharacterContentFactEntries.Single();
            Assert.AreEqual((2, 6u), (fact.Value, fact.ChangeTime));
        }

        [TestMethod]
        public void AMissionDeletedAndAddedAgainInOneUnitKeepsItsNewCounters()
        {
            using var connection = Database();
            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 1);
                unit.Complete();
            }

            using (var context = Context(connection))
            {
                var unit = Unit(context);
                unit.CharacterMissions.Delete(101, 900100);
                unit.CharacterMissions.Add(new CharacterMissionEntry(101, 900100, 0, 12),
                    new[] { new CharacterMissionObjectiveEntry { CharacterId = 101, MissionId = 900100, ObjectiveId = 1, Status = 1 } });
                unit.CharacterMissions.UpsertCounter(101, 900100, 1, 0, 0);
                unit.Complete();
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(12u, reloaded.CharacterMissionEntries.Single().ChangeTime);
            Assert.AreEqual(1, reloaded.CharacterMissionObjectiveEntries.Count());
            Assert.AreEqual(0, reloaded.CharacterMissionObjectiveCounterEntries.Single().Value);
        }
    }
}
