using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    public partial class ContentSchemaMigrationTests
    {
        [TestMethod]
        public void RifleRangeMigrationChangesOneFieldAndExactlyRollsBackThroughSqliteMigrator()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(BootcampFirstLoginYawMigration);
            var before = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            Assert.AreEqual(80u, before.Single(w => w.Id == 13713).Range);

            // Execute actual EF-generated provider SQL using the frozen target model.
            migrator.Migrate(BootcampCrateRifleRangeMigration);
            var after = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            Assert.AreEqual(60u, after.Single(w => w.Id == 13713).Range);
            // Compare every weapon row and field after accounting for the single intended change.
            after.Single(w => w.Id == 13713).Range = 80;
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));

            migrator.Migrate(BootcampFirstLoginYawMigration);
            var restored = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(restored));
            Assert.AreEqual(BootcampFirstLoginYawMigration, context.Database.GetAppliedMigrations().Last());
        }
    }
}
