using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class PreloaderBatchingTests
    {
        [TestMethod]
        public void ItemClassMappingSeedKeepsTheOriginalOrderedInt32Pairs()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            new ItemTemplateItemClassPreloader().Preload(builder);
            using var bytes = new MemoryStream();
            using var writer = new BinaryWriter(bytes);
            var count = 0;
            foreach (var operation in builder.Operations.Cast<InsertDataOperation>())
            {
                Assert.AreEqual("itemtemplate_itemclass", operation.Table);
                CollectionAssert.AreEqual(new[] { "itemTemplateId", "itemClassId" }, operation.Columns);
                Assert.AreEqual(2, operation.Values.GetLength(1));
                for (var row = 0; row < operation.Values.GetLength(0); row++, count++)
                    for (var column = 0; column < 2; column++)
                    {
                        Assert.IsInstanceOfType(operation.Values[row, column], typeof(int));
                        writer.Write((int)operation.Values[row, column]);
                    }
            }
            writer.Flush();
            using var sha = SHA256.Create();
            var hash = BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-", "").ToLowerInvariant();
            Assert.AreEqual(30225, count);
            // Independently extracted from the pre-rewrite literal yield expressions;
            // encoding and conversion record: docs/evidence/item-class-seed-parity.json.
            Assert.AreEqual("53f5689cd904ded92245e319edf939be66c6a8577b8de066155468b2a65d605a", hash);
        }

        private sealed class SeedContext : DbContext
        {
            private readonly SqliteConnection _connection;
            public SeedContext(SqliteConnection connection) => _connection = connection;
            protected override void OnConfiguring(DbContextOptionsBuilder builder) => builder.UseSqlite(_connection);
            protected override void OnModelCreating(ModelBuilder builder) => builder.Entity<ItemTemplateEntry>();
        }

        [DataTestMethod]
        [DataRow("Microsoft.EntityFrameworkCore.Sqlite")]
        [DataRow("Pomelo.EntityFrameworkCore.MySql")]
        public void LargeItemSeedPreservesEveryRowColumnAndValueInOrder(string provider)
        {
            var preloader = new ItemTemplateRegeneratedPreloader();
            var originalRows = ((IEnumerable<object[]>)typeof(ItemTemplateRegeneratedPreloader)
                .GetMethod("GetRows", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(preloader, null)).ToArray();
            Assert.AreEqual(30225, originalRows.Length);
            var columns = typeof(ItemTemplateEntry).GetProperties()
                .SelectMany(p => p.GetCustomAttributes<ColumnAttribute>()).Select(a => a.Name).ToArray();
            var builder = new MigrationBuilder(provider);
            preloader.Preload(builder);
            var operations = builder.Operations.Cast<InsertDataOperation>().ToArray();
            Assert.IsTrue(operations.Length > 1, "The original 30,225-row SQL command must be split.");
            var index = 0;
            foreach (var operation in operations)
            {
                Assert.AreEqual("itemtemplate", operation.Table);
                CollectionAssert.AreEqual(columns, operation.Columns);
                Assert.IsTrue(operation.Values.GetLength(0) <= 256);
                for (var row = 0; row < operation.Values.GetLength(0); row++, index++)
                    for (var column = 0; column < columns.Length; column++)
                        Assert.AreEqual(originalRows[index][column], operation.Values[row, column],
                            $"Original seed row {index}, column {columns[column]}");
            }
            Assert.AreEqual(originalRows.Length, index);
        }

        [TestMethod]
        public void SqliteSeedCommandsAreBoundedAndRemainInOneRollbackableTransaction()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = new SeedContext(connection);
            context.Database.EnsureCreated();
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            new ItemTemplateRegeneratedPreloader().Preload(builder);
            var commands = context.GetService<IMigrationsSqlGenerator>().Generate(builder.Operations, context.Model);
            Assert.AreEqual(builder.Operations.Count, commands.Count,
                "Separate insert operations must remain separate SQL commands.");
            Assert.IsTrue(commands.All(c => !c.TransactionSuppressed));
            using (context.Database.BeginTransaction())
            {
                foreach (var command in commands)
                    context.Database.ExecuteSqlRaw(command.CommandText);
                Assert.AreEqual(30225, context.Set<ItemTemplateEntry>().Count());
                Assert.AreEqual((byte)2, context.Set<ItemTemplateEntry>().Single(i => i.Id == 122875).QualityId);
                // Dispose without commit, as a later failing migration would roll back.
            }
            Assert.AreEqual(0, context.Set<ItemTemplateEntry>().Count());
        }
    }
}
