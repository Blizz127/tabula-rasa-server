using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Rasa.Data;
using Rasa.Managers;
using Rasa.Migrations.WildernessData;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Services.DbContext;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Reconstruction;

namespace Rasa.Test
{
    /// <summary>
    /// TordenConversationMissions: every seeded value is the one its manifest row records, the rollback gives back the
    /// client skeleton exactly, and the nine missions load as offerable definitions from the rows the migration writes
    /// onto the client's own objective and conversation skeleton.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class TordenConversationMissionsTests
    {
        private static readonly uint[] Seeded = { 1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330 };

        /// <summary>The client skeleton for the nine missions, as MissionClientObjectiveSkeleton seeds it.</summary>
        private static readonly (uint Mission, uint Objective, string Text, uint Package)[] Skeleton =
        {
            (1745, 5, "Report to Receptive Liaison Repp", 2026),
            (526, 1, "Deliver aid package to Coordinator Maila", 381),
            (526, 2, "Deliver aid package to Salvage Master Orto", 380),
            (648, 1, "Speak to Colonel Franks", 521),
            (802, 1, "Find Field Commander Foletto", 752),
            (1014, 1, "Report to the Fort Defiance Warehouse.", 959),
            (1014, 2, "Report to CID Headquarters.", 962),
            (1064, 1, "Deliver Encrypted Data to Colonel Franks", 521),
            (1070, 1, "Speak to Sergeant Obahmi", 751),
            (1326, 1, "Report to Snake", 331),
            (1330, 1, "Speak to Amee Corman", 329)
        };

        /// <summary>The Class VII consumables 1326/1330 offer: template, item class and stack size in rasaworld.db.</summary>
        private static readonly (uint Template, uint Class, uint StackSize)[] RewardTemplates =
        {
            (45062, 22545, 100), (118907, 28498, 5000), (111048, 26298, 5000), (111038, 26288, 5000),
            (118898, 28489, 5000), (45446, 22963, 5000)
        };

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) TordenConversationMissionsRows.InsertData(migration);
                else TordenConversationMissionsRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            TordenConversationMissionsRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var evidence = document.RootElement.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == TordenConversationMissionsRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            // Storage-only columns: the row comment of npc_mission and npc_mission_prerequisite.
            var storage = new HashSet<(string, string)> { ("npc_mission", "comment"), ("npc_mission_prerequisite", "comment") };
            foreach (var row in evidence)
            {
                var table = row.GetProperty("table").GetString();
                var key = row.GetProperty("key");
                var matches = inserted.Where(item => item.Table == table && key.EnumerateObject().All(field =>
                    item.Values.TryGetValue(field.Name, out var actual) && Same(actual, field.Value))).ToList();
                Assert.AreEqual(1, matches.Count, $"{table} {key} must match exactly one seeded row");
                var fields = row.GetProperty("fields");
                foreach (var field in fields.EnumerateObject())
                {
                    Assert.IsTrue(matches[0].Values.TryGetValue(field.Name, out var actual), $"{table} {key}.{field.Name} is not seeded");
                    Assert.IsTrue(Same(actual, field.Value.GetProperty("value")), $"{table} {key}.{field.Name} differs from the manifest");
                    Assert.IsTrue(field.Value.GetProperty("citations").GetArrayLength() > 0, $"{table} {key}.{field.Name} has no citation");
                }
                foreach (var column in matches[0].Values.Keys)
                    Assert.IsTrue(key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _) || storage.Contains((table, column)),
                        $"{table} {key}.{column} is seeded without provenance");
            }

            // The Pools level is the one analogue, and it names its approved decision.
            foreach (var mission in new[] { 1326, 1330 })
            {
                var level = evidence.Single(row => row.GetProperty("table").GetString() == "npc_mission" &&
                    row.GetProperty("key").GetProperty("id").GetInt32() == mission).GetProperty("fields").GetProperty("level");
                Assert.AreEqual("analogue", level.GetProperty("tier").GetString());
                Assert.AreEqual("OD-59", level.GetProperty("decision").GetString());
            }
        }

        [TestMethod]
        public void RollbackRestoresTheClientSkeletonExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            SeedSkeleton(context);
            var before = Snapshot(connection);

            Apply(context, Up());
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE ordinal IS NOT NULL AND is_required = 1"));

            Apply(context, Down());
            Assert.AreEqual(before, Snapshot(connection));
        }

        [TestMethod]
        public void TheNineMissionsLoadAsOfferableDefinitions()
        {
            var oldLogger = Logger.Config;
            if (oldLogger == null) Logger.UpdateConfig(new Logger.LoggerConfig { LogToFile = false });
            var restore = RegisterRewardTemplates();
            try
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using (var context = Context(connection))
                {
                    SeedSkeleton(context);
                    Apply(context, Up());
                }

                var manager = new MissionManager(new Factory(connection));
                manager.LoadMissions();

                foreach (var id in Seeded)
                {
                    var mission = manager.LoadedMissions[id];
                    CollectionAssert.AreEqual(Array.Empty<string>(), mission.DefinitionGaps(), $"mission {id}");
                    Assert.IsTrue(mission.IsDispensable, $"mission {id}");
                }

                // 1014: Provost (2) on acceptance, then Norton (1).
                var blueFlu = manager.LoadedMissions[1014];
                CollectionAssert.AreEqual(new uint[] { 2, 1 }, blueFlu.ObjectivesInOrder.Select(o => o.ObjectiveId).ToArray());
                CollectionAssert.AreEqual(new uint[] { 1 }, blueFlu.Transitions[2]);
                Assert.AreEqual(2u, blueFlu.ObjectivesList.Single().ObjectiveId);
                Assert.AreEqual(0, blueFlu.Rewards.Count);

                // 526: both deliveries on acceptance, turned in at Epp.
                var aid = manager.LoadedMissions[526];
                Assert.AreEqual(2, aid.ObjectivesList.Count);
                Assert.AreEqual((510068u, 510068u), (aid.MissionGiver, aid.MissionReciver));
                Assert.AreEqual((4350L, 33000L), ((long)aid.RewardCredits, (long)aid.RewardExperience));

                // 1326 and 1330: experience, credits and a choice of four consumable stacks in the wiki's order.
                var spoils = manager.LoadedMissions[1326];
                Assert.AreEqual((3600L, 32500L), ((long)spoils.RewardCredits, (long)spoils.RewardExperience));
                CollectionAssert.AreEqual(new[] { (45062u, 2u), (118907u, 2u), (111048u, 4u), (111038u, 4u) },
                    spoils.MissionConstantData.RewardInfo.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
                var retread = manager.LoadedMissions[1330];
                CollectionAssert.AreEqual(new[] { (45062u, 2u), (118898u, 2u), (45446u, 4u), (111048u, 4u) },
                    retread.MissionConstantData.RewardInfo.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
                Assert.AreEqual(0, retread.OfferedFixedItems.Count);
                Assert.AreEqual((20u, 20u), (spoils.MissionConstantData.Level, retread.MissionConstantData.Level));
            }
            finally
            {
                restore();
                if (oldLogger == null) typeof(Logger).GetProperty(nameof(Logger.Config)).SetValue(null, null);
            }
        }

        private static IReadOnlyList<MigrationOperation> Up()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            TordenConversationMissionsRows.InsertData(builder);
            return builder.Operations;
        }

        private static IReadOnlyList<MigrationOperation> Down()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            TordenConversationMissionsRows.DeleteData(builder);
            return builder.Operations;
        }

        private static void SeedSkeleton(SqliteWorldContext context)
        {
            context.Database.EnsureCreated();
            foreach (var (mission, objective, text, package) in Skeleton)
            {
                context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_objective (mission_id, objective_id, comment, is_required, ordinal, revealed_on_accept) VALUES ({0}, {1}, {2}, NULL, NULL, NULL)",
                    mission, objective, text);
                context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_objective_conversation (mission_id, objective_id, npc_package_id, player_flag_id, convo_type) VALUES ({0}, {1}, {2}, 1, 1)",
                    mission, objective, package);
            }
            // The two seeded missions the gates name, and an unrelated mission's rows the rollback must not touch.
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES " +
                "(887, 199209, 199200, 20, 1, 10000001, 0, 0, 'The Defiant Ones (Plateau)'), (1063, 199501, 199510, 35, 1, 10000001, 0, 0, 'Incriminating Evidence (Plains)')");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_reward (id, type, credits, item_template_id, quantity) VALUES (1063, 3, 25000, 0, 0)");
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "npc_mission", "npc_mission_objective", "npc_mission_objective_conversation", "npc_mission_objective_transition",
                         "npc_mission_reward", "npc_mission_prerequisite" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
                rows.Sort(StringComparer.Ordinal);
                parts.Add(table + ":" + string.Join(";", rows));
            }
            return string.Join("\n", parts);
        }

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }

        private static void Apply(SqliteWorldContext context, IReadOnlyList<MigrationOperation> operations)
        {
            var generator = context.GetService<IMigrationsSqlGenerator>();
            foreach (var command in generator.Generate(operations, context.Model))
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        /// <summary>Registers the six reward templates with their rasaworld.db class and stack size; returns the undo.</summary>
        private static Action RegisterRewardTemplates()
        {
            var oldClasses = new Dictionary<EntityClasses, EntityClass>();
            var oldTemplates = new Dictionary<uint, EntityClasses?>();
            foreach (var (templateId, classNumber, stackSize) in RewardTemplates)
            {
                var classId = (EntityClasses)classNumber;
                oldClasses[classId] = EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var old) ? old : null;
                oldTemplates[templateId] = ItemManager.Instance.ItemTemplateItemClass.TryGetValue(templateId, out var previous) ? previous : (EntityClasses?)null;
                var itemClass = new EntityClass(classNumber, "reward fixture", 0, 0, new List<AugmentationType>(), false)
                    { ItemClassInfo = new ItemClassInfo(new ItemClassEntry { MaxHitPoints = 1, StackSize = stackSize }) };
                var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = templateId, ItemClass = classNumber })
                    { InventoryCategory = InventoryCategory.Consumable, QualityId = 2 };
                itemClass.ItemTemplates.Add(templateId, template);
                EntityClassManager.Instance.LoadedEntityClasses[classId] = itemClass;
                ItemManager.Instance.ItemTemplateItemClass[templateId] = classId;
            }

            return () =>
            {
                foreach (var pair in oldClasses)
                    if (pair.Value == null) EntityClassManager.Instance.LoadedEntityClasses.Remove(pair.Key);
                    else EntityClassManager.Instance.LoadedEntityClasses[pair.Key] = pair.Value;
                foreach (var pair in oldTemplates)
                    if (pair.Value.HasValue) ItemManager.Instance.ItemTemplateItemClass[pair.Key] = pair.Value.Value;
                    else ItemManager.Instance.ItemTemplateItemClass.Remove(pair.Key);
            };
        }

        private static bool Same(object actual, JsonElement expected) => expected.ValueKind switch
        {
            JsonValueKind.Number => Convert.ToDecimal(actual) == expected.GetDecimal(),
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.String => Convert.ToString(actual) == expected.GetString(),
            _ => false
        };

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

        public class WorldProxy : DispatchProxy
        {
            public SqliteWorldContext Context;
            protected override object Invoke(MethodInfo method, object[] args)
            {
                switch (method.Name)
                {
                    case "get_NpcMissions": return new NpcMissionRepository(Context);
                    case "get_NpcMissionObjectives": return new NpcMissionObjectiveRepository(Context);
                    case "get_NpcMissionRewards": return new NpcMissionRewardRepository(Context);
                    case "Dispose": Context.Dispose(); return null;
                    default: throw new NotSupportedException(method.Name);
                }
            }
        }

        private sealed class Factory : IGameUnitOfWorkFactory
        {
            private readonly SqliteConnection _connection;
            public Factory(SqliteConnection connection) => _connection = connection;
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld()
            {
                var unit = DispatchProxy.Create<IWorldUnitOfWork, WorldProxy>();
                ((WorldProxy)(object)unit).Context = Context(_connection);
                return unit;
            }
        }
    }
}
