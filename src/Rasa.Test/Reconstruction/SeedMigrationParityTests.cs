using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Reconstruction
{
    [TestClass]
    public class SeedMigrationParityTests
    {
        private const string SyntheticSqlite = "Rasa.Test.Reconstruction.ParityFixtures.SqliteWorld";
        private const string SyntheticMySql = "Rasa.Test.Reconstruction.ParityFixtures.MySqlWorld";

        private static SeedMigrationParity.Discovery SyntheticDiscovery()
            => SeedMigrationParity.Discover(typeof(SeedMigrationParityTests).Assembly, SyntheticSqlite, SyntheticMySql, "BootcampSynthetic");

        private static SeedMigrationParity.MigrationPair SyntheticPair(string name)
        {
            var pair = SyntheticDiscovery().Pairs.SingleOrDefault(p => p.Name == name);
            Assert.IsNotNull(pair, $"synthetic pair {name} was not discovered");
            return pair;
        }

        [TestMethod]
        public void BootcampWorldDataMigrationsHaveProviderParity()
        {
            // Boot-camp slices and the Wilderness arrival (WildernessArrivalTrainingDay) share the frozen-rows contract,
            // as does the cross-zone mission-area batch (MissionAreaLinks, 2026-09-17) and the 2026-09-26 batches: the ready
            // missions (EarlyReadyMissions), the Torden missions, the mission reward items, the world defect fixes, the
            // missing mission givers and the official-notes corrections.
            foreach (var prefix in new[] { SeedMigrationParity.BootcampPrefix, SeedMigrationParity.WildernessPrefix, "MissionAreaLinks",
                "EarlyReadyMissions", "TordenConversationMissions", "MissionRewardItems", "WorldDefectsFix", "MissingMissionGivers",
                "OfficialNotesCorrections" })
            {
                var discovery = SeedMigrationParity.Discover(typeof(Rasa.Migrations.SqliteWorld.CorrectRogersNpcPackage).Assembly,
                    SeedMigrationParity.SqliteNamespace, SeedMigrationParity.MySqlNamespace, prefix);
                var errors = discovery.Errors.Concat(discovery.Pairs.SelectMany(SeedMigrationParity.Compare)).ToList();

                Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
                Assert.IsTrue(discovery.Pairs.Count > 0, $"no {prefix} data migration pair was found");
                System.Console.WriteLine($"{prefix} data migration pairs checked: {discovery.Pairs.Count}");
            }
        }

        [TestMethod]
        public void BootcampEquipCorrectionOnlyChangesItsBindingAndRestoresItOnRollback()
        {
            var up = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            var down = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
            Rasa.Migrations.BootcampData.BootcampEquipCrateGearRows.InsertData(up);
            Rasa.Migrations.BootcampData.BootcampEquipCrateGearRows.DeleteData(down);

            foreach (var builder in new[] { up, down })
            {
                Assert.AreEqual(1, builder.Operations.Count);
                var update = (Microsoft.EntityFrameworkCore.Migrations.Operations.UpdateDataOperation)builder.Operations.Single();
                Assert.AreEqual("npc_mission_objective_binding", update.Table);
                CollectionAssert.AreEqual(new[] { "mission_id", "objective_id", "binding_id" }, update.KeyColumns);
                CollectionAssert.AreEqual(new object[] { 1992u, 2u, (byte)0 }, update.KeyValues.Cast<object>().ToArray());
                CollectionAssert.AreEqual(new[] { "equip_match", "item_set_id", "comment" }, update.Columns);
                CollectionAssert.AreEqual(builder == up
                    ? new object[] { (byte)2, 19858u, "1992/2 equip crate gear" }
                    : new object[] { (byte)0, 0u, "1992/2 equip any" }, update.Values.Cast<object>().ToArray());
            }
        }

        [TestMethod]
        public void McAllisterWalkCorrectionIsNarrowAndRestoresItsPreviousRows()
        {
            foreach (var rollback in new[] { false, true })
            {
                var builder = new MigrationBuilder(SeedMigrationParity.SqliteProvider);
                if (rollback) Rasa.Migrations.BootcampData.BootcampMcAllisterWalkRows.DeleteData(builder);
                else Rasa.Migrations.BootcampData.BootcampMcAllisterWalkRows.InsertData(builder);
                Assert.AreEqual(2, builder.Operations.Count);
                var updates = builder.Operations.Cast<Microsoft.EntityFrameworkCore.Migrations.Operations.UpdateDataOperation>().ToArray();
                var speed = updates.Single(update => update.Table == "creature");
                CollectionAssert.AreEqual(new[] { "id" }, speed.KeyColumns);
                Assert.AreEqual(198500u, speed.KeyValues[0, 0]);
                CollectionAssert.AreEqual(new[] { "walk_speed" }, speed.Columns);
                Assert.AreEqual(rollback ? 0d : 2.5d, speed.Values[0, 0]);
                var rule = updates.Single(update => update.Table == "content_rule");
                CollectionAssert.AreEqual(new[] { "id" }, rule.KeyColumns);
                Assert.AreEqual(1985014u, rule.KeyValues[0, 0]);
                CollectionAssert.AreEqual(new[] { "event", "mission_id", "comment" }, rule.Columns);
                CollectionAssert.AreEqual(rollback
                    ? new object[] { (byte)6, 1990u, "1990 turned in -> McAllister walks to the gear" }
                    : new object[] { (byte)2, 1992u, "1992 accepted -> McAllister walks to the gear" }, rule.Values.Cast<object>().ToArray());
            }
        }

        [TestMethod]
        public void DiscoveryReadsTheRealWorldMigrationNamespacesAndRunsUp()
        {
            // Guards against a scan that passes only because it finds nothing: the real
            // CorrectRogersNpcPackage pair is found, invoked, and its raw Sql is reported.
            var discovery = SeedMigrationParity.Discover(typeof(Rasa.Migrations.SqliteWorld.CorrectRogersNpcPackage).Assembly,
                SeedMigrationParity.SqliteNamespace, SeedMigrationParity.MySqlNamespace, "CorrectRogersNpcPackage");

            Assert.AreEqual(0, discovery.Errors.Count, string.Join("\n", discovery.Errors));
            var pair = discovery.Pairs.Single();
            Assert.AreEqual(typeof(Rasa.Migrations.SqliteWorld.CorrectRogersNpcPackage), pair.Sqlite);
            Assert.AreEqual(typeof(Rasa.Migrations.MySqlWorld.CorrectRogersNpcPackage), pair.MySql);

            var errors = SeedMigrationParity.Compare(pair);
            Assert.IsTrue(errors.Any(e => e.Contains("CorrectRogersNpcPackage.Up operation 0: raw Sql is forbidden") && e.Contains("(SQLite)")), string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("CorrectRogersNpcPackage.Up operation 0: raw Sql is forbidden") && e.Contains("(MySQL)")), string.Join("\n", errors));
        }

        [TestMethod]
        public void IdenticalSyntheticPairPasses()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticMatch"));
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));

            var up = SeedMigrationParity.Operations(typeof(ParityFixtures.SqliteWorld.BootcampSyntheticMatch), "Up",
                SeedMigrationParity.SqliteProvider, errors);
            Assert.AreEqual(2, up.Count, "Up must be invoked with the provider builder");
        }

        [TestMethod]
        public void InsertValueMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticValueMismatch"));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains(errors[0], "BootcampSyntheticValueMismatch.Up operation 0: values row 1 column 'radius' SQLite 3 (Double) != MySQL 3.5 (Double)");
        }

        [TestMethod]
        public void InsertColumnMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticColumnMismatch"));
            Assert.IsTrue(errors.Any(e => e.Contains("columns SQLite [id, map_context_id, radius] != MySQL [id, map_context_id, half_height]")), string.Join("\n", errors));
        }

        [TestMethod]
        public void TableMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticTableMismatch"));
            Assert.IsTrue(errors.Any(e => e.Contains("table SQLite 'content_area' != MySQL 'content_placement'")), string.Join("\n", errors));
        }

        [TestMethod]
        public void ValueTypeMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticTypeMismatch"));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains(errors[0], "column 'map_context_id' SQLite 1985 (UInt32) != MySQL 1985 (Int32)");
        }

        [TestMethod]
        public void UpdateDataMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticUpdateMismatch"));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains(errors[0], "BootcampSyntheticUpdateMismatch.Up operation 0: values row 0 column 'instancing' SQLite 1 (Byte) != MySQL 0 (Byte)");
        }

        [TestMethod]
        public void DeleteDataMismatchInDownIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticDeleteMismatch"));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains(errors[0], "BootcampSyntheticDeleteMismatch.Down operation 0: key values row 0 column 'id' SQLite 198600 (UInt32) != MySQL 198601 (UInt32)");
        }

        [TestMethod]
        public void OperationCountMismatchIsDetected()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticOperationCount"));
            Assert.IsTrue(errors.Any(e => e.Contains("BootcampSyntheticOperationCount.Up: SQLite has 1 operations, MySQL has 2")), string.Join("\n", errors));
        }

        [TestMethod]
        public void RawSqlIsRejectedEvenWhenBothProvidersAgree()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticRawSql"));
            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors.All(e => e.Contains("raw Sql is forbidden in boot-camp data migrations")), string.Join("\n", errors));
        }

        [TestMethod]
        public void SchemaOperationIsRejectedInADataMigration()
        {
            var errors = SeedMigrationParity.Compare(SyntheticPair("BootcampSyntheticSchemaOperation"));
            Assert.AreEqual(2, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors.All(e => e.Contains("AddColumnOperation is not a data operation")), string.Join("\n", errors));
        }

        [TestMethod]
        public void UnpairedMigrationIsRejected()
        {
            var discovery = SyntheticDiscovery();
            Assert.AreEqual(1, discovery.Errors.Count, string.Join("\n", discovery.Errors));
            StringAssert.Contains(discovery.Errors[0],
                "BootcampSyntheticUnpaired: Rasa.Test.Reconstruction.ParityFixtures.SqliteWorld.BootcampSyntheticUnpaired has no Rasa.Test.Reconstruction.ParityFixtures.MySqlWorld counterpart");
            Assert.IsFalse(discovery.Pairs.Any(p => p.Name == "BootcampSyntheticUnpaired"));
        }
    }
}

