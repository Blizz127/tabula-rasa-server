using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Repositories.Char.Items;
using Rasa.Structures;
using Rasa.Structures.Char;
using Rasa.Structures.World;

namespace Rasa.Test
{
    [TestClass]
    public class ItemInstanceMetadataTests
    {
        [TestMethod]
        public void ItemInfoSnapshotsOrderedIdsAndEffectiveFlagsWithoutInventingWireLevels()
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 13713, ItemClass = 27220 })
            {
                HasSellableFlag = true, InventoryCategory = InventoryCategory.Equipment, QualityId = 3
            };
            template.ItemInfo.Tradable = true;
            var item = new Item
            {
                ItemTemplate = template, Crafter = "Fixture", CurrentHitPoints = 100,
                LootModules = new[] { new ItemLootModule(900178, null), new ItemLootModule(900312, 2) },
                TradableOverride = false, SellableOverride = false
            };
            var plain = new Item { ItemTemplate = template };
            Assert.IsTrue(plain.IsTradable);
            Assert.IsTrue(plain.IsSellable);
            var classInfo = new EntityClass(27220, "fixture", 0, 0, new List<AugmentationType>(), false)
            {
                ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 100 })
            };
            var packet = new ItemInfoPacket(item, classInfo);
            item.LootModules = new[] { new ItemLootModule(1, 99) };
            item.TradableOverride = true;
            item.SellableOverride = true;

            using var stream = new MemoryStream();
            using var writer = new PythonWriter(new BinaryWriter(stream));
            packet.Write(writer);
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(15, reader.ReadTuple());
            Assert.AreEqual(100, reader.ReadInt());
            Assert.AreEqual(100, reader.ReadInt());
            Assert.AreEqual("Fixture", reader.ReadString());
            Assert.AreEqual(13713u, reader.ReadUInt());
            Assert.IsFalse(reader.ReadBool()); // effective hasSellableFlag
            Assert.IsFalse(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual(0, reader.ReadList()); // class modules remain separate
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(900178, reader.ReadInt());
            Assert.AreEqual(900312, reader.ReadInt());
            Assert.AreEqual(3, reader.ReadInt());
            Assert.IsFalse(reader.ReadBool());
            Assert.IsTrue(reader.ReadBool()); // original negative notTradable flag
            Assert.IsFalse(reader.ReadBool());
            Assert.AreEqual((int)InventoryCategory.Equipment, reader.ReadInt());
            Assert.AreEqual(stream.Length, stream.Position);
        }

        [TestMethod]
        public void RepositoryCreatesAndUpdatesInstanceMetadataWithoutChangingTheOtherItemOrAmmo()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            uint customizedId, plainId;
            var modules = new[] { new ItemLootModule(31, 0), new ItemLootModule(30, null) };
            using (var context = WeaponReloadPersistenceTests.Context(connection))
            {
                context.Database.EnsureCreated();
                var repository = new ItemRepository(context);
                customizedId = repository.CreateItem(new Item
                {
                    ItemTemplateId = 13713, Crafter = "", StackSize = 1, CurrentAmmo = 17,
                    LootModules = modules, TradableOverride = false, SellableOverride = true
                });
                plainId = repository.CreateItem(new Item { ItemTemplateId = 13713, Crafter = "", StackSize = 1 });
                Assert.AreNotEqual(0u, customizedId);
                Assert.AreNotEqual(0u, plainId);
            }
            using (var context = WeaponReloadPersistenceTests.Context(connection))
            {
                var repository = new ItemRepository(context);
                var entry = repository.GetItem(customizedId);
                CollectionAssert.AreEqual(modules, ItemLootModules.Deserialize(entry.LootModulesJson).ToArray());
                Assert.AreEqual(false, entry.TradableOverride);
                Assert.AreEqual(true, entry.SellableOverride);
                var item = new Item { Id = customizedId };
                item.RestoreInstanceMetadata(entry);
                item.LootModules = modules.Reverse().ToArray();
                item.TradableOverride = null;
                repository.UpdateInstanceMetadata(item);
            }
            using (var context = WeaponReloadPersistenceTests.Context(connection))
            {
                var entry = context.ItemEntries.Find(customizedId);
                CollectionAssert.AreEqual(modules.Reverse().ToArray(), ItemLootModules.Deserialize(entry.LootModulesJson).ToArray());
                Assert.IsNull(entry.TradableOverride);
                Assert.AreEqual(17u, entry.AmmoCount);
                var plain = context.ItemEntries.Find(plainId);
                Assert.IsNull(plain.LootModulesJson);
                Assert.IsNull(plain.TradableOverride);
                Assert.IsNull(plain.SellableOverride);
            }
        }

        [TestMethod]
        public void SqliteMigrationPreservesLegacyRowsAndRollsBackThroughTheFrozenModel()
        {
            const string previous = "20260922110100_BackfillRaceUnlocks";
            const string current = "20260922233000_ItemInstanceMetadata";
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = WeaponReloadPersistenceTests.Context(connection);
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(previous);
            context.Database.ExecuteSqlRaw("INSERT INTO items (item_id,item_template_id,stack_size,current_hp,color,ammo_count,crafter_name,created_at) VALUES (1,13713,1,100,123,17,'Legacy','2009-02-28 00:00:00'),(2,28,1000,1,456,0,'','2009-02-28 00:00:00')");
            var before = LegacyRows(connection);
            migrator.Migrate(current);
            Assert.AreEqual(before, LegacyRows(connection));
            foreach (var entry in context.ItemEntries.AsNoTracking())
            {
                Assert.IsNull(entry.LootModulesJson);
                Assert.IsNull(entry.TradableOverride);
                Assert.IsNull(entry.SellableOverride);
            }
            context.Database.ExecuteSqlRaw(
                "UPDATE items SET loot_modules={0},tradable_override=0,sellable_override=1 WHERE item_id=1",
                "[{\"ModuleId\":31,\"Level\":null}]");
            migrator.Migrate(previous);
            Assert.AreEqual(before, LegacyRows(connection));
            Assert.AreEqual(previous, context.Database.GetAppliedMigrations().Last());
            migrator.Migrate(current);
            Assert.IsNull(context.ItemEntries.AsNoTracking().Single(row => row.ItemId == 1).LootModulesJson,
                "Downgrade intentionally removes only the added metadata; it cannot recover it on a subsequent upgrade.");
        }

        [TestMethod]
        public void BothProvidersAddOnlyNullableInstanceFieldsAndHaveFrozenTargetModels()
        {
            foreach (var migration in new Migration[]
            {
                new Rasa.Migrations.SqliteChar.ItemInstanceMetadata(),
                new Rasa.Migrations.MySqlChar.ItemInstanceMetadata()
            })
            {
                var columns = migration.UpOperations.Cast<AddColumnOperation>().ToArray();
                CollectionAssert.AreEquivalent(new[] { "loot_modules", "tradable_override", "sellable_override" }, columns.Select(c => c.Name).ToArray());
                Assert.IsTrue(columns.All(c => c.Table == "items" && c.IsNullable && c.DefaultValue == null));
                Assert.AreEqual(3, migration.DownOperations.OfType<DropColumnOperation>().Count());
                var model = migration.TargetModel.FindEntityType(typeof(ItemEntry));
                Assert.IsNotNull(model);
                Assert.IsTrue(model.FindProperty(nameof(ItemEntry.LootModulesJson)).IsNullable);
                Assert.IsTrue(model.FindProperty(nameof(ItemEntry.TradableOverride)).IsNullable);
                Assert.IsTrue(model.FindProperty(nameof(ItemEntry.SellableOverride)).IsNullable);
            }
        }

        private static string LegacyRows(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT item_id,item_template_id,stack_size,current_hp,color,ammo_count,crafter_name,created_at FROM items ORDER BY item_id";
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
            {
                var fields = new object[reader.FieldCount];
                reader.GetValues(fields);
                rows.Add(System.Text.Json.JsonSerializer.Serialize(fields));
            }
            return string.Join("\n", rows);
        }
    }
}
