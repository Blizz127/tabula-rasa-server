using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    public partial class ContentSchemaMigrationTests
    {
        [TestMethod]
        public void FirstLoginYawMigrationOnlyConvertsTheArrivalRotationAndExactlyRollsBack()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(BootcampPracticeDummyHealthMigration);
            var before = context.ContentLocationEntries.AsNoTracking().OrderBy(l => l.Id).ToArray();
            Assert.AreEqual(6.02139, before.Single(l => l.Id == 19851).Rotation);

            migrator.Migrate(BootcampFirstLoginYawMigration);
            var after = context.ContentLocationEntries.AsNoTracking().OrderBy(l => l.Id).ToArray();
            Assert.AreEqual(2.879793, after.Single(l => l.Id == 19851).Rotation);
            // Compare every location and field after accounting for the sole intended change.
            after.Single(l => l.Id == 19851).Rotation = 6.02139;
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));

            migrator.Migrate(BootcampPracticeDummyHealthMigration);
            var restored = context.ContentLocationEntries.AsNoTracking().OrderBy(l => l.Id).ToArray();
            Assert.AreEqual(JsonSerializer.Serialize(before), JsonSerializer.Serialize(restored));

            foreach (Migration migration in new Migration[]
                { new Rasa.Migrations.SqliteWorld.BootcampFirstLoginYaw(), new Rasa.Migrations.MySqlWorld.BootcampFirstLoginYaw() })
            {
                Assert.IsNotNull(migration.TargetModel.FindEntityType("Rasa.Structures.World.ContentLocationEntry"),
                    "A data-only migration still needs a frozen target model for later rollback SQL generation.");
                foreach (var operations in new[] { migration.UpOperations, migration.DownOperations })
                {
                    Assert.AreEqual(1, operations.Count);
                    var update = (UpdateDataOperation)operations.Single();
                    Assert.AreEqual("content_location", update.Table);
                    CollectionAssert.AreEqual(new[] { "id" }, update.KeyColumns);
                    Assert.AreEqual(19851u, update.KeyValues[0, 0]);
                    CollectionAssert.AreEqual(new[] { "rotation" }, update.Columns);
                    Assert.AreEqual(operations == migration.UpOperations ? 2.879793 : 6.02139, update.Values[0, 0]);
                }
            }
        }
    }
}