namespace Rasa.Test.Reconstruction.ParityFixtures
{
    /// <summary>Synthetic frozen rows shared by both providers, as boot-camp row classes will be.</summary>
    internal static class SyntheticRows
    {
        public static readonly string[] AreaColumns = { "id", "map_context_id", "radius" };

        public static readonly object[,] Areas =
        {
            { 198600u, 1985u, 2.5 },
            { 198601u, 1985u, 3.0 }
        };

        public static void InsertAreas(MigrationBuilder builder) => builder.InsertData("content_area", AreaColumns, Areas);

        public static void UpdateInstancing(MigrationBuilder builder, byte value)
            => builder.UpdateData("map_info", "map_context_id", 1985u, "instancing", value);

        public static void DeleteAreas(MigrationBuilder builder)
            => builder.DeleteData("content_area", "id", new object[] { 198600u, 198601u });
    }
}

namespace Rasa.Test.Reconstruction.ParityFixtures.SqliteWorld
{
    public class BootcampSyntheticMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SyntheticRows.InsertAreas(migrationBuilder);
            SyntheticRows.UpdateInstancing(migrationBuilder, 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            SyntheticRows.UpdateInstancing(migrationBuilder, 0);
            SyntheticRows.DeleteAreas(migrationBuilder);
        }
    }

    public class BootcampSyntheticValueMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_area", SyntheticRows.AreaColumns, new object[,] { { 198600u, 1985u, 2.5 }, { 198601u, 1985u, 3.0 } });

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticColumnMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticTableMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticTypeMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_area", SyntheticRows.AreaColumns, new object[,] { { 198600u, 1985u, 2.5 } });

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticUpdateMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.UpdateInstancing(migrationBuilder, 1);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.UpdateInstancing(migrationBuilder, 0);
    }

    public class BootcampSyntheticDeleteMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DeleteData("content_area", "id", new object[] { 198600u, 198601u });
    }

    public class BootcampSyntheticOperationCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticRawSql : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("UPDATE `map_info` SET `instancing` = 1 WHERE `map_context_id` = 1985;");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }

    public class BootcampSyntheticSchemaOperation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.AddColumn<byte>("instancing", "map_info", nullable: false, defaultValue: (byte)0);

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }

    public class BootcampSyntheticUnpaired : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }
}

