using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Repositories.Char.Items;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    [TestClass]
    public class WeaponReloadPersistenceTests
    {
        internal sealed class Configuration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public Configuration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        internal static SqliteCharContext Context(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()),
                new Configuration(connection), new SqliteDbContextPropertyModifier());

        internal static void Seed(SqliteCharContext context)
        {
            context.Database.EnsureCreated();
            context.ItemEntries.AddRange(
                new ItemEntry { ItemId = 1, ItemTemplateId = 1, AmmoCount = 5, StackSize = 1, CrafterName = "" },
                new ItemEntry { ItemId = 2, ItemTemplateId = 2, StackSize = 7, CrafterName = "" },
                new ItemEntry { ItemId = 3, ItemTemplateId = 2, StackSize = 25, CrafterName = "" });
            context.CharacterInventoryEntries.AddRange(
                new CharacterInventoryEntry(10, 101, 9, 0, 1),
                new CharacterInventoryEntry(10, 101, 1, 50, 2),
                new CharacterInventoryEntry(10, 101, 1, 51, 3));
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static ReloadAmmoChange[] Changes() => new[]
        {
            new ReloadAmmoChange(2, 50, 7, 7), new ReloadAmmoChange(3, 51, 25, 18)
        };

        [TestMethod]
        public void MultiStackTransferPersistsMagazineAndRemainingStackTogether()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                Seed(context);
                Assert.IsTrue(new ItemRepository(context).TryReloadWeapon(10, 101, 1, 5, 30, Changes()));
            }
            using var reloaded = Context(connection);
            Assert.AreEqual(30u, reloaded.ItemEntries.Find(1u).AmmoCount);
            Assert.AreEqual(0u, reloaded.ItemEntries.Find(2u).StackSize);
            Assert.AreEqual(7u, reloaded.ItemEntries.Find(3u).StackSize);
            Assert.IsNull(reloaded.CharacterInventoryEntries.Find(2u));
            Assert.AreEqual(51u, reloaded.CharacterInventoryEntries.Find(3u).SlotId);
        }

        [DataTestMethod]
        [DataRow("second_stack")]
        [DataRow("second_owner")]
        [DataRow("second_slot")]
        [DataRow("weapon_owner")]
        [DataRow("weapon_ammo")]
        public void StaleTransferRollsBackEveryEarlierWrite(string changed)
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            Seed(context);
            if (changed == "second_stack") context.ItemEntries.Find(3u).StackSize = 24;
            if (changed == "second_owner") context.CharacterInventoryEntries.Find(3u).CharacterId = 202;
            if (changed == "second_slot") context.CharacterInventoryEntries.Find(3u).SlotId = 52;
            if (changed == "weapon_owner") context.CharacterInventoryEntries.Find(1u).AccountId = 20;
            if (changed == "weapon_ammo") context.ItemEntries.Find(1u).AmmoCount = 4;
            context.SaveChanges();
            context.ChangeTracker.Clear();
            Assert.IsFalse(new ItemRepository(context).TryReloadWeapon(10, 101, 1, 5, 30, Changes()));
            Assert.AreEqual(changed == "weapon_ammo" ? 4u : 5u, context.ItemEntries.Find(1u).AmmoCount);
            Assert.AreEqual(7u, context.ItemEntries.Find(2u).StackSize);
            Assert.AreEqual(changed == "second_stack" ? 24u : 25u, context.ItemEntries.Find(3u).StackSize);
            Assert.AreEqual(3, context.CharacterInventoryEntries.Count());
        }

        [DataTestMethod]
        [DataRow("duplicate")]
        [DataRow("overdraw")]
        [DataRow("mismatch")]
        [DataRow("empty")]
        public void InvalidTransfersCannotCreateOrDestroyAmmunition(string kind)
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            Seed(context);
            var changes = kind switch
            {
                "duplicate" => new[] { new ReloadAmmoChange(2, 50, 7, 7), new ReloadAmmoChange(2, 50, 7, 7) },
                "overdraw" => new[] { new ReloadAmmoChange(2, 50, 7, 25) },
                "empty" => new ReloadAmmoChange[0],
                _ => new[] { new ReloadAmmoChange(2, 50, 7, 7) }
            };
            Assert.IsFalse(new ItemRepository(context).TryReloadWeapon(10, 101, 1, 5, 30, changes));
            Assert.AreEqual(5u, context.ItemEntries.Find(1u).AmmoCount);
            Assert.AreEqual(7u, context.ItemEntries.Find(2u).StackSize);
            Assert.AreEqual(25u, context.ItemEntries.Find(3u).StackSize);
            Assert.AreEqual(3, context.CharacterInventoryEntries.Count());
        }
    }
}
