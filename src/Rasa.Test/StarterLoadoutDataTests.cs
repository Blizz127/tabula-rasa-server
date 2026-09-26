using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Services.Preloader;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    [TestClass]
    public class StarterLoadoutDataTests
    {
        private static Dictionary<uint, Dictionary<string, object>> Rows(IPreloader preloader)
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            preloader.Preload(builder);
            var rows = new Dictionary<uint, Dictionary<string, object>>();
            foreach (var operation in builder.Operations.OfType<InsertDataOperation>())
                for (var row = 0; row < operation.Values.GetLength(0); row++)
                    rows.Add(Convert.ToUInt32(operation.Values[row, 0]), operation.Columns
                        .Select((name, column) => (name, value: operation.Values[row, column]))
                        .ToDictionary(c => c.name, c => c.value));
            return rows;
        }

        [TestMethod]
        public void ObservedStarterLoadoutFitsOriginalSeedClassesAndEquipmentSlots()
        {
            var mapping = Rows(new ItemTemplateItemClassPreloader());
            var classes = Rows(new ItemClassPrelaoder());
            var weapons = Rows(new WeaponClassPreloader());
            var slots = Rows(new EquipableClassPreloader());
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("new-character-loadout.json")));
            foreach (var row in evidence.RootElement.GetProperty("rows").EnumerateArray())
            {
                var fields = row.GetProperty("fields");
                uint Value(string name) => fields.GetProperty(name).GetProperty("value").GetUInt32();
                var template = Value("template_id");
                var itemClass = Value("class_id");
                Assert.AreEqual(itemClass, Convert.ToUInt32(mapping[template]["itemClassId"]));
                Assert.AreEqual(Value("current_hit_points"), Convert.ToUInt32(classes[itemClass]["max_hp"]));
                Assert.IsTrue(Value("quantity") <= Convert.ToUInt32(classes[itemClass]["stack_size"]));
                if (Value("inventory_type") == 8)
                    Assert.AreEqual(Value("slot_id"), Convert.ToUInt32(slots[itemClass]["slot_id"]));
                if (Value("ammo_count") != 0)
                {
                    Assert.AreEqual(Value("ammo_count"), Convert.ToUInt32(weapons[itemClass]["clip_size"]));
                    Assert.AreEqual(3147u, Convert.ToUInt32(weapons[itemClass]["ammo_class_id"]));
                }
            }
        }

        [TestMethod]
        public void BothWorldMigrationsApplyOnlyObservedStarterTradeFlags()
        {
            using var evidence = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("new-character-loadout.json")));
            foreach (var migration in new Migration[]
            {
                new Rasa.Migrations.SqliteWorld.RecruitLoadoutFlags(),
                new Rasa.Migrations.MySqlWorld.RecruitLoadoutFlags()
            })
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                void Execute(string sql)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
                Execute("CREATE TABLE itemtemplate (id INTEGER PRIMARY KEY, has_sellable_flag INTEGER, not_tradable_flag INTEGER)");
                Execute("INSERT INTO itemtemplate VALUES (28,1,0),(145,1,0),(122854,1,0),(122855,1,0),(122856,1,0),(122875,1,0)");
                foreach (var operation in migration.UpOperations.Cast<SqlOperation>())
                    Execute(operation.Sql);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT id,has_sellable_flag,not_tradable_flag FROM itemtemplate ORDER BY id";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var template = reader.GetInt32(0);
                        if (template == 28 || template == 145)
                        {
                            Assert.AreEqual(1, reader.GetInt32(1));
                            Assert.AreEqual(0, reader.GetInt32(2));
                            continue;
                        }
                        var fields = evidence.RootElement.GetProperty("rows").EnumerateArray().Single(r =>
                            r.GetProperty("fields").GetProperty("template_id").GetProperty("value").GetInt32() == template).GetProperty("fields");
                        Assert.AreEqual(fields.GetProperty("has_sellable_flag").GetProperty("value").GetInt32(), reader.GetInt32(1));
                        Assert.AreEqual(fields.GetProperty("not_tradable_flag").GetProperty("value").GetInt32(), reader.GetInt32(2));
                    }
                }
                foreach (var operation in migration.DownOperations.Cast<SqlOperation>())
                    Execute(operation.Sql);
                using var rollback = connection.CreateCommand();
                rollback.CommandText = "SELECT COUNT(*) FROM itemtemplate WHERE has_sellable_flag = 1 AND not_tradable_flag = 0";
                Assert.AreEqual(6L, (long)rollback.ExecuteScalar());
            }
        }
    }
}