namespace Rasa.Test.Reconstruction.ParityFixtures.MySqlWorld
{
    public class BootcampSyntheticMatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SyntheticRows.InsertAreas(migrationBuilder);
            SyntheticRows.UpdateInstancing(migrationBuilder, 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            SyntheticRows.UpdateInstancing(migrationBuilder, 0);
            SyntheticRows.DeleteAreas(migrationBuilder);
        }
    }

    public class BootcampSyntheticValueMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_area", SyntheticRows.AreaColumns, new object[,] { { 198600u, 1985u, 2.5 }, { 198601u, 1985u, 3.5 } });

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticColumnMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_area", new[] { "id", "map_context_id", "half_height" }, SyntheticRows.Areas);

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticTableMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_placement", SyntheticRows.AreaColumns, SyntheticRows.Areas);

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticTypeMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.InsertData("content_area", SyntheticRows.AreaColumns, new object[,] { { 198600u, 1985, 2.5 } });

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticUpdateMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.UpdateInstancing(migrationBuilder, 0);
        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.UpdateInstancing(migrationBuilder, 0);
    }

    public class BootcampSyntheticDeleteMismatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => SyntheticRows.InsertAreas(migrationBuilder);

        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.DeleteData("content_area", "id", new object[] { 198601u, 198601u });
    }

    public class BootcampSyntheticOperationCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SyntheticRows.InsertAreas(migrationBuilder);
            SyntheticRows.InsertAreas(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder) => SyntheticRows.DeleteAreas(migrationBuilder);
    }

    public class BootcampSyntheticRawSql : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("UPDATE `map_info` SET `instancing` = 1 WHERE `map_context_id` = 1985;");

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }

    public class BootcampSyntheticSchemaOperation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.AddColumn<byte>("instancing", "map_info", nullable: false, defaultValue: (byte)0);

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
