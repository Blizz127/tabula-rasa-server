using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
using Rasa.Migrations.WildernessData;
using Rasa.Services.DbContext;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// CreatureLootFootage against the footage ledger (docs/evidence/creature-loot-footage-ledger.json): every seeded
    /// rate and stack range is recomputed here from the ledger's own drops and kill windows, the ammunition rows do not
    /// follow the killer's weapon because the ledger refutes that, every seeded value is the one its manifest row records,
    /// and the rollback puts the emulator's cartridge rows back exactly.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CreatureLootFootageTests
    {
        private static JsonDocument Ledger() =>
            JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("creature-loot-footage-ledger.json")));

        private static JsonDocument Manifest() =>
            JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));

        private static List<JsonElement> Drops(JsonDocument ledger) =>
            ledger.RootElement.GetProperty("drops").EnumerateArray().Concat(ledger.RootElement.GetProperty("pravus_drops").EnumerateArray()).ToList();

        private static string Item(JsonElement drop) => drop.GetProperty("item").GetProperty("text").GetString();

        private static string Category(JsonElement drop) => drop.GetProperty("item").GetProperty("category").GetString();

        /// <summary>Credited kills and the drops of the windows of one population, straight from the ledger.</summary>
        private static (int Kills, List<JsonElement> Drops) Population(JsonDocument ledger, string population)
        {
            var byId = ledger.RootElement.GetProperty("drops").EnumerateArray().ToDictionary(d => d.GetProperty("id").GetString());
            var windows = ledger.RootElement.GetProperty("kill_windows").EnumerateArray()
                .Where(w => w.GetProperty("population").GetString() == population).ToList();
            var kills = windows.Sum(w => w.GetProperty("credited_kills").GetInt32());
            var drops = windows.SelectMany(w => w.GetProperty("drops").EnumerateArray().Select(id => byId[id.GetString()])).ToList();
            return (kills, drops);
        }

        [TestMethod]
        public void TheSeededRatesAreTheLedgersCounts()
        {
            using var ledger = Ledger();

            // Every drop sits in exactly one kill window, and every window's drop is in the ledger.
            var windowed = ledger.RootElement.GetProperty("kill_windows").EnumerateArray()
                .SelectMany(w => w.GetProperty("drops").EnumerateArray().Select(id => id.GetString())).ToList();
            Assert.AreEqual(windowed.Count, windowed.Distinct().Count(), "a drop is counted in two windows");

            var (kills, drops) = Population(ledger, "Thrax Infantry Initiate");
            var skulls = drops.Where(d => Item(d) == "Thrax Skull").ToList();
            var ammo = drops.Where(d => Category(d) == "ammo").ToList();
            Assert.AreEqual((42, 22, 4), (kills, skulls.Count, ammo.Count));
            Assert.AreEqual(Math.Round(100.0 * skulls.Count / kills, 2), CreatureLootFootageRows.ThraxSkullChance);
            Assert.IsTrue(skulls.All(d => d.GetProperty("quantity").GetInt32() == 1), "every Thrax Skull line reads 1");
            Assert.AreEqual(Math.Round(100.0 * ammo.Count / kills / CreatureLootFootageRows.Ammo.Length, 2), CreatureLootFootageRows.AmmoChancePerType);

            var (boargarKills, boargarDrops) = Population(ledger, "Young Forest Boargar");
            var ears = boargarDrops.Where(d => Item(d) == "Boargar Ear").ToList();
            Assert.AreEqual(Math.Round(100.0 * ears.Count / boargarKills, 2), CreatureLootFootageRows.BoargarEarChance);
            Assert.AreEqual((1, 2), (ears.Min(d => d.GetProperty("quantity").GetInt32()), ears.Max(d => d.GetProperty("quantity").GetInt32())));

            // The ledger's own statistics block says the same.
            var stats = ledger.RootElement.GetProperty("statistics").GetProperty("thrax_infantry_initiate");
            Assert.AreEqual(CreatureLootFootageRows.ThraxSkullChance, stats.GetProperty("thrax_skull").GetProperty("per_kill_percent").GetDouble());
            Assert.AreEqual(kills, stats.GetProperty("credited_kills").GetInt32());
        }

        [TestMethod]
        public void EachAmmunitionTypeTakesTheStackRangeObservedOfIt()
        {
            using var ledger = Ledger();
            var ammo = Drops(ledger).Where(d => Category(d) == "ammo").ToList();
            Assert.AreEqual(19, ammo.Count);

            foreach (var (template, min, max, label) in CreatureLootFootageRows.Ammo)
            {
                var stacks = ammo.Where(d => Item(d) == label).Select(d => d.GetProperty("quantity").GetInt32()).ToList();
                Assert.IsTrue(stacks.Count >= 2, $"{label}: at least two observed stacks");
                Assert.AreEqual(((int)min, (int)max), (stacks.Min(), stacks.Max()), label);
                Assert.IsTrue(ammo.Where(d => Item(d) == label).All(d => d.GetProperty("item").GetProperty("template_id").GetUInt32() == template), label);
            }

            // Every observed ammunition type is one the rows carry, and no row is for a type never seen.
            CollectionAssert.AreEquivalent(ammo.Select(Item).Distinct().ToList(), CreatureLootFootageRows.Ammo.Select(a => a.Label).ToList());
        }

        [TestMethod]
        public void TheAmmunitionDropDoesNotFollowTheKillersWeapon()
        {
            using var ledger = Ledger();
            var attributable = Drops(ledger).Where(d => Category(d) == "ammo" && d.GetProperty("receiver").GetString() == "player"
                && d.GetProperty("player_weapon").GetProperty("ammo_class_id").ValueKind == JsonValueKind.Number).ToList();
            var mismatched = attributable.Count(d => d.GetProperty("item").GetProperty("class_id").GetUInt32()
                != d.GetProperty("player_weapon").GetProperty("ammo_class_id").GetUInt32());

            // Five of seven are a type the receiver's weapon cannot fire, so a weapon-matched rule would be invented.
            Assert.AreEqual((7, 5), (attributable.Count, mismatched));
            var verdict = ledger.RootElement.GetProperty("hypotheses").EnumerateArray()
                .Single(h => h.GetProperty("id").GetString() == "H-AMMO-MATCHES-WEAPON").GetProperty("verdict").GetString();
            Assert.AreEqual("refuted", verdict);

            // So each Thrax creature carries all five types at one chance, and nothing about the killer enters the roll.
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            CreatureLootFootageRows.InsertData(migration);
            foreach (var creature in CreatureLootFootageRows.ThraxCreatures)
            {
                var rows = Inserted(migration).Where(r => (uint)r["creature_id"] == creature && CreatureLootFootageRows.Ammo.Any(a => a.Template == (uint)r["item_template_id"])).ToList();
                Assert.AreEqual(5, rows.Count, $"creature {creature}");
                Assert.IsTrue(rows.All(r => (double)r["chance"] == CreatureLootFootageRows.AmmoChancePerType), $"creature {creature}");
            }
        }

        [TestMethod]
        public void HandWrittenRowsCarryStoreTypesAndOnlyTouchCreatureLoot()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) CreatureLootFootageRows.InsertData(migration);
                else CreatureLootFootageRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                Assert.IsTrue(migration.Operations.All(o => o is InsertDataOperation i ? i.Table == "creature_loot" : o is DeleteDataOperation d && d.Table == "creature_loot"));
                Assert.IsFalse(migration.Operations.OfType<UpdateDataOperation>().Any());
            }

            // Every id is in the batch's block, and every comment fits the varchar(96) column.
            var insert = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            CreatureLootFootageRows.InsertData(insert);
            var rows = Inserted(insert);
            Assert.AreEqual(19, rows.Count);
            Assert.IsTrue(rows.All(r => (uint)r["id"] >= 199400u && (uint)r["id"] <= 199449u));
            Assert.IsTrue(rows.All(r => ((string)r["comment"]).Length <= 96));
            CollectionAssert.AreEqual(new object[] { 1u, 8u, 15u },
                insert.Operations.OfType<DeleteDataOperation>().Select(d => d.KeyValues[0, 0]).ToArray());
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            CreatureLootFootageRows.InsertData(migration);
            var inserted = Inserted(migration);

            using var manifest = Manifest();
            var root = manifest.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == CreatureLootFootageRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            foreach (var row in evidence)
            {
                Assert.AreEqual("creature_loot", row.GetProperty("table").GetString());
                var id = row.GetProperty("key").GetProperty("id").GetUInt32();
                var seeded = inserted.Single(r => (uint)r["id"] == id);
                var fields = row.GetProperty("fields");
                foreach (var field in fields.EnumerateObject())
                {
                    Assert.IsTrue(Same(seeded[field.Name], field.Value.GetProperty("value")), $"creature_loot {id}.{field.Name} differs from the manifest");
                    var tier = field.Value.GetProperty("tier").GetString();
                    // Nothing here is presented as original or observed, and nothing is an analogue.
                    Assert.IsTrue(tier == "measured" || tier == "inferred", $"creature_loot {id}.{field.Name} is {tier}");
                    if (tier == "measured")
                        Assert.IsTrue(field.Value.TryGetProperty("uncertainty", out _), $"creature_loot {id}.{field.Name} has no uncertainty");
                    Assert.IsTrue(field.Value.GetProperty("citations").EnumerateArray()
                        .Any(c => c.GetProperty("source").GetString() == "measurement:creature-loot-ledger"), $"creature_loot {id}.{field.Name} does not cite the ledger");
                }
                Assert.AreEqual(seeded["comment"], row.GetProperty("storage_fields").GetProperty("comment").GetProperty("value").GetString());
                foreach (var column in seeded.Keys.Where(c => c != "id" && c != "comment"))
                    Assert.IsTrue(fields.TryGetProperty(column, out _), $"creature_loot {id}.{column} is seeded without provenance");
            }

            // The Initiate's skull chance is measured; on the stand-in Thrax Soldier it is the same number, inferred.
            string ChanceTier(uint id) => evidence.Single(r => r.GetProperty("key").GetProperty("id").GetUInt32() == id)
                .GetProperty("fields").GetProperty("chance").GetProperty("tier").GetString();
            Assert.AreEqual(("measured", "measured", "inferred", "measured"), (ChanceTier(199400), ChanceTier(199406), ChanceTier(199412), ChanceTier(199418)));

            // The removed emulator rows have no manifest row left and one change entry each.
            Assert.IsFalse(root.GetProperty("rows").EnumerateArray().Any(r => r.GetProperty("table").GetString() == "creature_loot"
                && r.GetProperty("key").GetProperty("id").GetUInt32() is 1 or 8 or 15));
            var removals = root.GetProperty("changes").EnumerateArray()
                .Where(c => c.GetProperty("migration").GetString() == CreatureLootFootageRows.Migration && c.GetProperty("field").GetString() == "existence")
                .Select(c => c.GetProperty("key").GetProperty("id").GetUInt32()).OrderBy(i => i).ToArray();
            CollectionAssert.AreEqual(new uint[] { 1, 8, 15 }, removals);

            // The decisions and gaps the rows and the stand-in rely on are recorded.
            var decisions = root.GetProperty("owner_decisions").EnumerateArray().ToDictionary(d => d.GetProperty("id").GetString());
            foreach (var id in new[] { "OD-110", "OD-111", "OD-112" })
                Assert.AreEqual("approved-by-agent-pending-owner-review", decisions[id].GetProperty("review_status").GetString(), id);
            StringAssert.Contains(decisions["OD-110"].GetProperty("choice").GetString(), "drop nothing");
            var gaps = root.GetProperty("gaps").EnumerateArray().Select(g => g.GetProperty("id").GetString()).ToHashSet();
            using var ledger = Ledger();
            foreach (var gap in ledger.RootElement.GetProperty("gaps").EnumerateArray().Select(g => g.GetString()))
                Assert.IsTrue(gaps.Contains(gap), $"{gap} is not in the manifest");
            Assert.AreEqual("OD-110", ledger.RootElement.GetProperty("fallback").GetProperty("decision").GetString());
            Assert.AreEqual("analogue", ledger.RootElement.GetProperty("fallback").GetProperty("tier").GetString());
        }

        [TestMethod]
        public void RollbackRestoresTheMigratedWorldExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            ContentSchemaMigrationTests.CreatePreviousWorld(context, connection);
            var all = context.Database.GetMigrations().ToList();
            var self = all.Single(id => id.EndsWith("_" + CreatureLootFootageRows.Migration, StringComparison.Ordinal));
            var migrator = context.GetService<IMigrator>();
            migrator.Migrate(all[all.IndexOf(self) - 1]);
            var before = Snapshot(connection);
            Assert.AreEqual(21L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot"));

            migrator.Migrate(self);
            Assert.AreEqual(37L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE id IN (1, 8, 15)"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE item_template_id = 41666 AND chance = 52.38 AND stacksize_min = 1 AND stacksize_max = 1 AND creature_id IN (3, 198507, 198513)"));
            Assert.AreEqual(15L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE item_template_id IN (28, 56, 30, 32, 636) AND chance = 1.9"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE id = 199418 AND creature_id = 44 AND item_template_id = 42296 AND chance = 100 AND stacksize_min = 1 AND stacksize_max = 2"));
            // The emulator's armour and med-pack rows stay.
            Assert.AreEqual(18L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE id BETWEEN 1 AND 21"));

            migrator.Migrate(all[all.IndexOf(self) - 1]);
            Assert.AreEqual(before, Snapshot(connection));
        }

        /// <summary>
        /// The deployed world (read-only): the five ammunition templates are the client's standard-grade weapon ammunition,
        /// each named as ammunition by weaponclass rows, and the junk templates are the live Loot_Junk classes.
        /// </summary>
        [TestMethod]
        public void TheTemplatesAreTheClientsStandardGradeAmmunitionAndLiveJunk()
        {
            using var world = OpenWorld(RepositoryRoot());
            foreach (var (template, _, _, label) in CreatureLootFootageRows.Ammo)
            {
                var klass = Scalar(world, $"SELECT itemClassId FROM itemtemplate_itemclass WHERE itemTemplateId = {template}");
                Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM entityclass WHERE id = {klass} AND class_name LIKE 'Ammo\\_%\\_1\\_Standard\\_Grade' ESCAPE '\\'"), label);
                Assert.IsTrue(Scalar(world, $"SELECT COUNT(*) FROM weaponclass WHERE ammo_class_id = {klass}") > 0, $"{label} is no weapon's ammunition");
            }
            // Exactly five standard-grade ammunition classes are anybody's ammunition, and the rows carry all five.
            Assert.AreEqual(5L, Scalar(world, "SELECT COUNT(DISTINCT e.id) FROM entityclass e JOIN weaponclass w ON w.ammo_class_id = e.id WHERE e.class_name LIKE '%\\_1\\_Standard\\_Grade' ESCAPE '\\'"));
            Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM itemtemplate_itemclass t JOIN entityclass e ON e.id = t.itemClassId WHERE t.itemTemplateId = {CreatureLootFootageRows.ThraxSkull} AND e.id = 20307 AND e.class_name = 'Loot_Junk_Thrax_Skull'"));
            Assert.AreEqual(1L, Scalar(world, $"SELECT COUNT(*) FROM itemtemplate_itemclass t JOIN entityclass e ON e.id = t.itemClassId WHERE t.itemTemplateId = {CreatureLootFootageRows.BoargarEar} AND e.id = 20877 AND e.class_name = 'Loot_Junk_Boargar_Ear'"));
            // The creatures are the world's Initiates, its Thrax stand-in and its Young Forest Boargar.
            Assert.AreEqual(2L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id IN (198507, 198513) AND name_id = 7674"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 44 AND name_id = 7803"));
            Assert.AreEqual(1L, Scalar(world, "SELECT COUNT(*) FROM creature WHERE id = 3 AND class_id = 20757"));
        }

        private static List<Dictionary<string, object>> Inserted(MigrationBuilder migration) =>
            migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => operation.Columns.Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value)))
                .ToList();

        private static string Snapshot(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM creature_loot";
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
            rows.Sort(StringComparer.Ordinal);
            return string.Join(";", rows);
        }

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static bool Same(object actual, JsonElement expected) => expected.ValueKind switch
        {
            JsonValueKind.Number => Convert.ToDecimal(actual) == expected.GetDecimal(),
            JsonValueKind.String => Convert.ToString(actual) == expected.GetString(),
            _ => false
        };

        /// <summary>The repository root, found by walking up from the test binaries to the folder with the navmeshes.</summary>
        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }

        private static SqliteConnection OpenWorld(string root)
        {
            var path = Path.Combine(root, "rasaworld.db");
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                Assert.Inconclusive("rasaworld.db is not in the repository root; this check reads the world database");
            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }

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
    }
}
