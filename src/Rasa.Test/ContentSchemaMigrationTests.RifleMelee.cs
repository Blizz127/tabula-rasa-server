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
        public void RifleMeleeMigrationChangesOnlyTheAlternateAndRollsBack()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(MissionSpeakerDialogueClassesMigration);
            var before = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            var rifle = before.Single(w => w.Id == 13713);
            Assert.AreEqual(1u, rifle.AltActionId);
            Assert.AreEqual(133u, rifle.AltActionArgId);
            Assert.AreEqual(25u, rifle.AltMaxDamage);
            Assert.AreEqual(80u, rifle.AltRange);

            migrator.Migrate(BootcampRifleMeleeMigration);
            var after = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            rifle = after.Single(w => w.Id == 13713);
            Assert.AreEqual(174u, rifle.AltActionId);
            Assert.AreEqual(5u, rifle.AltActionArgId);
            Assert.AreEqual(82u, rifle.AltMaxDamage);
            Assert.AreEqual(1u, rifle.AltDamageType);
            Assert.AreEqual(4u, rifle.AltRange);
            rifle.AltActionId = 1;
            rifle.AltActionArgId = 133;
            rifle.AltMaxDamage = 25;
            rifle.AltRange = 80;
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));

            migrator.Migrate(MissionSpeakerDialogueClassesMigration);
            var restored = context.ItemTemplateWeaponEntries.AsNoTracking().OrderBy(w => w.Id).ToArray();
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(restored));
        }
    }
}
