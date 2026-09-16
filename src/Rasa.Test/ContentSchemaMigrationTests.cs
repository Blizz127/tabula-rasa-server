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
        private const string PreviousWorldMigration = "20260913180000_Add_map_link";
        private const string PreviousCharMigration = "20260913025737_MissionObjectiveProgress";
        private const string ContentLayerMigration = "20260913180618_MissionContentLayer";
        private const string BootcampS1Migration = "20260913201420_BootcampS1Initiation";
        private const string ObjectiveColumnsMigration = "20260913234728_MissionObjectiveClientColumns";
        private const string ObjectiveSkeletonMigration = "20260913235900_MissionClientObjectiveSkeleton";
        private const string BootcampS2Migration = "20260914003000_BootcampS2GearingUp";
        private const string BootcampFixNpcAppearanceMigration = "20260914050000_BootcampFixNpcAppearance";
        private const string KraftwerksMigration = "20260914120000_Add_kraftwerks";
        private const string BootcampS3Migration = "20260914130000_BootcampS3PerCharacterInstancing";
        private const string BootcampS4Migration = "20260914140000_BootcampS4CaptureTheFlag";
        private const string BootcampS5Migration = "20260914150000_BootcampS5Reinforcements";
        private const string BootcampS6Migration = "20260914160000_BootcampS6ExitToAliaDas";
        private const string BootcampFixRogersTurnInMigration = "20260914170000_BootcampFixRogersTurnIn";
        private const string WildernessArrivalTrainingDayMigration = "20260914180000_WildernessArrivalTrainingDay";
        private const string WildernessClassGearMigration = "20260914190000_WildernessClassGear";
        /// <summary>PR #91's map-region table (merged from upstream 2026-09-15).</summary>
        private const string AddMapRegionMigration = "20260915120000_Add_map_region";
        private const string BootcampAreaVerticalExtentMigration = "20260915140000_BootcampAreaVerticalExtent";
        private const string WildernessHubReceptiveReceptionMigration = "20260915160000_WildernessHubReceptiveReception";
        private const string BootcampObjectiveIndicatorsMigration = "20260915180000_BootcampObjectiveIndicators";
        private const string BootcampScriptedMovesMigration = "20260915200000_BootcampScriptedMoves";
        private const string ContentRuleActionDamageMigration = "20260915220000_ContentRuleActionDamage";
        private const string BootcampDetonationDamageMigration = "20260915230000_BootcampDetonationDamage";
        private const string BootcampObjectiveAreaRadiusMigration = "20260916000000_BootcampObjectiveAreaRadius";
        private const string BootcampCaveInTriggerRadiusMigration = "20260916020000_BootcampCaveInTriggerRadius";
        private const string BootcampObjectiveAreaHeightMigration = "20260916040000_BootcampObjectiveAreaHeight";
        private const string BootcampRemainingTriggerHeightMigration = "20260916060000_BootcampRemainingTriggerHeight";
        private const string BootcampPlacementGroundSnapMigration = "20260916080000_BootcampPlacementGroundSnap";
        private const string BootcampPlatformTopCorrectionMigration = "20260916100000_BootcampPlatformTopCorrection";
        private const string BootcampEscortDestinationGroundMigration = "20260916120000_BootcampEscortDestinationGround";
        private const string WildernessHubConscientiousObjectorMigration = "20260916140000_WildernessHubConscientiousObjector";
        private const string WildernessHubConscientiousObjectorPathMigration = "20260916160000_WildernessHubConscientiousObjectorPath";
        private const string WildernessHubConversationChainMigration = "20260916180000_WildernessHubConversationChain";
        private const string WildernessHubConversationChainRewardsMigration = "20260916200000_WildernessHubConversationChainRewards";
        private const string WildernessHubKillObjectiveMigration = "20260916220000_WildernessHubKillObjective";
        private const string DivideConversationNpcMigration = "20260917000000_DivideConversationNpc";
        private const string PalisadesConversationNpcMigration = "20260917020000_PalisadesConversationNpc";
        private const string PlateauConversationNpcMigration = "20260917040000_PlateauConversationNpc";
        private const string MarshesConversationNpcMigration = "20260917060000_MarshesConversationNpc";
        private const string MiresConversationNpcMigration = "20260917080000_MiresConversationNpc";

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
        /// and every earlier migration is recorded as applied. The model already carries the
        /// MissionObjectiveClientColumns schema, so the two objective tables are recreated in their
        /// pre-20260913234728 shape for the migration to apply onto.
        /// </summary>
        public static void CreatePreviousWorld(SqliteWorldContext context, SqliteConnection connection)
        {
            context.Database.EnsureCreated();
            foreach (var table in WorldTables)
                context.Database.ExecuteSqlRaw($"DROP TABLE \"{table}\"");
            // Add_kraftwerks (20260914120000) and Add_map_region (20260915120000) come after the content layer too.
            context.Database.ExecuteSqlRaw("DROP TABLE \"kraftwerks\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"map_region\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"npc_mission_objective\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"npc_mission_objective_conversation\"");
            context.Database.ExecuteSqlRaw(
                "CREATE TABLE \"npc_mission_objective\" (\"mission_id\" INTEGER NOT NULL, \"objective_id\" INTEGER NOT NULL, " +
                "\"ordinal\" INTEGER NOT NULL, \"is_required\" INTEGER NOT NULL, \"revealed_on_accept\" INTEGER NOT NULL, \"comment\" TEXT NOT NULL, " +
                "CONSTRAINT \"PK_npc_mission_objective\" PRIMARY KEY (\"mission_id\", \"objective_id\"))");
            context.Database.ExecuteSqlRaw(
                "CREATE TABLE \"npc_mission_objective_conversation\" (\"mission_id\" INTEGER NOT NULL, \"objective_id\" INTEGER NOT NULL, " +
                "\"npc_package_id\" INTEGER NOT NULL, \"player_flag_id\" INTEGER NOT NULL, " +
                "CONSTRAINT \"PK_npc_mission_objective_conversation\" PRIMARY KEY (\"mission_id\", \"objective_id\", \"npc_package_id\", \"player_flag_id\"))");
            context.Database.ExecuteSqlRaw(
                "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            foreach (var migration in context.Database.GetMigrations().TakeWhile(id => id != ContentLayerMigration))
                context.Database.ExecuteSqlRaw("INSERT INTO \"__EFMigrationsHistory\" VALUES ({0}, '5.0.1')", migration);
            CollectionAssert.AreEqual(
                new[] { ContentLayerMigration, BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration },
                context.Database.GetPendingMigrations().ToArray());
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
            context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4), (1220, 'adv_foreas_concordia_wilderness', 1556, 0), (1148, 'adv_foreas_concordia_divide', 1584, 10), (1244, 'adv_foreas_concordia_palisades', 1584, 10), (1497, 'adv_foreas_valverde_plateau', 1584, 10), (1304, 'adv_foreas_valverde_pools', 1584, 10), (1454, 'adv_foreas_valverde_marshes', 1584, 10), (1759, 'adv_arieki_torden_mires', 1584, 10)");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES (321, 101, 100, 5, 1, 1, 0, 0, 'Assemble With Lieutenant Perkins')");
            context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");
            var before = WorldRows(connection);

            context.Database.GetService<IMigrator>().Migrate(ContentLayerMigration);
            CollectionAssert.AreEqual(
                new[] { BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration },
                context.Database.GetPendingMigrations().ToArray());
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
        public void BootcampS1SeedsTheInitiationRowsAndRollsBackToEmptyContentTables()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var context = World(connection);
            CreatePreviousWorld(context, connection);
            context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4)");
            context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");

            context.Database.Migrate();
            Assert.AreEqual(0, context.Database.GetPendingMigrations().Count());

            // The initiation content: the start location, the NPC, the mission and its content rows.
            Assert.AreEqual(19851L, Scalar(connection, "SELECT id FROM content_location WHERE purpose = 1"));
            Assert.AreEqual(10566L, Scalar(connection, "SELECT name_id FROM creature WHERE id = 198500"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1990"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1990"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1990 AND completed_objective_id = 1 AND revealed_objective_id = 2"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1990"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1990 AND kind = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id = 198900"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id BETWEEN 1985000 AND 1985003"));
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id BETWEEN 1985000 AND 1985003"));
            Assert.AreEqual(198650L, Scalar(connection, "SELECT id FROM content_placement"));

            // S2 completes the chain: 1992 becomes offerable through the 1990 turn-in rule.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1992"));
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1992 AND ordinal IS NOT NULL"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985004"));

            // S4 seeds Capture the Flag: the mission, its promotion rule, boss, receiver and ambient assault.
            Assert.AreEqual(198504L, Scalar(connection, "SELECT giver_id FROM npc_mission WHERE id = 1994"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1994 AND ordinal IS NOT NULL"));
            Assert.AreEqual(17L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 198658 AND 198674"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (198506, 198507) AND action1 = 33"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985005"));

            // S5 seeds Calling for Reinforcements and its retry: both missions, the bomb, wreck, corpse and pad NPCs, the fact rules.
            Assert.AreEqual(198505L, Scalar(connection, "SELECT giver_id FROM npc_mission WHERE id = 1995"));
            Assert.AreEqual(198514L, Scalar(connection, "SELECT reciver_id FROM npc_mission WHERE id = 2005"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1995, 2005) AND ordinal IS NOT NULL"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_timer WHERE mission_id IN (1995, 2005) AND limit_seconds = 600 AND on_expire = 2"));
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 198675 AND 198683"));
            Assert.AreEqual(4930L, Scalar(connection, "SELECT fuse_ms FROM content_placement WHERE id = 198677"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 198508 AND 198513"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id BETWEEN 1985006 AND 1985009"));

            // S6 seeds the exit: the pad area, its rule and the Alia Das destination.
            Assert.AreEqual(1220L, Scalar(connection, "SELECT map_context_id FROM content_location WHERE id = 19852"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985010"));

            // BootcampFixRogersTurnIn places the reserved Rogers row in Alia Das and makes him the 1995/2005 receiver.
            Assert.AreEqual(20L, Scalar(connection, "SELECT level FROM creature WHERE id = 198514 AND name_id = 2973"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198684 AND map_context_id = 1220 AND creature_id = 198514 AND npc_package_id = 116 AND present_condition_id = 0"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1995, 2005) AND reciver_id = 198514"));

            // WildernessArrivalTrainingDay places Kincaid in Alia Das and seeds Training Day, its forced offer and the two reward pistols.
            Assert.AreEqual(8L, Scalar(connection, "SELECT level FROM creature WHERE id = 198515 AND name_id = 10604 AND class_id = 3846"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198685 AND map_context_id = 1220 AND creature_id = 198515 AND npc_package_id = 2588 AND present_condition_id = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1526 AND giver_id = 0 AND reciver_id = 198515 AND shareable = 0 AND category_id = 10000001"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1526 AND ordinal = 1 AND is_required = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1526"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id = 198910"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985011 AND action = 1 AND mission_id = 1526 AND forced = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id IN (116929, 116930) AND quality_id = 3 AND inventory_category = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id IN (116929, 116930) AND range = 20"));

            // WildernessClassGear seeds the class-gear missions 2010/2011, Caufield's missing dialogue package,
            // the twelve D11 gear item templates and the two class_selected rules.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 132 AND package_id = 133"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 2010 AND reciver_id = 132 AND category_id = 10000002 AND level = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 2011 AND reciver_id = 132 AND category_id = 10000003 AND level = 5"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (2010, 2011) AND ordinal = 1 AND is_required = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 2010 AND type = 4"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 2011 AND type = 4"));
            Assert.AreEqual(12L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id BETWEEN 122859 AND 122871 AND inventory_category = 1"));
            Assert.AreEqual(10L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_armor WHERE id IN (122859, 122860, 122862, 122863, 122864, 122866, 122867, 122868, 122869, 122870) AND armor_value > 0"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id IN (122865, 122871) AND range = 80"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id IN (198911, 198912)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985012, 1985013) AND event = 13 AND map_context_id = 1220"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id IN (1985012, 1985013) AND action = 1 AND forced = 1"));

            // BootcampAreaVerticalExtent gives the two bridge-standing S1 objective areas a vertical extent, so a
            // recruit walking the ceremonial bridge (deck ~131) is inside a trigger whose Y came from the terrain
            // under it (118.75 and 112.75).
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND shape = 2 AND half_height = 40"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id = 198600 AND pos_x = 387.22 AND pos_z = -28.3 AND radius = 10"));

            // BootcampObjectiveAreaRadius widens 1990's two triggers to the bridge's width after live play showed
            // a recruit crossing the span without touching objective 2's 4 m sphere.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND radius = 10"));

            // WildernessHubConscientiousObjector seeds the hub's conversation chain: four missions, their
            // packages, every objective flagged, the branch transitions and 1390's credits.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1390, 1392, 1393, 1407)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (114, 38) AND package_id IN (1646, 113)"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1390 AND ordinal IS NOT NULL"));
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1390 AND type = 1 AND credits = 400"));
            // WildernessHubConversationChain: five more Wilderness missions whose objectives the client fully binds
            // with conversations, with the experience and credits TaRapedia records.
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (431, 442, 444, 549, 836)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (431, 442, 444, 549, 836) AND ordinal IS NULL"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (431, 442, 444, 549, 836) AND type = 3"));
            // MiresConversationNpc: five Torden NPCs on the mires map 1759 and one liaison on the plateau.
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199400 AND 199405 AND name_id > 0"));
            Assert.AreEqual(7L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN 199400 AND 199405"));

            // MarshesConversationNpc: four more (Lieutenant Morrison and three Retreads) and their missions, all on
            // the main marshes map 1454 that TaRapedia gives them.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199300 AND 199303 AND name_id > 0"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN 199300 AND 199303"));

            // PlateauConversationNpc: six more giver NPCs and their six conversation missions on the Valverde
            // plateau (map 1497) and in the pools (1304), which is where TaRapedia puts them.
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199200 AND 199205 AND name_id > 0"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN 199200 AND 199205"));

            // PalisadesConversationNpc: the same pipeline applied to the Palisades - eight giver NPCs and the
            // nine conversation missions they hand out.
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199100 AND 199107 AND name_id > 0"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199100 AND 199107 AND map_context_id = 1244"));
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN 199100 AND 199107"));

            // DivideConversationNpc: the Divide's conversation missions and the four giver NPCs they needed,
            // created from the client name table (ids), TaRapedia (level/zone/loc) and an appearance analogue.
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (332, 347, 382, 796, 1743)"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199000 AND 199003 AND name_id > 0"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199000 AND 199003 AND kind = 1"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (332, 347, 382, 796, 1743) AND ordinal IS NULL"));

            // WildernessHubKillObjective: two hub missions whose blocking objective is a kill of a creature the world
            // seed carries, bound through the binding kind the boot camp's 1994 already uses.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (427, 682)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id IN (427, 682) AND kind = 6"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 427 AND objective_id = 6 AND creature_id = 76"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 682 AND objective_id = 3 AND creature_id = 77"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (427, 682) AND credits >= 400"));

            // The amount lives in the credits column for both reward types: TaRapedia's experience restored.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (431, 442, 549, 836) AND type = 3 AND credits > 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 431 AND type = 3 AND credits = 2500"));
            // WildernessHubConscientiousObjectorPath makes the client's unbound escort steps optional and reveals
            // Apirka from either answer, so 1390 offers through the conversations the client actually defines.
            // Only the two conversation steps the client binds stay required (1 question Quillas, 10 speak to
            // Apirka); the escort and technical objectives are optional and 10 is revealed from either answer.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1390 AND is_required = 1"));
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390"));

            // BootcampEscortDestinationGround lifts the scripted escort destination onto the walkable surface.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19853 AND pos_y = 120.75"));


            // BootcampPlacementGroundSnap lifts Delessio, the crate and DeSimone onto the walkable surface.
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198655, 198651, 198657) AND pos_y > 120"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198655, 198651) AND pos_y = 122.1"));

            // BootcampRemainingTriggerHeight puts the two sphere triggers on the player's level as cylinders.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198602, 198603) AND shape = 2 AND half_height = 25"));

            // BootcampObjectiveAreaHeight raises the S1 triggers' ceiling: the bridge arches, so the recruit crossed
            // the marker above the volume (the probe showed the height climbing toward it).
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND half_height = 40"));

            // BootcampCaveInTriggerRadius applies OD-44 to 1994 objective 2's cave-in trigger.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id = 198602 AND radius = 10 AND half_height = 25"));

            // ContentRuleActionDamage adds the column and BootcampDetonationDamage fills the blast's -21 into it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985007 AND sequence = 3 AND action = 15 AND damage = 21"));

            // BootcampScriptedMoves seeds the two scripted NPC walks the content layer could not express before.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE purpose = 3 AND map_context_id = 1985"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985014, 1985015)"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id IN (1985014, 1985015) AND action = 14"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985014 AND placement_id = 198650 AND location_id = 19853"));

            // BootcampObjectiveIndicators gives 1990's objectives the world markers the footage shows.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 1990 AND indicator_id IN (430, 431)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 1990 AND objective_id = 1 AND pos_y = 131.3"));

            // WildernessHubReceptiveReception seeds the Alia Das chain's first mission: Solis gives it, Apirka ends it,
            // its first objective waits on the Enhance shrine, and "Too Close For Comfort" gates it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 42 AND package_id = 168"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 43 AND package_id = 112"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1069 AND giver_id = 42 AND reciver_id = 43 AND level = 5"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1069 AND is_required = 1 AND ordinal BETWEEN 1 AND 3"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1069 AND objective_id = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1069"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1069 AND objective_id = 1 AND kind = 8 AND placement_id = 10"));
            // 1069's TaRapedia requirement (1407) is not seeded yet: see GAP-W3-1069-GATE.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 1069"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1069 AND ((type = 3 AND credits = 4000) OR (type = 1 AND credits = 600))"));

            // Add_map_region (upstream PR #91) creates the region table and preloads 373 volumes on 58 maps:
            // the Wilderness ones (context 1220) cover Alia Das and the caverns.
            Assert.AreEqual(373L, Scalar(connection, "SELECT COUNT(*) FROM map_region"));
            Assert.AreEqual(21L, Scalar(connection, "SELECT COUNT(*) FROM map_region WHERE map_context_id = 1220"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM map_region WHERE map_context_id = 1220 AND region_id = 18 AND shape = 1 AND underground = 2 AND enabled = 1"));

            // Rolling W2 back removes every class-gear row and restores the NULL-flag objective skeleton it replaced.
            context.GetService<IMigrator>().Migrate(WildernessArrivalTrainingDayMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'map_region'"));
            // The vertical extent rolls back with it, leaving the areas as the spheres the S1 migration seeded.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND shape = 1 AND half_height = 0"));
            // The hub mission's rows go with their migration, restoring the client's NULL-flag skeleton.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE purpose = 3"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985014, 1985015)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 1990"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1069"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1069"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 1069"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1069"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (42, 43)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND radius = 4"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id = 198602 AND radius = 5"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1069 AND is_required IS NULL AND ordinal IS NULL AND revealed_on_accept IS NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1390, 1392, 1393, 1407)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (114, 38)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1390 AND ordinal IS NOT NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (431, 442, 444, 549, 836)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (427, 682)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id IN (427, 682)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 2010 AND objective_id = 1 AND ordinal IS NULL AND is_required IS NULL AND revealed_on_accept IS NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (2010, 2011)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (2010, 2011)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 132"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id BETWEEN 122859 AND 122871"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_armor WHERE id BETWEEN 122859 AND 122871"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id BETWEEN 122859 AND 122871"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id IN (198911, 198912)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985014, 1985015)"));

            // Rolling it back removes every W1 row and restores the NULL-flag (1526,1) skeleton row it replaced.
            context.GetService<IMigrator>().Migrate(BootcampFixRogersTurnInMigration);
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1526 AND objective_id = 1 AND ordinal IS NULL AND is_required IS NULL AND revealed_on_accept IS NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1526"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1526"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198515"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198685"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id = 198910"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985011"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985011"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id IN (116929, 116930)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id IN (116929, 116930)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198684"));

            // Rolling the correction back restores the S5 receiver (emulator creature 100) and removes both rows.
            context.GetService<IMigrator>().Migrate(BootcampS6Migration);
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1995, 2005) AND reciver_id = 100"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198514"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198684"));

            // Rolling S6 and S5 back restores the NULL-flag objective skeleton that S5 replaced.
            context.GetService<IMigrator>().Migrate(BootcampS4Migration);
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1995, 2005) AND ordinal IS NULL AND is_required IS NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1995, 2005)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19852"));
            Assert.AreEqual(17L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 198658 AND 198699"));

            // Rolling back the pair removes every seeded row and leaves the content tables empty again.
            context.GetService<IMigrator>().Migrate(ContentLayerMigration);
            CollectionAssert.AreEqual(
                new[] { BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration },
                context.Database.GetPendingMigrations().ToArray());
            foreach (var table in WorldTables)
                Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM {table}"), table);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198500"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1990"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 198505 AND 198515"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1995, 2005)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (2010, 2011)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 132"));
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
