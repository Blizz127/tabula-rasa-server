using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.World;
using Rasa.Services.DbContext;
using Rasa.Services.Preloader;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class NpcPackagePersistenceTests
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

        private static void Apply(SqliteWorldContext context, IReadOnlyList<MigrationOperation> operations)
        {
            var generator = context.GetService<IMigrationsSqlGenerator>();
            foreach (var command in generator.Generate(operations, context.Model))
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        [TestMethod]
        public void FreshNpcSeedsBindRogersAndWitherspoonToOriginalDialoguePackages()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            context.Database.EnsureCreated();
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            new NpcPackagePrelader().Preload(builder);
            Apply(context, builder.Operations);

            Assert.AreEqual(2, context.NpcPackageEntries.Count());
            Assert.AreEqual(116u, context.NpcPackageEntries.Find(100u).PackageId);
            Assert.AreEqual(208u, context.NpcPackageEntries.Find(101u).PackageId);
        }

        [DataTestMethod]
        [DataRow(false, 726u, 116u)]
        [DataRow(false, 116u, 116u)]
        [DataRow(false, 999u, 999u)]
        [DataRow(true, 726u, 116u)]
        [DataRow(true, 116u, 116u)]
        [DataRow(true, 999u, 999u)]
        public void CorrectionTargetsOnlyTheKnownRogersPackage(bool mySqlMigration, uint oldPackage, uint expectedPackage)
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Context(connection))
            {
                context.Database.EnsureCreated();
                context.NpcPackageEntries.AddRange(
                    new NpcPackageEntry { Id = 100, PackageId = oldPackage, Comment = "Rogers fixture" },
                    new NpcPackageEntry { Id = 101, PackageId = 208, Comment = "Witherspoon fixture" },
                    new NpcPackageEntry { Id = 112, PackageId = 726, Comment = "Unrelated fixture" });
                context.SaveChanges();
                Migration migration = mySqlMigration
                    ? new Rasa.Migrations.MySqlWorld.CorrectRogersNpcPackage()
                    : new Rasa.Migrations.SqliteWorld.CorrectRogersNpcPackage();
                // Both providers carry the same portable data correction. These
                // checks execute each migration's actual operations in SQLite.
                Apply(context, migration.UpOperations);
                Apply(context, migration.UpOperations);
            }

            using var reloaded = Context(connection);
            Assert.AreEqual(3, reloaded.NpcPackageEntries.Count());
            Assert.AreEqual(expectedPackage, reloaded.NpcPackageEntries.Find(100u).PackageId);
            Assert.AreEqual("Rogers fixture", reloaded.NpcPackageEntries.Find(100u).Comment);
            Assert.AreEqual(208u, reloaded.NpcPackageEntries.Find(101u).PackageId);
            Assert.AreEqual(726u, reloaded.NpcPackageEntries.Find(112u).PackageId);
        }
    }
}
