using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Reconstruction
{
    [TestClass]
    public class BootcampCourtyardWarriorProvenanceTests
    {
        [TestMethod]
        public void EverySeededWarriorValueMatchesItsEvidenceRecord()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(
                EvidenceLocator.EvidenceFile("bootcamp-courtyard-forean-warrior.json")));
            var root = document.RootElement;
            var sources = root.GetProperty("sources");
            var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            Rasa.Migrations.BootcampData.BootcampCourtyardForeanWarriorRows.InsertData(builder);
            var inserts = builder.Operations.OfType<InsertDataOperation>().ToArray();
            Assert.AreEqual(3, inserts.Length);

            foreach (var row in root.GetProperty("rows").EnumerateArray())
            {
                var table = row.GetProperty("table").GetString();
                var key = row.GetProperty("key");
                var id = key.GetProperty("id").GetUInt32();
                var insert = inserts.Single(operation => operation.Table == table);
                Assert.AreEqual(1, insert.Values.GetLength(0));
                var idColumn = Array.IndexOf(insert.Columns, "id");
                Assert.IsTrue(idColumn >= 0, table);
                Assert.AreEqual(id, Convert.ToUInt32(insert.Values[0, idColumn]), table);

                var documented = row.GetProperty("fields").EnumerateObject().ToArray();
                foreach (var field in documented)
                {
                    var index = Array.IndexOf(insert.Columns, field.Name);
                    Assert.IsTrue(index >= 0, $"{table}.{field.Name} is documented but not seeded");
                    var evidence = field.Value;
                    var tier = evidence.GetProperty("tier").GetString();
                    Assert.IsTrue(new[] { "original", "observed", "measured", "inferred", "analogue" }.Contains(tier),
                        $"{table}.{field.Name}: unknown provenance tier {tier}");
                    foreach (var source in evidence.GetProperty("source").GetString().Split(new[] { ", " }, StringSplitOptions.None))
                        Assert.IsTrue(sources.TryGetProperty(source, out _), $"{table}.{field.Name}: missing source {source}");
                    if (tier == "measured")
                        Assert.IsTrue(evidence.TryGetProperty("uncertainty", out _), $"{table}.{field.Name}: no measurement uncertainty");

                    var expected = evidence.GetProperty("value").GetDecimal();
                    var actual = Convert.ToDecimal(insert.Values[0, index], CultureInfo.InvariantCulture);
                    Assert.AreEqual(expected, actual, $"{table}.{field.Name}");
                }

                // A nonzero gameplay value must have a field record. Zero denotes
                // the schema's neutral default; primary keys and comments are storage.
                for (var column = 0; column < insert.Columns.Length; column++)
                {
                    var name = insert.Columns[column];
                    if (name == "id" || name == "slot_id" || name == "comment")
                        continue;
                    var value = insert.Values[0, column];
                    if (value is IConvertible && Convert.ToDecimal(value, CultureInfo.InvariantCulture) != 0)
                        Assert.IsTrue(documented.Any(field => field.Name == name), $"{table}.{name} lacks field provenance");
                }
            }
        }
    }
}
