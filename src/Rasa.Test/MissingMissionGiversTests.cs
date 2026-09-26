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
    /// MissingMissionGivers: every seeded value is the one its manifest row records (analogues name their approved
    /// decision), the rollback gives back the world and the client skeleton exactly, and the five missions load as
    /// offerable definitions completed through the receivers' own client packages.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissingMissionGiversTests
    {
        private static readonly uint[] Seeded = { 1741, 1744, 390, 818, 1862 };

        /// <summary>The client skeleton for the five missions, as MissionClientObjectiveSkeleton seeds it.</summary>
        private static readonly (uint Mission, uint Objective, string Text, uint Package)[] Skeleton =
        {
            (1741, 1, "Report to Liaison Standley", 2049),
            (1744, 4, "Report to Receptive Liaison Arizpe", 2025),
            (390, 1, "Deliver Crate to Warrior Mela", 190),
            (818, 1, "Report to Field Sergeant Hanna", 423),
            (1862, 1, "Deliver the Infensus Garrison Directive", 1280)
        };

        /// <summary>The Class V consumables 1862 offers: template, item class and stack size in rasaworld.db.</summary>
        private static readonly (uint Template, uint Class, uint StackSize)[] RewardTemplates =
        {
            (45056, 22539, 100), (118812, 28466, 5000), (45444, 22961, 5000)
        };

        /// <summary>
        /// Columns written with the table's neutral value and no manifest field: action2..8 (no further attack) and the
        /// placement's state, usable, condition, loot, escort and timing columns, which a stationary conversation NPC
        /// does not use; rotation 0 is the unrecorded facing (GAP-MISSING-GIVER-PRESENTATION); npc_package_id 0 means
        /// "the creature's own npc_package row" for the three givers that carry none.
        /// </summary>
        private static readonly HashSet<(string, string)> NeutralColumns = new()
        {
            ("creature", "action2"), ("creature", "action3"), ("creature", "action4"), ("creature", "action5"),
            ("creature", "action6"), ("creature", "action7"), ("creature", "action8"),
            ("content_placement", "npc_package_id"), ("content_placement", "entity_class_id"), ("content_placement", "usable_kind"),
            ("content_placement", "rotation"), ("content_placement", "initial_state"), ("content_placement", "alternate_state"),
            ("content_placement", "alternate_state_condition_id"), ("content_placement", "windup_ms"), ("content_placement", "name_override_id"),
            ("content_placement", "hit_points"), ("content_placement", "restore_ms"), ("content_placement", "fuse_ms"),
            ("content_placement", "loot_item_set_id"), ("content_placement", "respawn_ms"), ("content_placement", "present_condition_id"),
            ("content_placement", "usable_condition_id"), ("content_placement", "escort_mission_id")
        };

        [TestMethod]
        public void HandWrittenSeedCarriesStoreTypesForEveryDataOperation()
        {
            foreach (var up in new[] { true, false })
            {
                var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
                if (up) MissingMissionGiversRows.InsertData(migration);
                else MissingMissionGiversRows.DeleteData(migration);
                foreach (var row in migration.Operations.OfType<InsertDataOperation>())
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<DeleteDataOperation>())
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                foreach (var row in migration.Operations.OfType<UpdateDataOperation>())
                {
                    Assert.AreEqual(row.KeyColumns.Length, row.KeyColumnTypes?.Length ?? 0, row.Table);
                    Assert.AreEqual(row.Columns.Length, row.ColumnTypes?.Length ?? 0, row.Table);
                }
            }
        }

        [TestMethod]
        public void EverySeededRowMatchesItsManifestRow()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            MissingMissionGiversRows.InsertData(migration);
            var inserted = migration.Operations.OfType<InsertDataOperation>()
                .Where(operation => operation.Table != "creature_appearance")
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (operation.Table, Values: operation.Columns
                        .Select((column, columnIndex) => (column, value: operation.Values[index, columnIndex]))
                        .ToDictionary(pair => pair.column, pair => pair.value))))
                .ToList();

            using var document = JsonDocument.Parse(File.ReadAllText(EvidenceLocator.EvidenceFile("bootcamp-d11-reconstruction-manifest.json")));
            var root = document.RootElement;
            var evidence = root.GetProperty("rows").EnumerateArray()
                .Where(row => row.GetProperty("migration").GetString() == MissingMissionGiversRows.Migration).ToList();
            Assert.AreEqual(inserted.Count, evidence.Count, "every inserted row needs exactly one manifest row");

            foreach (var row in evidence)
            {
                var table = row.GetProperty("table").GetString();
                var key = row.GetProperty("key");
                var matches = inserted.Where(item => item.Table == table && key.EnumerateObject().All(field =>
                    item.Values.TryGetValue(field.Name, out var actual) && Same(actual, field.Value))).ToList();
                Assert.AreEqual(1, matches.Count, $"{table} {key} must match exactly one seeded row");
                var fields = row.GetProperty("fields");
                var storage = row.TryGetProperty("storage_fields", out var storageFields) ? storageFields : default;
                foreach (var field in fields.EnumerateObject())
                {
                    Assert.IsTrue(matches[0].Values.TryGetValue(field.Name, out var actual), $"{table} {key}.{field.Name} is not seeded");
                    Assert.IsTrue(Same(actual, field.Value.GetProperty("value")), $"{table} {key}.{field.Name} differs from the manifest");
                    Assert.IsTrue(field.Value.GetProperty("citations").GetArrayLength() > 0, $"{table} {key}.{field.Name} has no citation");
                    if (field.Value.GetProperty("tier").GetString() == "analogue")
                        CollectionAssert.Contains(new[] { "OD-11", "OD-45" }, field.Value.GetProperty("decision").GetString(), $"{table} {key}.{field.Name}");
                }
                foreach (var (column, value) in matches[0].Values)
                {
                    if (key.TryGetProperty(column, out _) || fields.TryGetProperty(column, out _))
                        continue;
                    if (storage.ValueKind == JsonValueKind.Object && storage.TryGetProperty(column, out var stored))
                    {
                        Assert.IsTrue(Same(value, stored.GetProperty("value")), $"{table} {key}.{column} differs from its storage field");
                        continue;
                    }
                    Assert.IsTrue(NeutralColumns.Contains((table, column)), $"{table} {key}.{column} is seeded without provenance");
                    Assert.AreEqual(0m, Convert.ToDecimal(value), $"{table} {key}.{column} must hold its neutral value");
                }
            }

            // Mela's pool row now carries the dated reading the migration moves it to.
            var mela = root.GetProperty("rows").EnumerateArray().Single(row => row.GetProperty("table").GetString() == "spawnpool" &&
                row.GetProperty("key").GetProperty("id").GetUInt32() == MissingMissionGiversRows.Mela).GetProperty("fields");
            var move = MissingMissionGiversRows.MelaMove;
            Assert.AreEqual((move.X, move.Y, move.Z), (mela.GetProperty("pos_x").GetProperty("value").GetDouble(),
                mela.GetProperty("pos_y").GetProperty("value").GetDouble(), mela.GetProperty("pos_z").GetProperty("value").GetDouble()));
            Assert.AreEqual("measured", mela.GetProperty("pos_y").GetProperty("tier").GetString());
            var update = migration.Operations.OfType<UpdateDataOperation>().Single(operation => operation.Table == "spawnpool");
            CollectionAssert.AreEqual(new object[] { move.X, move.Y, move.Z }, update.Values.Cast<object>().ToArray());

            // Langerman's class change and the outfits are recorded as analogue changes under OD-45.
            var changes = root.GetProperty("changes").EnumerateArray()
                .Where(change => change.GetProperty("migration").GetString() == MissingMissionGiversRows.Migration).ToList();
            var classChange = changes.Single(change => change.GetProperty("table").GetString() == "creature");
            Assert.AreEqual((29423, 3846, "OD-45"), (classChange.GetProperty("old").GetInt32(), classChange.GetProperty("new").GetInt32(),
                classChange.GetProperty("decision").GetString()));
            Assert.AreEqual(2, changes.Count(change => change.GetProperty("table").GetString() == "creature_appearance"));

            // The gaps the migration leaves are registered, and GAP-MELA-POSITION is closed.
            var gaps = root.GetProperty("gaps").EnumerateArray().ToDictionary(gap => gap.GetProperty("id").GetString());
            foreach (var id in new[] { "GAP-ALMOS-HEIGHT", "GAP-CONWAY-POSITION", "GAP-DEKAY-POSITION", "GAP-DEKAY-RIFLEMEN", "GAP-TARINA-SINGLE-SOURCE",
                         "GAP-MISSING-NPC-LEVELS", "GAP-LOGOS-CHAIN-START-POST-D11", "GAP-MISSING-GIVER-PRESENTATION", "GAP-MISSING-GIVERS-818-GATE",
                         "GAP-MISSING-GIVERS-REWARDS", "GAP-MISSING-GIVERS-ACCEPT-ITEMS", "GAP-827-BRANCH-ARMS" })
                Assert.AreEqual("open", gaps[id].GetProperty("status").GetString(), id);
            Assert.AreEqual("closed", gaps["GAP-MELA-POSITION"].GetProperty("status").GetString());
        }

        [TestMethod]
        public void TheCreatedGiversCarryTheirBodiesAndOnlyHannaAPlacementPackage()
        {
            var migration = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            MissingMissionGiversRows.InsertData(migration);
            var appearance = migration.Operations.OfType<InsertDataOperation>().Where(operation => operation.Table == "creature_appearance")
                .SelectMany(operation => Enumerable.Range(0, operation.Values.GetLength(0))
                    .Select(index => (Id: Convert.ToUInt32(operation.Values[index, 0]), Slot: Convert.ToUInt32(operation.Values[index, 1]))))
                .ToList();
            Assert.AreEqual(6, appearance.Count(row => row.Id == MissingMissionGiversRows.Langerman));
            Assert.AreEqual(6, appearance.Count(row => row.Id == MissingMissionGiversRows.Simpson));
            Assert.AreEqual((MissingMissionGiversRows.Tarina, 13u), appearance.Single(row => row.Id == MissingMissionGiversRows.Tarina));
            Assert.AreEqual(5, appearance.Count(row => row.Id == MissingMissionGiversRows.Hanna));
            Assert.AreEqual(6, appearance.Count(row => row.Id == MissingMissionGiversRows.Dekay));
            Assert.AreEqual(appearance.Count, appearance.Distinct().Count(), "one row per creature and slot");

            foreach (var npc in MissingMissionGiversRows.Npcs)
            {
                Assert.AreEqual(npc.Id == MissingMissionGiversRows.Hanna ? 423u : 0u, npc.Package, npc.Comment);
                Assert.IsTrue(npc.Comment.Length <= 50 && npc.PlacementComment.Length <= 50, npc.Comment);
            }
            Assert.IsTrue(MissingMissionGiversRows.Missions.All(mission => mission.Comment.Length <= 50));
            Assert.IsTrue(MissingMissionGiversRows.Packages.All(package => package.Comment.Length <= 50));
            // No objective of the five completes through its giver's package: nothing needs MissionRedirectConversations.
            Assert.IsFalse(MissionRedirectConversations.All.Any(redirect => Seeded.Contains(redirect.MissionId)));
        }

        [TestMethod]
        public void RollbackRestoresTheWorldAndTheClientSkeletonExactly()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = Context(connection);
            SeedWorld(context);
            var before = Snapshot(connection);

            Apply(context, Up());
            Assert.AreNotEqual(before, Snapshot(connection));
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE ordinal = 1 AND is_required = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 133 AND class_id = 3846"));

            Apply(context, Down());
            Assert.AreEqual(before, Snapshot(connection));
        }

        [TestMethod]
        public void TheFiveMissionsLoadAsOfferableDefinitions()
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
                    SeedWorld(context);
                    Apply(context, Up());
                }

                var manager = new MissionManager(new Factory(connection));
                manager.LoadMissions();

                foreach (var (mission, objective, _, package) in Skeleton)
                {
                    var definition = manager.LoadedMissions[mission];
                    CollectionAssert.AreEqual(Array.Empty<string>(), definition.DefinitionGaps(), $"mission {mission}");
                    Assert.IsTrue(definition.IsDispensable, $"mission {mission}");
                    Assert.IsTrue(definition.HasObjectiveConversation(objective, package, 1), $"mission {mission}");
                    Assert.AreEqual(0, definition.Prerequisites.Count, $"mission {mission}");
                }

                Assert.AreEqual((133u, 134u), (manager.LoadedMissions[1741].MissionGiver, manager.LoadedMissions[1741].MissionReciver));
                Assert.AreEqual((199004u, 199085u), (manager.LoadedMissions[1744].MissionGiver, manager.LoadedMissions[1744].MissionReciver));
                Assert.AreEqual((199951u, 510117u), (manager.LoadedMissions[390].MissionGiver, manager.LoadedMissions[390].MissionReciver));
                Assert.AreEqual((510066u, 199952u), (manager.LoadedMissions[818].MissionGiver, manager.LoadedMissions[818].MissionReciver));
                Assert.AreEqual((199953u, 510090u), (manager.LoadedMissions[1862].MissionGiver, manager.LoadedMissions[1862].MissionReciver));

                // 1741 pays experience only (TaRapedia's credits read "None"); the pre-1.4 missions pay no item.
                Assert.AreEqual((0L, 4000L), ((long)manager.LoadedMissions[1741].RewardCredits, (long)manager.LoadedMissions[1741].RewardExperience));
                Assert.AreEqual((800L, 8000L), ((long)manager.LoadedMissions[1744].RewardCredits, (long)manager.LoadedMissions[1744].RewardExperience));
                Assert.AreEqual((1950L, 13000L), ((long)manager.LoadedMissions[390].RewardCredits, (long)manager.LoadedMissions[390].RewardExperience));
                Assert.AreEqual((2800L, 15500L), ((long)manager.LoadedMissions[818].RewardCredits, (long)manager.LoadedMissions[818].RewardExperience));
                foreach (var id in new uint[] { 1741, 1744, 390, 818 })
                    Assert.AreEqual(0, manager.LoadedMissions[id].OfferedSelectableRewards.Count + manager.LoadedMissions[id].OfferedFixedItems.Count, $"mission {id} pays no item");

                // 1862: experience, credits and a choice of three Class V stacks in the wiki's order.
                var directive = manager.LoadedMissions[1862];
                Assert.AreEqual((2500L, 12500L), ((long)directive.RewardCredits, (long)directive.RewardExperience));
                CollectionAssert.AreEqual(new[] { (45056u, 2u), (118812u, 2u), (45444u, 3u) },
                    directive.MissionConstantData.RewardInfo.SelectableReward.Select(item => (item.ItemTemplateId, item.Quantity)).ToArray());
                Assert.AreEqual(0, directive.OfferedFixedItems.Count);
                Assert.AreEqual((5u, 10u, 10u, 35u, 35u), (manager.LoadedMissions[1741].MissionConstantData.Level, manager.LoadedMissions[1744].MissionConstantData.Level,
                    manager.LoadedMissions[390].MissionConstantData.Level, manager.LoadedMissions[818].MissionConstantData.Level, directive.MissionConstantData.Level));
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
            MissingMissionGiversRows.InsertData(builder);
            return builder.Operations;
        }

        private static IReadOnlyList<MigrationOperation> Down()
        {
            var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite");
            MissingMissionGiversRows.DeleteData(builder);
            return builder.Operations;
        }

        /// <summary>
        /// The rows the migration finds: the client skeleton of the five missions, world-seed Langerman on the Redshirt
        /// class, upstream's Mela pool at the preloader's single-precision marker position, and unrelated rows (Standley's
        /// creature, another creature's outfit, a seeded mission) the migration must not touch.
        /// </summary>
        private static void SeedWorld(SqliteWorldContext context)
        {
            context.Database.EnsureCreated();
            foreach (var (mission, objective, text, package) in Skeleton)
            {
                context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_objective (mission_id, objective_id, comment, is_required, ordinal, revealed_on_accept) VALUES ({0}, {1}, {2}, NULL, NULL, NULL)",
                    mission, objective, text);
                context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_objective_conversation (mission_id, objective_id, npc_package_id, player_flag_id, convo_type) VALUES ({0}, {1}, {2}, 1, 1)",
                    mission, objective, package);
            }
            context.Database.ExecuteSqlRaw("INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) VALUES " +
                "(133, 'Receptive Liason Langermon', 29423, 1, 10, 1000, 10010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), " +
                "(134, 'Receptive Liason Standley', 3846, 1, 10, 1000, 10011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            context.Database.ExecuteSqlRaw("INSERT INTO creature_appearance (id, slot_id, Class_id, color) VALUES (134, 2, 4021, 4294934528)");
            context.Database.ExecuteSqlRaw("INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count) VALUES " +
                "(510117, 0, 0, 20, 282.70001220703125, 170.6199951171875, 1071.800048828125, 0, 1148, 510117, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES " +
                "(1743, 199003, 199004, 10, 1, 10000001, 0, 0, 'Report to Liaison Noonan (Divide)')");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission_reward (id, type, credits, item_template_id, quantity) VALUES (1743, 3, 6500, 0, 0)");
        }

        private static string Snapshot(SqliteConnection connection)
        {
            var parts = new List<string>();
            foreach (var table in new[] { "creature", "creature_appearance", "content_placement", "npc_package", "spawnpool", "npc_mission",
                         "npc_mission_objective", "npc_mission_objective_conversation", "npc_mission_reward", "npc_mission_prerequisite" })
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
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

        /// <summary>Registers the three reward templates with their rasaworld.db class and stack size; returns the undo.</summary>
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
