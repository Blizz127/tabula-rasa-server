using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Configuration;
using Rasa.Configuration.ContextSetup;
using Rasa.Context.Char;
using Rasa.Context.World;
using Rasa.Services.DbContext;
using Rasa.Structures.Char;

namespace Rasa.Test
{
    /// <summary>
    /// The content layer is additive: existing world and character rows survive the upgrade and the
    /// rollback, and the new tables start empty.
    /// </summary>
    [TestClass]
    public class ContentSchemaMigrationTests
    {
        private const string PreviousWorldMigration = "20260913030423_MissionObjectiveDefinitions";
        private const string PreviousCharMigration = "20260913025737_MissionObjectiveProgress";
        private const string ContentLayerMigration = "20260913180618_MissionContentLayer";

        public static readonly string[] WorldTables =
        {
            "content_area", "content_condition", "content_item_set", "content_location", "content_map_setting", "content_placement",
            "content_rule", "content_rule_action", "npc_mission_objective_binding", "npc_mission_objective_counter",
            "npc_mission_objective_indicator", "npc_mission_objective_timer", "npc_mission_prerequisite"
        };

        private sealed class TestConfiguration : IDbContextConfigurationService
        {
            private readonly SqliteConnection _connection;
            public TestConfiguration(SqliteConnection connection) => _connection = connection;
            public void Configure(DbContextOptionsBuilder builder, DatabaseConnectionConfiguration configuration)
                => builder.UseSqlite(_connection);
        }

        private static SqliteWorldContext World(SqliteConnection connection)
            => new SqliteWorldContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static SqliteCharContext Char(SqliteConnection connection)
            => new SqliteCharContext(Options.Create(new DatabaseConfiguration()), new TestConfiguration(connection), new SqliteDbContextPropertyModifier());

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar();
        }

        private static bool TableExists(SqliteConnection connection, string table)
            => Scalar(connection, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{table}'") == 1;

        /// <summary>
        /// The world schema as it stood before MissionContentLayer, with rows standing in for the seed.
        /// Replaying the full seed-data migration is too slow for a unit test, so the schema is created
        /// from the model without the content tables (the model snapshot diff adds only those tables)
        /// and every earlier migration is recorded as applied.
        /// </summary>
        public static void CreatePreviousWorld(SqliteWorldContext context, SqliteConnection connection)
        {
            context.Database.EnsureCreated();
            foreach (var table in WorldTables)
                context.Database.ExecuteSqlRaw($"DROP TABLE \"{table}\"");
            context.Database.ExecuteSqlRaw(
                "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            foreach (var migration in context.Database.GetMigrations().TakeWhile(id => id != ContentLayerMigration))
                context.Database.ExecuteSqlRaw("INSERT INTO \"__EFMigrationsHistory\" VALUES ({0}, '5.0.1')", migration);
            Assert.AreEqual(ContentLayerMigration, context.Database.GetPendingMigrations().Single());
            Assert.AreEqual(PreviousWorldMigration, context.Database.GetAppliedMigrations().Last());
        }

        private static (long, long, long) WorldRows(SqliteConnection connection)
            => (Scalar(connection, "SELECT COUNT(*) FROM npc_mission"), Scalar(connection, "SELECT COUNT(*) FROM map_info"),
                Scalar(connection, "SELECT COUNT(*) FROM logos"));

        [TestMethod]
        public void WorldUpgradeAddsEmptyContentTablesAndRollbackKeepsExistingRows()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            CreatePreviousWorld(context, connection);
            context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4), (1220, 'adv_foreas_concordia_wilderness', 1556, 0)");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES (321, 101, 100, 5, 1, 1, 0, 0, 'Assemble With Lieutenant Perkins')");
            context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");
            var before = WorldRows(connection);

            context.Database.Migrate();
            Assert.AreEqual(0, context.Database.GetPendingMigrations().Count());
            foreach (var table in WorldTables)
            {
                Assert.IsTrue(TableExists(connection, table), table);
                Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM {table}"), table);
            }
            Assert.AreEqual(before, WorldRows(connection));

            // Content keys are explicit: an insert keeps its id (no generated keys to renumber a 0 or a reserved id).
            context.Database.ExecuteSqlRaw("INSERT INTO content_map_setting (map_context_id, instancing, comment) VALUES (1985, 0, '')");
            context.Database.ExecuteSqlRaw("INSERT INTO content_rule (id, map_context_id, event, mission_id, objective_id, area_id, placement_id, state_id, condition_id, comment) VALUES (1985000, 1985, 1, 0, 0, 0, 0, 0, 0, '')");
            Assert.AreEqual(1985000L, Scalar(connection, "SELECT id FROM content_rule"));
            context.Database.ExecuteSqlRaw("DELETE FROM content_rule");
            context.Database.ExecuteSqlRaw("DELETE FROM content_map_setting");

            context.GetService<IMigrator>().Migrate(PreviousWorldMigration);
            Assert.IsFalse(WorldTables.Any(table => TableExists(connection, table)));
            Assert.AreEqual(before, WorldRows(connection));
        }

        [TestMethod]
        public void CharUpgradeAddsTimerColumnsWithDefaultsAndRollbackKeepsMissions()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using (var context = Char(connection))
            {
                context.GetService<IMigrator>().Migrate(PreviousCharMigration);
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission (character_id, mission_id, mission_state, change_time) VALUES (101, 429, 0, 5)");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO character_mission_objective (character_id, mission_id, objective_id, status) VALUES (101, 429, 1, 1)");
                Assert.IsFalse(TableExists(connection, "character_content_fact"));

                context.Database.Migrate();
                Assert.AreEqual(0, context.Database.GetPendingMigrations().Count());
                Assert.IsTrue(TableExists(connection, "character_content_fact"));
                Assert.IsTrue(TableExists(connection, "character_mission_objective_counter"));
            }

            using (var reloaded = Char(connection))
            {
                var objective = reloaded.CharacterMissionObjectiveEntries.Single();
                Assert.AreEqual((101u, 429u, 1u, 1u), (objective.CharacterId, objective.MissionId, objective.ObjectiveId, objective.Status));
                Assert.IsNull(objective.TimerRemainingMs);
                Assert.IsNull(objective.TimerAnchorMs);
                Assert.IsFalse(objective.TimerDisarmed);
                reloaded.CharacterMissionObjectiveCounterEntries.Add(new CharacterMissionObjectiveCounterEntry { CharacterId = 101, MissionId = 429, ObjectiveId = 1, CounterId = 0, Value = 1 });
                reloaded.SaveChanges();

                reloaded.GetService<IMigrator>().Migrate(PreviousCharMigration);
            }

            Assert.IsFalse(TableExists(connection, "character_mission_objective_counter"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM character_mission WHERE character_id = 101 AND mission_id = 429 AND change_time = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM character_mission_objective WHERE character_id = 101 AND mission_id = 429 AND status = 1"));
        }
    }
}
