using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Config;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Repositories.Char.Character;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;
using Rasa.Structures.Content;
using Rasa.Structures.World;

namespace Rasa.Test
{
    /// <summary>
    /// The boot-camp entry switch is an operational lever. The entry path (S1) is implemented, so the
    /// switch decides: Disabled and unlisted accounts still start in the Wilderness, everyone else at the
    /// lowest live new-character start location.
    /// </summary>
    [TestClass]
    public class BootcampEntryGateTests
    {
        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private sealed class AllReferences : IContentReferences
        {
            public bool MapContextExists(uint mapContextId) => true;
            public bool MissionExists(uint missionId) => true;
            public bool ObjectiveExists(uint missionId, uint objectiveId) => true;
            public uint MissionGiver(uint missionId) => 0;
            public bool MissionOfferable(uint missionId) => true;
            public bool CreatureExists(uint creatureId) => true;
            public bool EntityClassExists(uint entityClassId) => true;
            public bool ItemTemplateExists(uint itemTemplateId) => true;
            public bool LogosExists(uint logosId) => true;
            public bool HasLegacyWorldObjects(uint mapContextId) => false;
        }

        private static ContentValidation WithLocations(uint withheldContext = 0)
        {
            var catalog = new MissionContentCatalog(
                withheldContext == 0
                    ? Enumerable.Empty<ContentMapSettingEntry>()
                    : new[] { new ContentMapSettingEntry { MapContextId = withheldContext, Instancing = 255 } },
                Enumerable.Empty<NpcMissionPrerequisiteEntry>(), Enumerable.Empty<NpcMissionObjectiveBindingEntry>(),
                Enumerable.Empty<NpcMissionObjectiveCounterEntry>(), Enumerable.Empty<NpcMissionObjectiveTimerEntry>(), Enumerable.Empty<NpcMissionObjectiveIndicatorEntry>(),
                Enumerable.Empty<ContentAreaEntry>(), Enumerable.Empty<ContentPlacementEntry>(), Enumerable.Empty<ContentConditionEntry>(),
                Enumerable.Empty<ContentRuleEntry>(), Enumerable.Empty<ContentRuleActionEntry>(), Enumerable.Empty<ContentItemSetEntry>(),
                new[]
                {
                    new ContentLocationEntry { Id = 900005, Purpose = (byte)ContentLocationPurpose.TransferDestination, MapContextId = 1220 },
                    new ContentLocationEntry { Id = 900002, Purpose = (byte)ContentLocationPurpose.NewCharacterStart, MapContextId = 1985, PosX = 387.2, PosY = 0, PosZ = -79.1 },
                    new ContentLocationEntry { Id = 900001, Purpose = (byte)ContentLocationPurpose.NewCharacterStart, MapContextId = 1985, PosX = 1, PosY = 2, PosZ = 3, Rotation = 0.5 }
                });
            return catalog.Validate(new AllReferences(), ContentCapabilities.All);
        }

        [TestMethod]
        public void DefaultConfigurationIsDisabled()
        {
            Assert.AreEqual(BootcampEntryMode.Disabled, new BootcampConfig().EntryMode);
            Assert.AreEqual(0, new BootcampConfig().AccountIds.Length);
            Assert.AreEqual(BootcampEntryMode.Disabled, new GameDataConfig().Bootcamp.EntryMode);
            Assert.AreEqual(BootcampEntryMode.Disabled, new MissionContentManager(null).Bootcamp.EntryMode);
        }

        [TestMethod]
        public void TheEntrySwitchIsHonouredOnceTheEntryPathIsImplemented()
        {
            var content = WithLocations();
            var allowListed = new BootcampConfig { EntryMode = BootcampEntryMode.AllowListedAccounts, AccountIds = new uint[] { 10 } };

            Assert.IsNull(BootcampEntryGate.StartLocation(new BootcampConfig(), 10, content));                       // Disabled
            Assert.IsNull(BootcampEntryGate.StartLocation(allowListed, 20, content));                                // account not listed
            Assert.AreEqual(900001u, BootcampEntryGate.StartLocation(allowListed, 10, content).Id);                  // listed
            Assert.AreEqual(900001u, BootcampEntryGate.StartLocation(new BootcampConfig { EntryMode = BootcampEntryMode.AllNewCharacters }, 10, content).Id);

            // The server's default switch is Disabled, so creating a character still starts in the Wilderness.
            Assert.IsNull(new MissionContentManager(null).NewCharacterStart(10));
        }

        [TestMethod]
        public void OnceImplementedTheSwitchSelectsAccountsAndTheLowestLiveStartLocation()
        {
            var content = WithLocations();
            var allowListed = new BootcampConfig { EntryMode = BootcampEntryMode.AllowListedAccounts, AccountIds = new uint[] { 10, 30 } };
            var everyone = new BootcampConfig { EntryMode = BootcampEntryMode.AllNewCharacters };

            Assert.IsNull(BootcampEntryGate.StartLocation(new BootcampConfig(), 10, content, entryImplemented: true));
            Assert.IsNull(BootcampEntryGate.StartLocation(null, 10, content, entryImplemented: true));
            Assert.IsNull(BootcampEntryGate.StartLocation(allowListed, 20, content, entryImplemented: true));
            Assert.IsNull(BootcampEntryGate.StartLocation(new BootcampConfig { EntryMode = BootcampEntryMode.AllowListedAccounts, AccountIds = null }, 10, content, entryImplemented: true));
            Assert.AreEqual(900001u, BootcampEntryGate.StartLocation(allowListed, 10, content, entryImplemented: true).Id);
            Assert.AreEqual(900001u, BootcampEntryGate.StartLocation(everyone, 20, content, entryImplemented: true).Id);
            Assert.IsNull(BootcampEntryGate.StartLocation(everyone, 20, MissionContentCatalog.Empty.Validate(new AllReferences()), entryImplemented: true));

            // Start locations in a withheld context are never used.
            var withheld = WithLocations(withheldContext: 1985);
            CollectionAssert.AreEquivalent(new uint[] { 900001, 900002 }, withheld.WithheldLocations.ToArray());
            Assert.IsNull(BootcampEntryGate.StartLocation(everyone, 20, withheld, entryImplemented: true));
        }

        [TestMethod]
        public void CreationUsesTheGivenStartLocationOrTheWildernessStart()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = new SqliteCharContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());
            context.Database.Migrate();
            var account = new GameAccountEntry { Id = 10, Email = "one@example.invalid", Name = "One" };
            context.GameAccountEntries.Add(account);
            context.SaveChanges();
            var characters = new CharacterRepository(context);

            var wilderness = characters.Create(account, 1, "Recruit", 1, 1.0, 0, new MissionContentManager(null).NewCharacterStart(account.Id));
            Assert.AreEqual((1220u, 894.9d, 307.9d, 347.1d, 0d), (wilderness.MapContextId, wilderness.CoordX, wilderness.CoordY, wilderness.CoordZ, wilderness.Rotation));

            var start = WithLocations().Catalog.Locations[900001];
            var placed = characters.Create(account, 2, "Other", 1, 1.0, 0, start);
            Assert.AreEqual((1985u, 1d, 2d, 3d, 0.5d), (placed.MapContextId, placed.CoordX, placed.CoordY, placed.CoordZ, placed.Rotation));
        }
    }
}
