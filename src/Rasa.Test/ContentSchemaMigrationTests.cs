using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Migrations.WildernessData;
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
    public partial class ContentSchemaMigrationTests
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
        private const string PlainsConversationNpcMigration = "20260917100000_PlainsConversationNpc";
        private const string InclineConversationNpcMigration = "20260917120000_InclineConversationNpc";
        private const string TordenNpcGroundSnapMigration = "20260917140000_TordenNpcGroundSnap";
        private const string WildernessMortarByNumbersMigration = "20260917160000_WildernessMortarByNumbers";
        private const string WildernessCollectionDropMigration = "20260917180000_WildernessCollectionDrop";
        private const string WildernessGiverFixMigration = "20260917200000_WildernessGiverFix";
        private const string MiresReconstructedSpeciesMigration = "20260917220000_MiresReconstructedSpecies";
        private const string AddCreatureLootMigration = "20260918000000_Add_creature_loot";
        private const string ContentPlacementEscortMigration = "20260918020000_ContentPlacementEscort";
        private const string WildernessEscortMilpasMigration = "20260918040000_WildernessEscortMilpas";
        private const string MissionAreaLinksMigration = "20260918060000_MissionAreaLinks";

        /// <summary>W3: Mining Coord. Richards and the wounded Forean Ranger, the two NPCs 422 and 429 complete through.</summary>
        private const string WildernessPinholeNpcMigration = "20260918080000_WildernessPinholeNpc";

        /// <summary>The fourteen placements that were buried in or floating over the ground, put on the floor.</summary>
        private const string WorldPlacementFloorSnapMigration = "20260918100000_WorldPlacementFloorSnap";

        /// <summary>The twelve world-seed NPCs that had no dialogue package, given the one the client names.</summary>
        private const string WildernessDialogueBindingMigration = "20260918120000_WildernessDialogueBinding";

        /// <summary>The Target Dummy moved out of a sandbag emplacement into the empty range lane.</summary>
        /// <summary>Ellimist's creature_class_flag table (cherry-picked from 0e2a53c, id kept for their databases).</summary>
        /// <summary>Ellimist's client action tables (cherry-picked from 474d1ce, id kept for their databases).</summary>
        private const string AddActionsMigration = "20260917120000_Add_actions";

        private const string AddCreatureClassFlagMigration = "20260917180000_Add_creature_class_flag";

        private const string BootcampTargetDummyLaneMigration = "20260919000000_BootcampTargetDummyLane";

        /// <summary>Seven entityclass rows put back to the client's own table.</summary>
        private const string EntityClassClientFidelityMigration = "20260919120000_EntityClassClientFidelity";

        /// <summary>The supply crate's armour is the uncommon level-1 set the footage's gloves tooltip shows.</summary>
        private const string BootcampCrateUncommonGearMigration = "20260919130000_BootcampCrateUncommonGear";

        /// <summary>Mission 430's mortars are Bane Mortar creatures the client can target, not scenery.</summary>
        private const string WildernessMortarCreatureMigration = "20260919140000_WildernessMortarCreature";

        /// <summary>The Bane Mortars fire their ground-target launcher from where they stand.</summary>
        private const string WildernessMortarFireMigration = "20260919150000_WildernessMortarFire";
        private const string DevilsDenFransiscoMigration = "20260919170000_DevilsDenFransisco";
        private const string BootcampTargetDummyFrontMigration = "20260919180000_BootcampTargetDummyFront";
        private const string ContentNpcAppearanceMigration = "20260919190000_ContentNpcAppearance";
        private const string QuestNpcDialogueBatchMigration = "20260919200000_QuestNpcDialogueBatch";
        private const string TarapediaGiverAuditMigration = "20260919210000_TarapediaGiverAudit";
        private const string TarapediaMissingNpcsMigration = "20260919220000_TarapediaMissingNpcs";
        private const string TarapediaMachineClassMigration = "20260920000000_TarapediaMachineClass";
        private const string BootcampRetryIndicatorsMigration = "20260920010000_BootcampRetryIndicators";
        private const string AddMapMarkerMigration = "20260920020000_Add_map_marker";
        private const string AddRecipeMigration = "20260920030000_Add_recipe";
        private const string RegenerateItemTemplateMigration = "20260920040000_Regenerate_item_template";
        private const string AddServiceNpcsMigration = "20260920050000_Add_service_npcs";
        private const string AddMissionNpcsMigration = "20260920060000_Add_mission_npcs";
        private const string AddClassTrainersMigration = "20260920070000_Add_class_trainers";
        private const string AddBossSpawnsMigration = "20260920080000_Add_boss_spawns";
        private const string PlaceDropshipPadsMigration = "20260920090000_Place_dropship_pads";
        private const string FixLogosShrinesMigration = "20260920100000_Fix_logos_shrines";
        private const string PlaceRemainingLogosMigration = "20260920110000_Place_remaining_logos";
        private const string AddSkillCharacterMigration = "20260920120000_Add_skill_character";
        private const string RetuneWeaponToolTypeMigration = "20260920140000_Retune_weapon_tool_type";
        private const string EllathaNpcPositionsMigration = "20260920150000_EllathaNpcPositions";
        private const string EllathaWorldNpcsMigration = "20260920160000_EllathaWorldNpcs";
        private const string WorldFloorSweepMigration = "20260920170000_WorldFloorSweep";
        private const string QuestGiverBriefingFixMigration = "20260920180000_QuestGiverBriefingFix";
        private const string PropOverlapFixMigration = "20260920190000_PropOverlapFix";
        private const string TarapediaNpcPositionsMigration = "20260920200000_TarapediaNpcPositions";
        private const string WorldSweepCorrectionsMigration = "20260920210000_WorldSweepCorrections";
        private const string TarapediaMissingNpcBatchMigration = "20260920220000_TarapediaMissingNpcBatch";
        private const string PhostBenonFloorMigration = "20260920230000_PhostBenonFloor";
        private const string TarapediaLastNpcsMigration = "20260920240000_TarapediaLastNpcs";
        private const string QuestWiringFixesMigration = "20260920250000_QuestWiringFixes";
        private const string CodexPlacementFixesMigration = "20260920260000_CodexPlacementFixes";
        private const string MissionSpeakersMigration = "20260920270000_MissionSpeakers";
        private const string AppearanceSlotRepairMigration = "20260920280000_AppearanceSlotRepair";
        private const string SeedLogosMissionsMigration = "20260920300000_SeedLogosMissions";
        private const string MissionPropSpeakersMigration = "20260920310000_MissionPropSpeakers";
        private const string WeaponRowRepairMigration = "20260920320000_WeaponRowRepair";
        private const string ContentRuleBarkMigration = "20260920330000_ContentRuleBark";
        private const string ClientNamesForItemsMigration = "20260920340000_ClientNamesForItems";
        private const string CodexNpcCorrectionsMigration = "20260920370000_CodexNpcCorrections";
        private const string RecruitLoadoutFlagsMigration = "20260922110000_RecruitLoadoutFlags";
        private const string BootcampEquipCrateGearMigration = "20260922180000_BootcampEquipCrateGear";
        private const string CreatureFractionalMovementRatesMigration = "20260922190000_CreatureFractionalMovementRates";
        private const string BootcampMcAllisterWalkMigration = "20260922190100_BootcampMcAllisterWalk";
        private const string ContentItemSetInitialAmmoMigration = "20260922200000_ContentItemSetInitialAmmo";
        private const string BootcampCrateLoadedRifleMigration = "20260922200100_BootcampCrateLoadedRifle";
        private const string BootcampPracticeDummyHealthMigration = "20260922210000_BootcampPracticeDummyHealth";
        private const string BootcampFirstLoginYawMigration = "20260922220000_BootcampFirstLoginYaw";
        private const string BootcampCrateRifleRangeMigration = "20260922230000_BootcampCrateRifleRange";
        private const string MoawiDialogueClassMigration = "20260922234000_MoawiDialogueClass";
        private const string MissionSpeakerDialogueClassesMigration = "20260922234500_MissionSpeakerDialogueClasses";
        private const string BootcampRifleMeleeMigration = "20260922235000_BootcampRifleMelee";
        private const string BootcampConradCorpsePlacementMigration = "20260922235500_BootcampConradCorpsePlacement";
        private const string BootcampBombHullPlacementMigration = "20260922235600_BootcampBombHullPlacement";
        private const string WildernessHubReceptiveGateMigration = "20260923010000_WildernessHubReceptiveGate";
        private const string WildernessHubReceptiveLevelMigration = "20260923011000_WildernessHubReceptiveLevel";
        private const string MissionItemDropChanceMigration = "20260923012000_MissionItemDropChance";
        private const string WildernessHubFormingAlliancesMigration = "20260923013000_WildernessHubFormingAlliances";
        private const string WildernessHubConscientiousGateMigration = "20260923014000_WildernessHubConscientiousGate";
        private const string WildernessHubConscientiousBranchesMigration = "20260923015000_WildernessHubConscientiousBranches";
        private const string SolisCavernsPlacementMigration = "20260923020000_SolisCavernsPlacement";
        private const string BootcampDeSimoneCampPlacementMigration = "20260923030000_BootcampDeSimoneCampPlacement";
        private const string BootcampCampGunnerCompanionMigration = "20260923040000_BootcampCampGunnerCompanion";
        private const string BootcampCampArcherShamanCompanionsMigration = "20260923050000_BootcampCampArcherShamanCompanions";
        private const string BootcampCourtyardForeanWarriorMigration = "20260923060000_BootcampCourtyardForeanWarrior";
        private const string BootcampRetryReinforcementWalkMigration = "20260924090000_BootcampRetryReinforcementWalk";
        private const string BootcampLightningHitCreditMigration = "20260924120000_BootcampLightningHitCredit";
        private const string TooCloseForComfortLevelMigration = "20260924130000_TooCloseForComfortLevel";
        private const string BootcampReinforcementPadHoldMigration = "20260924140000_BootcampReinforcementPadHold";
        private const string EarlyReadyMissionsMigration = "20260926100000_EarlyReadyMissions";
        private const string WorldDefectsFixMigration = "20260926130000_WorldDefectsFix";
        private const string LiaisonLogosMissionsMigration = "20260926190000_LiaisonLogosMissions";
        private const string TordenConversationMissionsMigration = "20260926110000_TordenConversationMissions";
        private const string MissionRewardItemsMigration = "20260926120000_MissionRewardItems";

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

        /// <summary>
        /// The missions a batch's NPCs are part of, on either side. The batches seeded most missions with
        /// giver = receiver and MissionAreaLinks then gave the cross-zone hand-offs the givers TaRapedia names, which
        /// moves a mission out of one batch's giver set and into another's receiver set; counting either side is what
        /// the original numbers meant and it stays true as those corrections land.
        /// </summary>
        private static long MissionsInvolving(Microsoft.Data.Sqlite.SqliteConnection connection, uint first, uint last)
            => Scalar(connection, $"SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN {first} AND {last} OR reciver_id BETWEEN {first} AND {last}");

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
            context.Database.ExecuteSqlRaw("DROP TABLE \"creature_loot\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"creature_class_flag\"");
            // Add_map_marker (20260920020000) is later still.
            context.Database.ExecuteSqlRaw("DROP TABLE \"map_marker\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"recipe\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"recipe_input\"");
            context.Database.ExecuteSqlRaw("DROP TABLE \"skill_character\"");
            foreach (var actionTable in new[] { "action", "action_level", "action_cost", "action_property", "action_item_requirement", "item_template_action" })
                context.Database.ExecuteSqlRaw($"DROP TABLE \"{actionTable}\"");
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
                new[] { ContentLayerMigration, BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration, PlainsConversationNpcMigration, AddActionsMigration, InclineConversationNpcMigration, TordenNpcGroundSnapMigration, WildernessMortarByNumbersMigration, AddCreatureClassFlagMigration, WildernessCollectionDropMigration, WildernessGiverFixMigration, MiresReconstructedSpeciesMigration, AddCreatureLootMigration, ContentPlacementEscortMigration, WildernessEscortMilpasMigration, MissionAreaLinksMigration, WildernessPinholeNpcMigration, WorldPlacementFloorSnapMigration, WildernessDialogueBindingMigration, BootcampTargetDummyLaneMigration, EntityClassClientFidelityMigration, BootcampCrateUncommonGearMigration, WildernessMortarCreatureMigration, WildernessMortarFireMigration, DevilsDenFransiscoMigration, BootcampTargetDummyFrontMigration, ContentNpcAppearanceMigration, QuestNpcDialogueBatchMigration, TarapediaGiverAuditMigration, TarapediaMissingNpcsMigration, TarapediaMachineClassMigration, BootcampRetryIndicatorsMigration, AddMapMarkerMigration, AddRecipeMigration, RegenerateItemTemplateMigration, AddServiceNpcsMigration, AddMissionNpcsMigration, AddClassTrainersMigration, AddBossSpawnsMigration, PlaceDropshipPadsMigration, FixLogosShrinesMigration, PlaceRemainingLogosMigration, AddSkillCharacterMigration, RetuneWeaponToolTypeMigration, EllathaNpcPositionsMigration, EllathaWorldNpcsMigration, WorldFloorSweepMigration, QuestGiverBriefingFixMigration, PropOverlapFixMigration, TarapediaNpcPositionsMigration, WorldSweepCorrectionsMigration, TarapediaMissingNpcBatchMigration, PhostBenonFloorMigration, TarapediaLastNpcsMigration, QuestWiringFixesMigration, CodexPlacementFixesMigration, MissionSpeakersMigration, AppearanceSlotRepairMigration, SeedLogosMissionsMigration, MissionPropSpeakersMigration, WeaponRowRepairMigration, ContentRuleBarkMigration, ClientNamesForItemsMigration, CodexNpcCorrectionsMigration, RecruitLoadoutFlagsMigration, BootcampEquipCrateGearMigration, CreatureFractionalMovementRatesMigration, BootcampMcAllisterWalkMigration, ContentItemSetInitialAmmoMigration, BootcampCrateLoadedRifleMigration, BootcampPracticeDummyHealthMigration, BootcampFirstLoginYawMigration, BootcampCrateRifleRangeMigration, MoawiDialogueClassMigration, MissionSpeakerDialogueClassesMigration, BootcampRifleMeleeMigration, BootcampConradCorpsePlacementMigration, BootcampBombHullPlacementMigration, WildernessHubReceptiveGateMigration, WildernessHubReceptiveLevelMigration, MissionItemDropChanceMigration, WildernessHubFormingAlliancesMigration, WildernessHubConscientiousGateMigration, WildernessHubConscientiousBranchesMigration, SolisCavernsPlacementMigration, BootcampDeSimoneCampPlacementMigration, BootcampCampGunnerCompanionMigration, BootcampCampArcherShamanCompanionsMigration, BootcampCourtyardForeanWarriorMigration, BootcampRetryReinforcementWalkMigration, BootcampLightningHitCreditMigration, TooCloseForComfortLevelMigration, BootcampReinforcementPadHoldMigration, EarlyReadyMissionsMigration, TordenConversationMissionsMigration, MissionRewardItemsMigration, WorldDefectsFixMigration, LiaisonLogosMissionsMigration },
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
            context.Database.ExecuteSqlRaw("INSERT INTO map_info (map_context_id, map_name, map_version, base_region) VALUES (1985, 'adv_bootcamp', 783, 4), (1220, 'adv_foreas_concordia_wilderness', 1556, 0), (1148, 'adv_foreas_concordia_divide', 1584, 10), (1244, 'adv_foreas_concordia_palisades', 1584, 10), (1497, 'adv_foreas_valverde_plateau', 1584, 10), (1304, 'adv_foreas_valverde_pools', 1584, 10), (1454, 'adv_foreas_valverde_marshes', 1584, 10), (1759, 'adv_arieki_torden_mires', 1584, 10), (1764, 'adv_arieki_torden_plains', 1584, 10), (1761, 'adv_arieki_torden_incline', 1584, 10), (1394, 'adv_foreas_concordia_palisades_devilsden', 327, 0), (1397, 'adv_foreas_concordia_palisades_treebackcamp', 286, 0), (1347, 'adv_foreas_concordia_divide_minoscaverns', 293, 4), (1773, 'adv_arieki_torden_plains_attacolony', 274, 0), (2034, 'adv_arieki_torden_plains_penalresearch', 230, 0), (1430, 'adv_foreas_concordia_wilderness_pravusresearch', 555, 0), (1506, 'adv_foreas_concordia_wilderness_cavesofdonn02', 535, 8), (1502, 'adv_foreas_valverde_plateau_ustoryard', 187, 9), (1721, 'adv_foreas_concordia_wilderness_clrf', 290, 0), (1734, 'adv_arieki_ligo_ashendesert', 373, 0), (1830, 'adv_foreas_valverde_plateau_maligobasev3', 144, 0), (1865, 'adv_arieki_torden_incline_ojasaattahive', 222, 0), (2028, 'adv_arieki_torden_abyss', 410, 0), (2051, 'adv_foreas_howlingmaw1', 421, 0), (2115, 'adv_arieki_torden_mires_banefluxitemines', 204, 0)");
            context.Database.ExecuteSqlRaw("INSERT INTO npc_mission (id, giver_id, reciver_id, level, group_type, category_id, shareable, radio_completeable, comment) VALUES (321, 101, 100, 5, 1, 1, 0, 0, 'Assemble With Lieutenant Perkins')");
            context.Database.ExecuteSqlRaw("INSERT INTO logos (id, class_id, map_context_id, pos_x, pos_y, pos_z, name) VALUES (23, 7302, 1220, 1, 2, 3, 'Power')");
            var before = WorldRows(connection);

            context.Database.GetService<IMigrator>().Migrate(ContentLayerMigration);
            CollectionAssert.AreEqual(
                new[] { BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration, PlainsConversationNpcMigration, AddActionsMigration, InclineConversationNpcMigration, TordenNpcGroundSnapMigration, WildernessMortarByNumbersMigration, AddCreatureClassFlagMigration, WildernessCollectionDropMigration, WildernessGiverFixMigration, MiresReconstructedSpeciesMigration, AddCreatureLootMigration, ContentPlacementEscortMigration, WildernessEscortMilpasMigration, MissionAreaLinksMigration, WildernessPinholeNpcMigration, WorldPlacementFloorSnapMigration, WildernessDialogueBindingMigration, BootcampTargetDummyLaneMigration, EntityClassClientFidelityMigration, BootcampCrateUncommonGearMigration, WildernessMortarCreatureMigration, WildernessMortarFireMigration, DevilsDenFransiscoMigration, BootcampTargetDummyFrontMigration, ContentNpcAppearanceMigration, QuestNpcDialogueBatchMigration, TarapediaGiverAuditMigration, TarapediaMissingNpcsMigration, TarapediaMachineClassMigration, BootcampRetryIndicatorsMigration, AddMapMarkerMigration, AddRecipeMigration, RegenerateItemTemplateMigration, AddServiceNpcsMigration, AddMissionNpcsMigration, AddClassTrainersMigration, AddBossSpawnsMigration, PlaceDropshipPadsMigration, FixLogosShrinesMigration, PlaceRemainingLogosMigration, AddSkillCharacterMigration, RetuneWeaponToolTypeMigration, EllathaNpcPositionsMigration, EllathaWorldNpcsMigration, WorldFloorSweepMigration, QuestGiverBriefingFixMigration, PropOverlapFixMigration, TarapediaNpcPositionsMigration, WorldSweepCorrectionsMigration, TarapediaMissingNpcBatchMigration, PhostBenonFloorMigration, TarapediaLastNpcsMigration, QuestWiringFixesMigration, CodexPlacementFixesMigration, MissionSpeakersMigration, AppearanceSlotRepairMigration, SeedLogosMissionsMigration, MissionPropSpeakersMigration, WeaponRowRepairMigration, ContentRuleBarkMigration, ClientNamesForItemsMigration, CodexNpcCorrectionsMigration, RecruitLoadoutFlagsMigration, BootcampEquipCrateGearMigration, CreatureFractionalMovementRatesMigration, BootcampMcAllisterWalkMigration, ContentItemSetInitialAmmoMigration, BootcampCrateLoadedRifleMigration, BootcampPracticeDummyHealthMigration, BootcampFirstLoginYawMigration, BootcampCrateRifleRangeMigration, MoawiDialogueClassMigration, MissionSpeakerDialogueClassesMigration, BootcampRifleMeleeMigration, BootcampConradCorpsePlacementMigration, BootcampBombHullPlacementMigration, WildernessHubReceptiveGateMigration, WildernessHubReceptiveLevelMigration, MissionItemDropChanceMigration, WildernessHubFormingAlliancesMigration, WildernessHubConscientiousGateMigration, WildernessHubConscientiousBranchesMigration, SolisCavernsPlacementMigration, BootcampDeSimoneCampPlacementMigration, BootcampCampGunnerCompanionMigration, BootcampCampArcherShamanCompanionsMigration, BootcampCourtyardForeanWarriorMigration, BootcampRetryReinforcementWalkMigration, BootcampLightningHitCreditMigration, TooCloseForComfortLevelMigration, BootcampReinforcementPadHoldMigration, EarlyReadyMissionsMigration, TordenConversationMissionsMigration, MissionRewardItemsMigration, WorldDefectsFixMigration, LiaisonLogosMissionsMigration },
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
            // Two of the seven entityclass rows EntityClassClientFidelity corrects, as the old import left them.
            context.Database.ExecuteSqlRaw("INSERT INTO entityclass (id, class_name, mesh_id, class_collision_role, target_flag, aug_list) VALUES (21307, 'UsableItemDispElohLogos0V01', 0, 1, 1, '')");
            context.Database.ExecuteSqlRaw("INSERT INTO entityclass (id, class_name, mesh_id, class_collision_role, target_flag, aug_list) VALUES (20684, 'MisCavesofDonn_DyingForean', 30264, 1, 1, '8')");
            context.Database.ExecuteSqlRaw("INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) VALUES (38, 'Council Elder Moawi', 6163, 1, 9, 400, 2970, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            context.Database.ExecuteSqlRaw("INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) VALUES (132, 'AFS Quartermaster Caufield', 29423, 1, 5, 400, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");
            // World-seed Outpost Commander Randolph (130) and Receptive Liaison Standley (134), on the Redshirt class
            // without NPC augmentation, as EarlyReadyMissions finds them.
            context.Database.ExecuteSqlRaw("INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, action1, action2, action3, action4, action5, action6, action7, action8) VALUES " +
                "(130, 'AFS Outpost Commnader Randolph', 29423, 1, 10, 1000, 3076, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), " +
                "(134, 'Receptive Liason Standley', 29423, 1, 10, 1000, 10011, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");

            // Original-seed spawn pools 184 (named Council Elder Solis, creature 42) and 92 (an unnamed Forean
            // shaman, creature 52), as the world seed places them before SolisCavernsPlacement.
            context.Database.ExecuteSqlRaw("INSERT INTO spawnpool (id, mode, anim_type, respown_time, pos_x, pos_y, pos_z, rotation, map_context_id, creature_1_Id, creature_1_min_count, creature_1_max_count, creature_2_Id, creature_2_min_count, creature_2_max_count, creature_3_Id, creature_3_min_count, creature_3_max_count, creature_4_Id, creature_4_min_count, creature_4_max_count, creature_5_Id, creature_5_min_count, creature_5_max_count, creature_6_Id, creature_6_min_count, creature_6_max_count) VALUES " +
                "(184, 0, 0, 20, 809.3008, 302.09375, 503.76562, 5.54, 1220, 42, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), " +
                "(92, 0, 0, 100, 786.8711, 287.23828, 581.46875, 3.0, 1220, 52, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)");

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
            // packages, every objective flagged, the corrected branch transitions and 1390's credits.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1390, 1392, 1393, 1407)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (114, 38) AND package_id IN (1646, 113)"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1390 AND ordinal IS NOT NULL"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1390 AND type = 1 AND credits = 400"));
            // WildernessHubConversationChain: five more Wilderness missions whose objectives the client fully binds
            // with conversations, with the experience and credits TaRapedia records.
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (431, 442, 444, 549, 836)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (431, 442, 444, 549, 836) AND ordinal IS NULL"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (431, 442, 444, 549, 836) AND type = 3"));
            // ContentPlacementEscort + WildernessEscortMilpas: the escort column and the Conscientious Objector
            // escort (Ranger Milpas walking with the player to Warrior Apirka or the Divide entrance).
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199803 AND behavior = 3 AND escort_mission_id = 1390"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198605, 198606)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1390 AND kind = 1"));

            // Add_creature_loot: the original creature_type_loot rows, on this world's Thrax soldiers (three
            // creatures x the seven surviving rows).
            Assert.AreEqual(21L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE creature_id = 3 AND item_template_id = 28 AND chance = 12 AND stacksize_min = 1 AND stacksize_max = 35"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE item_template_id = 44917 AND chance = 5"));
            Assert.AreEqual(15L, Scalar(connection, "SELECT COUNT(*) FROM creature_loot WHERE chance = 0.5"));

            // MiresReconstructedSpecies: three givers (OD-45), three species from the client's class table placed as
            // clusters around the mission's own area (OD-48), and the missions that count them.
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199800 AND 199802"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199810 AND 199812"));
            Assert.AreEqual(32L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199820 AND 199865"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_counter WHERE mission_id = 956 AND target_value = 12"));

            // WildernessGiverFix: four conversation missions whose giver the sources name without the world seed's
            // rank prefix, and which 751 deliberately stays out of.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (421, 422, 451, 698)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 751"));

            // WildernessCollectionDrop: the missions whose objectives ask for a creature drop, counted as kills of the
            // creature that drops it (OD-47).
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (767, 771, 787)"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_counter WHERE mission_id IN (767, 771, 787) AND target_value > 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 767 AND creature_id = 88 AND counter_id = 0"));

            // WildernessPinholeNpc: the two NPCs 422 and 429 complete their objectives through. Their positions are
            // the client's own map markers (uimapmarker text ids 40 and 39) with the navmesh floor as Y, and their
            // conversation packages are the two the client's objectiveconversation table names.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (199910, 199911) AND name_id IN (3077, 3013)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199910, 199911) AND package_id IN (213, 726)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199910 AND map_context_id = 1220 AND creature_id = 199910 AND npc_package_id = 213 AND behavior = 1 AND pos_x = 341.16 AND pos_y = 228.7 AND pos_z = 477.85"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199911 AND map_context_id = 1220 AND creature_id = 199911 AND npc_package_id = 726 AND behavior = 1 AND pos_x = 309.51 AND pos_y = 271.38 AND pos_z = 436.97"));
            // 422 is turned in at Richards, which is what TaRapedia records him as.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 422 AND giver_id = 198514 AND reciver_id = 199910"));
            // 429 finally carries the flags the loader needs to offer it: the recon (5) first and revealed on
            // acceptance, the report (4) second.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 429 AND objective_id = 5 AND ordinal = 1 AND is_required = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 429 AND objective_id = 4 AND ordinal = 2 AND is_required = 1 AND revealed_on_accept = 0"));
            // The recon reveals the report; without the transition the loader refuses the mission.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 429 AND completed_objective_id = 5 AND revealed_objective_id = 4"));

            // The InfiniteRasa data pass: the service NPCs the client's markers place, the mission NPCs whose place a
            // mission sentence states, the class trainers, the named bosses, and the skill level requirements. The 25
            // mission NPCs this branch had already reconstructed from TaRapedia's own /loc are not among them.
            Assert.AreEqual(324L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 500001 AND 500325"));   // 325 less the one in the boot camp
            Assert.AreEqual(177L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 510001 AND 510999"));
            // Corporal Orton (199086) deliberately shares his name id with upstream's 510196: CodexNpcCorrections creates
            // him on the Palisades where the client's mission text puts him and flags the Pools row rather than deleting it.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature a JOIN creature b ON a.name_id = b.name_id AND a.id < b.id WHERE a.name_id <> 0 AND a.id BETWEEN 198500 AND 199999 AND a.id <> 199086 AND b.id BETWEEN 510001 AND 510999"));
            Assert.AreEqual(73L, Scalar(connection, "SELECT COUNT(*) FROM skill_character"));
            // ContentRuleBark: the play_bark column, and McAllister's line on the recruit's return from the
            // holograms. The sphere is the client's own 20 m bark range, centred on McAllister himself, so
            // "in the area" and "able to hear him" are the same event.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id = 198604 AND map_context_id = 1985 AND shape = 1 AND radius = 20.0 AND half_height = 0.0"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id = 198913 AND or_group = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985016 AND map_context_id = 1985 AND event = 10 AND area_id = 198604 AND condition_id = 198913 AND mission_id = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985016 AND sequence = 0 AND action = 16 AND placement_id = 198650 AND bark_id = 852"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE bark_id <> 0 AND rule_id <> 1985016"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area a JOIN content_placement p ON p.id = 198650 WHERE a.id = 198604 AND a.pos_x = p.pos_x AND a.pos_y = p.pos_y AND a.pos_z = p.pos_z"));

            // EllathaWorldNpcs: seven the database records that this world did not have, at its coordinates.
            Assert.AreEqual(7L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199014 AND 199020"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature a JOIN creature b ON a.name_id = b.name_id AND a.id < b.id WHERE a.name_id <> 0 AND b.id BETWEEN 199014 AND 199020"));

            // WorldFloorSweep: every body in the world measured against the navmesh floor again, spawn pools
            // included this time. 114 pools and 14 placements were more than half a metre off it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 500005 AND pos_y = 163.292"));      // was 174.3, 11 m over the landing zone
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 500171 AND pos_y = 13.396"));       // was 19.3, the worst of the vendors
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 500128 AND pos_y = 208.879"));      // was 213.6, Outpost Lexington's hospital
            // It lifted Lt. Galloway (199107) 40 m on the overworld; WorldDefectsFix supersedes that with the Treeback
            // Camp instance her Y was always the floor of - checked with that migration below.

            // QuestGiverBriefingFix: the missions whose giver was not the character their own briefing names.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1392, 1393) AND giver_id = 43 AND reciver_id = 198514"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 976 AND giver_id = 199021 AND reciver_id = 199021"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 199021 AND name_id = 8657"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199021 AND map_context_id = 1759 AND creature_id = 199021"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 199021"));

            // PropOverlapFix: the seven NPCs standing inside the map's own furniture, pushed clear of it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 501004 AND pos_x = 760.671 AND pos_z = 386.774"));   // the cot at Alia Das
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199210 AND pos_x = 450.44 AND pos_z = 367.421"));
            // TarapediaNpcPositions: the wiki's own location table, for the NPCs more than 25 m from it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510002 AND pos_x = 870 AND pos_z = 389"));           // Brigadier General Beacham
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510082 AND pos_x = 210 AND pos_z = -215"));          // Agent Zim, Irendas Penal Colony
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id IN (171, 184) AND pos_x IN (-46, 784.7)"));            // the original server's own rows are not moved

            // WorldSweepCorrections: the three rows the sweep itself left wrong, found by the audits it shipped.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510189 AND pos_y = 350.383"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id IN (500319, 500325) AND pos_x IN (-33.85, 70.151)"));

            // TarapediaMissingNpcBatch: the 51 NPCs the wiki documents that this world did not have.
            Assert.AreEqual(51L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199022 AND 199073"));
            Assert.AreEqual(51L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199022 AND 199073 AND kind = 1"));
            // Nothing of this batch stands in the boot camp: what is in the camp is the manifest's business.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199022 AND 199073 AND map_context_id = 1985"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature a JOIN creature b ON a.name_id = b.name_id AND a.id < b.id WHERE a.name_id <> 0 AND b.id BETWEEN 199022 AND 199073"));
            // A swapset body with no appearance rows renders naked and headless, which is how Youngblood was found.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature c WHERE c.id BETWEEN 199022 AND 199073 AND c.class_id IN (3846, 3848, 6339, 6340, 7775, 7776) AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id)"));
            // Field Officer Hogan at Foreas Base, the wiki's own x and z with the navmesh floor for y.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement p JOIN creature c ON c.id = p.creature_id WHERE c.name_id = 153 AND p.map_context_id = 1148 AND p.pos_x = -4.3 AND p.pos_z = 536.6"));

            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199034 AND pos_y = 229.114"));

            // TarapediaLastNpcs: the eight the parser had missed, and the five bodies the wiki puts elsewhere.
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199074 AND 199081 AND kind = 1"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature c WHERE c.id BETWEEN 199074 AND 199081 AND c.class_id IN (3846, 3848) AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510199 AND map_context_id = 1454"));   // Lieutenant Holloway, off the Pools
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199042 AND map_context_id = 1759")); // Rijii, into the Mires

            // QuestWiringFixes: the conversation packages that sat on the wrong body, and the log order.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199010 AND npc_package_id = 67"));    // 347 completes at Kapler
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199511 AND npc_package_id = 1095"));  // 640 at the mainframe
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199412 AND npc_package_id = 1062"));  // 940 at Ricardo
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199510 AND npc_package_id = 1119"));  // 1063 at Liu
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199001, 199500, 199400, 199501, 199002) AND npc_package_id <> 0"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199001, 199500, 199400, 199501, 199002, 510085, 100)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1904 AND reciver_id = 199303"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 429"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1390 AND type = 3 AND credits = 2000"));
            // The log sorts by ordinal, so these are what the player reads in order.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 332 AND objective_id = 3 AND ordinal = 2"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 969 AND completed_objective_id = 3 AND revealed_objective_id = 1"));

            // CodexPlacementFixes: the Staging Point trio beside the Staging Point, and the arena's own hospital.
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id IN (510121, 510125, 510127) AND pos_x > 880"));
            // The arena hospital's map is checked in MissionLinkAuditTests instead: teleporter rows are preloader
            // seed data and this database is built from the migrations alone, so the row is not here to update.

            // MissionSpeakers: the three the client sends the player to, who were not here.
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199082 AND 199084 AND kind = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199082 AND npc_package_id = 74 AND map_context_id = 1148"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199000, 199401, 199402) AND npc_package_id <> 0"));
            Assert.AreEqual(18L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id BETWEEN 199082 AND 199084"));

            // AppearanceSlotRepair: the officer suit keyed to the slots the client gives it.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE Class_id = 4021 AND slot_id = 3"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE Class_id = 4022 AND slot_id = 15"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE Class_id = 4023 AND slot_id = 16"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE slot_id = 1015"));
            // Creatures 8 and 23 are preloader rows, not migration rows, so they are not in this database to
            // repair; AppearanceSlotAuditTests checks them against the client's own slot table in the real world.

            // SeedLogosMissions: 23 Logos missions, each bound to a shrine this world already places. Verified
            // against the real world database as well: all 24 shrines exist and sit on their giver's own map.
            const string LogosMissions = "(1645,1648,1649,1650,1651,1654,1655,1657,1658,1659,1661,1663,1664,1665," +
                                         "1666,1667,1668,1669,1670,1671,1749,1750,1814)";
            Assert.AreEqual(23L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission WHERE id IN {LogosMissions}"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission m WHERE m.id IN {LogosMissions} AND (m.giver_id <> m.reciver_id OR NOT EXISTS (SELECT 1 FROM content_placement p WHERE p.creature_id = m.giver_id AND p.kind = 1))"));
            Assert.AreEqual(24L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN {LogosMissions} AND is_required = 1 AND revealed_on_accept = 1 AND ordinal > 0"));
            Assert.AreEqual(24L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id IN {LogosMissions} AND binding_id = 0 AND kind = 8 AND placement_id <> 0 AND counter_id = 255"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective o WHERE o.mission_id IN {LogosMissions} AND NOT EXISTS (SELECT 1 FROM npc_mission_objective_binding b WHERE b.mission_id = o.mission_id AND b.objective_id = o.objective_id)"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id IN {LogosMissions}"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1659 AND level = 30 AND giver_id = 199405"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission m JOIN creature c ON c.id = m.giver_id WHERE m.id IN {LogosMissions} AND m.id <> 1659 AND m.level <> c.level"));
            Assert.AreEqual(46L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id IN {LogosMissions} AND type IN (1, 3) AND credits > 0 AND item_template_id = 0 AND quantity = 0"));

            // MissionPropSpeakers: the two props that finish a mission, given a body that can be spoken to.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199912, 199913) AND kind = 1 AND creature_id = id AND entity_class_id = 0 AND usable_kind = 0"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199912, 199913) AND package_id IN (1486, 1300)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199912 AND npc_package_id = 1486 AND map_context_id = 1220 AND pos_y = 220.642"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199913 AND npc_package_id = 1300 AND map_context_id = 1773 AND pos_y = 191.75"));

            // WeaponRowRepair: 3381 templates had no weapon row at all, and the tooltip reads WeaponInfo
            // unconditionally; 120 blades read Ranged.
            // The 2444 shipped rows are preloader seed data and are not in this database; WeaponRowAuditTests
            // checks the whole table against the client in the real world. Here: the 3381 new rows exist ...
            Assert.IsTrue(Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon") >= 3385L, "the 3381 new weapon rows and the four migration rows");
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon w WHERE NOT EXISTS (SELECT 1 FROM itemtemplate t WHERE t.id = w.id)"));
            // ... and the 145 new melee rows are melee (the 120 blades are seed rows, checked in the world).
            Assert.IsTrue(Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE attack_type = 1") >= 145L, "the new melee rows");
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id IN (116929, 116930) AND tool_type = 8"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id = 122871 AND tool_type = 6"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE id = 116929 AND windup = 0 AND recovery = 250 AND refire = 150 AND range = 20 AND alt_max_damage = 115"));

            // ClientNamesForItems and the talking props' classes are checked in MissionLinkAuditTests: entityclass
            // is preloader seed data and is not in this database.

            // CodexNpcCorrections: the eleven NPCs codex-tr.net recovers and the three positions a second source moves.
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199085 AND 199095"));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199085 AND 199095 AND kind = 1"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature c WHERE c.id BETWEEN 199085 AND 199095 AND c.class_id IN (3846, 3848, 6339, 6340, 7775, 7776) AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199085 AND 199095 AND map_context_id = 1985"));
            // Corporal Orton is the one duplicate name id, and it is deliberate: the Pools row is flagged, not deleted.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature a JOIN creature b ON a.name_id = b.name_id AND a.id < b.id WHERE a.name_id <> 0 AND a.id BETWEEN 199085 AND 199095"));   // a is 199086, b is 510196
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 510196 AND comment LIKE 'Corporal Orton - Obelisk (FLAG:%'"));
            // One seed row overruled on two independent sources (Burke), two InfiniteRasa rows corrected; Solis held.
            // Pools 171 and 184 are the original server's seed rows, not in this database: Burke's move is checked
            // against the real world in MissionLinkAuditTests, and Solis is not moved.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510005 AND pos_x = -754 AND pos_z = -279"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510004 AND pos_x = 789 AND pos_z = 369"));
            // The four CP Defense Vendors were refused: 9472 is the launch client's name for the Prestige Vendor's slot.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE name_id = 9472"));
            // Every Y is a probed navmesh floor; a marker height or a 0.0 placeholder never ships.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199085 AND 199095 AND pos_y < 100"));

            // EllathaNpcPositions: the two NPCs this branch had guessed at, where the live-game fan site puts them.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199008 AND pos_x = -77 AND pos_z = 176"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199011 AND pos_x = -556 AND pos_z = 426"));
            // Retune_weapon_tool_type: every row carried 15, which the client's tooltype table does not have - it
            // printed a damage line on tools, a blank line on every weapon, and broke reload modifiers.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_weapon WHERE tool_type = 15"));
            // The dropship pad corrections are guarded updates against rows the world seed carries, so they are
            // checked against the world itself in MissionLinkAuditTests.TheDropshipPadsAreReachable.

            // Add_recipe and Regenerate_item_template (InfiniteRasa 492954a): crafting needs every item template to
            // exist and to carry a price, and the table held 4985 stubs priced at a credit each.
            Assert.AreEqual(160L, Scalar(connection, "SELECT COUNT(*) FROM recipe"));
            Assert.AreEqual(30225L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM recipe r WHERE NOT EXISTS (SELECT 1 FROM itemtemplate t WHERE t.id = r.id) OR NOT EXISTS (SELECT 1 FROM itemtemplate t WHERE t.id = r.result_template_id)"));
            // The Training Day reward pistol: the price the class's loot value gives it, where this branch had none.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id = 116929 AND buy_price = 500 AND sell_price = 126"));

            // Add_map_marker (InfiniteRasa 492954a): the map screen's status markers, keyed by the client's own marker
            // entity ids and tied to the teleporter and crafting-station rows whose state they show.
            Assert.AreEqual(307L, Scalar(connection, "SELECT COUNT(*) FROM map_marker"));
            // What each marker points at is checked against the world itself in
            // MissionLinkAuditTests.EveryMapMarkerPointsAtSomethingInTheWorld; this database has the tables but not
            // the seed's rows.

            // BootcampRetryIndicators: the retry (2005) repeats 1995's objectives 1 and 4 and had no waypoint at all,
            // so its timer ran down with nothing on the map to walk towards.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 2005"));

            // TarapediaMissingNpcs: the twenty-four NPCs the rest of TaRapedia's givers and reward givers name, and the
            // dialogue packages five of them carry - including the Computer Access Terminal that mission 1112 turns in at.
            Assert.AreEqual(24L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199007, 199008, 199009, 199010, 199011, 199012, 199013, 199109, 199110, 199111, 199209, 199210, 199304, 199305, 199409, 199410, 199411, 199412, 199413, 199508, 199509, 199510, 199511, 199512)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1112 AND reciver_id = 199512"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 199512 AND package_id = 1213"));
            // TarapediaMachineClass: a console class carries the NPC augmentation but not Creature, so the server cannot
            // build an actor from it; both machines use the world's own talking machine instead.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (199511, 199512) AND class_id = 10642"));
            // 321 is a world-seed mission, so it is not in a database built from the migrations alone; 1310 is one of ours.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1310 AND giver_id = 199509"));

            // TarapediaGiverAudit: eighteen missions were given or turned in at the wrong NPC, against TaRapedia's own
            // MissionGiver and RewardGiver. A sample of them, including 1407, whose row named Moawi where its own
            // provenance said Outpost Commander Rogers.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 366 AND giver_id = 199103"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 367 AND giver_id = 199104"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 413 AND giver_id = 199101"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 421 AND reciver_id = 116"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 444 AND reciver_id = 94"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 451 AND reciver_id = 91"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1407 AND giver_id = 198514"));
            // The first objective is a conversation with Moawi. His seed class has no client NPC
            // augmentation, so a package and server-side marker alone cannot make him speakable.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 38 AND class_id = 28415"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 132 AND class_id = 3846"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 132"));

            // QuestNpcDialogueBatch: eight NPCs at TaRapedia's own /loc, each carrying the package its objective completes
            // through, and Captain Reyko moved off the console's line (1213) onto his own (1200).
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199006, 199207, 199208, 199406, 199407, 199408, 199506, 199507)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 199502 AND package_id = 1200"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 199006 AND package_id = 177"));

            // ContentNpcAppearance: NPC_Human_Swapset_Male (3846) is assembled from clothing pieces, so a created NPC with no
            // creature_appearance rows renders bare - head included. Every one of them carries a shipped analogue set.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature c WHERE c.class_id = 3846 AND NOT EXISTS (SELECT 1 FROM creature_appearance a WHERE a.id = c.id)"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 198505"));

            // BootcampTargetDummyFront: the Target Dummy stands in front of its lane's sandbags as the practice dummy does in
            // its own (BootcampTargetDummyLane's centre line was inside the emplacement, and the client refused it).
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198653 AND pos_x = 379.21 AND pos_y = 119.4 AND pos_z = 186.8"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198652 AND pos_x = 384.7"));

            // WildernessMortarCreature: the four mortars are kill targets of one Bane Mortar creature (class 7482,
            // name 8186, the creature mortar launcher 10604 in its weapon slot), where the client map puts them.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 199720 AND class_id = 7482 AND name_id = 8186 AND faction = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 199720 AND slot_id = 13 AND Class_id = 10604"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199700 AND 199703 AND kind = 1 AND creature_id = 199720 AND entity_class_id = 0 AND respawn_ms = 60000"));
            // WildernessMortarFire: they guard their spot and fire the launcher's action 411 with the client's timing.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199700 AND 199703 AND behavior = 2"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature c JOIN creature_action a ON a.id = c.action1 WHERE c.id = 199720 AND a.action_id = 411 AND a.action_arg_id = 1 AND a.windup = 500 AND a.cooldown = 1454 AND a.range_max = 60"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199700 AND pos_x = 360.8661 AND pos_z = 95.2629"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 430 AND kind = 6 AND placement_id BETWEEN 199700 AND 199703 AND destroying_hit_only = 0"));

            // BootcampCrateUncommonGear: the crate holds the uncommon gloves whose tooltip the footage shows
            // ("Body Armor: 28", absorb 281), and the rest of the level-1 uncommon set - none of the common rows.
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM content_item_set WHERE item_set_id = 19858 AND item_template_id IN (12209, 15803, 26879, 12208, 13713)"));
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM content_item_set WHERE item_set_id = 19858"));

            // EntityClassClientFidelity: a corrupted name and the dying Forean's augmentation list are the client's.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM entityclass WHERE id = 21307 AND class_name = 'UsableItemDispElohLogosNoneV01'"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM entityclass WHERE id = 20684 AND aug_list = '52'"));

            // WildernessDialogueBinding: twelve world-seed NPCs that stood in the world with nothing to say.
            // Each carries the package the client's objectiveconversation table binds to an objective of theirs,
            // and none of them had an npc_package row before, so fourteen objectives stop being dead ends.
            Assert.AreEqual(12L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (91, 92, 94, 98, 99, 103, 115, 116, 121, 125, 138, 139)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 138 AND package_id = 251"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 92 AND package_id = 566"));
            // Engineer Salter: the seed has two, and 115 is the one at Alia Das.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 115 AND package_id = 382"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 117"));

            // WorldPlacementFloorSnap: the fourteen placements that were not on the ground. The boot camp's
            // base-gate Thrax was 0.73 m under it - the live "thrax infantry is in the ground" of 2026-09-17 -
            // and Lt. Gerry 1.48 m over it. Nothing else moved, and the bomb on the wreck hull is left alone.
            Assert.AreEqual(14L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198674, 199206, 199303, 199402, 199603, 199821, 199822, 199823, 199824, 199825, 199828, 199840, 199864, 199865)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198674 AND pos_y = 110.23"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199603 AND pos_y = 223.48"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199402 AND pos_y = 229.46"));
            Assert.AreEqual(1L, Scalar(connection, $"SELECT COUNT(*) FROM content_placement WHERE id = {WorldPlacementFloorSnapRows.MountedBomb} AND pos_y = 102.3"));

            // WildernessMortarByNumbers: mission 430's four mortars at the positions the client map gives them, one
            // bound to each objective. WildernessMortarCreature later makes them creatures (asserted above).
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199700 AND 199703"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 430 AND objective_id BETWEEN 3 AND 6"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 430 AND type = 3 AND credits = 6000"));

            // TordenNpcGroundSnap: Receptive Liaison Sage's Y moved from TaRapedia's reading (237) to the surface
            // the client's navmesh has under her (233.8).
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199504 AND pos_y = 233.8"));

            // PlainsConversationNpc: five Torden NPCs on the plains (1764) and the incline (1761) with twelve
            // missions between them; two missions were skipped because Colonel Franks has no /loc recorded anywhere.
            // One of the twelve is the incline's 1747, which Sage hands out from here.
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199500 AND 199504 AND name_id > 0"));
            Assert.AreEqual(17L, MissionsInvolving(connection, 199500, 199504));   // 12 plus the 5 Logos missions SeedLogosMissions hangs on the Liaison here
            // InclineConversationNpc: two more NPCs and their missions. Both were first seeded with
            // giver = receiver because no giver was known; MissionAreaLinks gave them the givers TaRapedia names -
            // 1747 is handed out by Sage (199504) and 1826 by Parsons - so the incline pair now *receive* both, and
            // only 1748 is handed out from here, by Maddox.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199600 AND 199601 AND name_id > 0"));
            Assert.AreEqual(7L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE giver_id BETWEEN 199600 AND 199601"));   // 1748, plus the six Mires Logos missions SeedLogosMissions hangs on Maddox
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE reciver_id BETWEEN 199600 AND 199601 AND id IN (1747, 1826)"));

            // MiresConversationNpc: five Torden NPCs on the mires map 1759 and one liaison on the plateau.
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199400 AND 199405 AND name_id > 0"));
            Assert.AreEqual(8L, MissionsInvolving(connection, 199400, 199405));   // 7 plus the 1 Logos missions SeedLogosMissions hangs on the Liaison here

            // MarshesConversationNpc: four more (Lieutenant Morrison and three Retreads) and their missions, all on
            // the main marshes map 1454 that TaRapedia gives them.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199300 AND 199303 AND name_id > 0"));
            Assert.AreEqual(4L, MissionsInvolving(connection, 199300, 199303));

            // PlateauConversationNpc: six more giver NPCs and their six conversation missions on the Valverde
            // plateau (map 1497) and in the pools (1304), which is where TaRapedia puts them.
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199200 AND 199205 AND name_id > 0"));
            // Two of the six are handed out from the Divide (Bosley) and received here. Five after TarapediaGiverAudit:
            // Incommunicado is Colonel Bosley's (199206) on both ends, as TaRapedia records it, not Carvelle's.
            // TordenConversationMissions adds two more received here: 1326 at Snake (199203) and 1330 at Amee Corman (199204).
            Assert.AreEqual(7L, MissionsInvolving(connection, 199200, 199205));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1040 AND giver_id = 199206 AND reciver_id = 199206"));

            // PalisadesConversationNpc: the same pipeline applied to the Palisades - eight giver NPCs and the
            // nine conversation missions they hand out.
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id BETWEEN 199100 AND 199107 AND name_id > 0"));
            // Six stand on the overworld; Bagby and Galloway in the Treeback Camp instance since WorldDefectsFix.
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id BETWEEN 199100 AND 199107 AND map_context_id = 1244"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199106, 199107) AND map_context_id = 1397"));
            Assert.AreEqual(9L, MissionsInvolving(connection, 199100, 199107));

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
            // The branch-aware runtime requires the selected escort and return sequence.
            // Objective 1 is the shared required step; the two route endings are alternative requirements.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1390 AND is_required = 1"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390"));

            // BootcampEscortDestinationGround lifts the scripted escort destination onto the walkable surface.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19853 AND pos_y = 120.75"));


            // Delessio and the crate remain above y=120. DeSimone's later
            // camp-placement correction puts him on the lower camp floor.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198655, 198651, 198657) AND pos_y > 120"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198655, 198651) AND pos_y = 122.1"));

            // The separately observed level-2 Forean Warrior is staged on the
            // courtyard floor only during the 1994 cave-fight condition.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198519 AND class_id = 6043 AND level = 2 AND action1 = 5 AND action2 = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198691 AND creature_id = 198519 AND pos_x = 294 AND pos_y = 120.5 AND pos_z = 65 AND behavior = 2 AND present_condition_id = 198914"));

            // BootcampRemainingTriggerHeight puts the two sphere triggers on the player's level as cylinders.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198602, 198603) AND shape = 2 AND half_height = 25"));

            // BootcampObjectiveAreaHeight raises the S1 triggers' ceiling: the bridge arches, so the recruit crossed
            // the marker above the volume (the probe showed the height climbing toward it).
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id IN (198600, 198601) AND half_height = 40"));

            // BootcampCaveInTriggerRadius applies OD-44 to 1994 objective 2's cave-in trigger.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_area WHERE id = 198602 AND radius = 10 AND half_height = 25"));

            // ContentRuleActionDamage adds the column and BootcampDetonationDamage fills the blast's -21 into it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985007 AND sequence = 3 AND action = 15 AND damage = 21"));

            // BootcampScriptedMoves seeded two scripted NPC walks. McAllister's remains; BootcampReinforcementPadHold
            // later removes the reinforcements' walk-off (1985015) and its retry copy (1985017) together with their
            // inferred destination 19854, because B2-012 shows the three still on the pad 11.5 s after 1995/1.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE purpose = 3 AND map_context_id = 1985"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198500 AND walk_speed = 2.5 AND run_speed = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985014 AND event = 2 AND mission_id = 1992"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985014, 1985015)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id IN (1985014, 1985015) AND action = 14"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985014 AND placement_id = 198650 AND location_id = 19853"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id IN (1985015, 1985017)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id IN (1985015, 1985017) OR location_id = 19854"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19854"));
            // EarlyReadyMissions: 1742, 441, 434 and 408 from the 2026-09-26 dossiers. Single required objective each,
            // TaRapedia's experience and credits, 441 gated on 549, Randolph bound to 434's completion package 212, and
            // Randolph/Standley on NPC swapset classes (female 3848, male 3846) with their analogue outfits.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1742 AND giver_id = 134 AND reciver_id = 199003 AND level = 5 AND group_type = 1 AND category_id = 10000001 AND shareable = 0 AND radio_completeable = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 441 AND giver_id = 130 AND reciver_id = 125 AND level = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 434 AND giver_id = 101 AND reciver_id = 130 AND level = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 408 AND giver_id = 199089 AND reciver_id = 510133 AND level = 15"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE ((mission_id = 1742 AND objective_id = 2) OR (mission_id IN (441, 434, 408) AND objective_id = 1)) AND is_required = 1 AND ordinal = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE item_template_id = 0 AND quantity = 0 AND ((id = 1742 AND ((type = 3 AND credits = 6000) OR (type = 1 AND credits = 600))) OR (id = 441 AND ((type = 3 AND credits = 3500) OR (type = 1 AND credits = 700))) OR (id = 434 AND ((type = 3 AND credits = 3000) OR (type = 1 AND credits = 600))) OR (id = 408 AND ((type = 3 AND credits = 10000) OR (type = 1 AND credits = 2000))))"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1742, 441, 434, 408)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 441 AND or_group = 0 AND required_mission_id = 549 AND required_state = 4"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN (1742, 434, 408)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 130 AND package_id = 212"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 130 AND class_id = 3848"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 134 AND class_id = 3846"));
            Assert.AreEqual(5L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 130 AND slot_id IN (2, 14, 15, 16, 17)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 130 AND slot_id = 15 AND Class_id = 4023 AND color = 13933202"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 134"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 134 AND slot_id = 17 AND Class_id = 24008 AND color = 4286690539"));
            // The client's redirect rows on the givers' packages stay as the client ships them; the loader, not the
            // data, keeps them from completing anything (MissionRedirectConversations).
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE (mission_id = 434 AND npc_package_id = 208) OR (mission_id = 441 AND npc_package_id = 212)"));
            // MissionRewardItems gives seven already-seeded missions their TaRapedia post-1.4 reward items as one
            // choice each (type 5), in the wiki's order, beside the credit and experience rows they already had.
            AssertMissionRewardItems(connection, present: true);
            // Its rollback takes every row back out and leaves the client's NULL-flag objectives and the Redshirt class.
            context.GetService<IMigrator>().Migrate(BootcampReinforcementPadHoldMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1742, 441, 434, 408)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1742, 441, 434, 408)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 441"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE ((mission_id = 1742 AND objective_id = 2 AND comment = 'Report to Liaison Brice') OR (mission_id = 441 AND objective_id = 1 AND comment = 'Inform Duncan.') OR (mission_id = 434 AND objective_id = 1 AND comment = 'Deliver the field report.') OR (mission_id = 408 AND objective_id = 1 AND comment = 'Return to Jorai')) AND is_required IS NULL AND ordinal IS NULL AND revealed_on_accept IS NULL"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 130"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (130, 134) AND class_id = 29423"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id IN (130, 134)"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE (mission_id = 434 AND npc_package_id = 208) OR (mission_id = 441 AND npc_package_id = 212)"));
            // Rolling the pad hold back restores both walk-offs: the 1995 original and the retry copy that
            // BootcampRetryReinforcementWalk added for 2005, where the same destroyed-wreck fact reveals the three.
            context.GetService<IMigrator>().Migrate(TooCloseForComfortLevelMigration);
            // MissionRewardItems is later still, so it rolls back too and leaves only the currency rows.
            AssertMissionRewardItems(connection, present: false);
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19854 AND purpose = 3 AND map_context_id = 1985 AND pos_x = -215 AND pos_y = 100 AND pos_z = -62"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985015 AND event = 3 AND mission_id = 1995 AND objective_id = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985017 AND event = 3 AND mission_id = 2005 AND objective_id = 1"));
            Assert.AreEqual(6L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id IN (1985015, 1985017) AND action = 14 AND location_id = 19854 AND placement_id IN (198680, 198681, 198682)"));
            context.GetService<IMigrator>().Migrate(BootcampLightningHitCreditMigration);
            // TooCloseForComfortLevel sets 1407 to TaRapedia's pre-shutdown level 4; its rollback restores 5.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1407 AND level = 5"));
            context.GetService<IMigrator>().Migrate(BootcampRetryReinforcementWalkMigration);
            // BootcampLightningHitCredit's rollback makes 1992/8 wait for a destroying hit again.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1992 AND objective_id = 8 AND binding_id = 0 AND destroying_hit_only = 1"));
            context.GetService<IMigrator>().Migrate(BootcampCourtyardForeanWarriorMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985017"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM content_rule_action WHERE rule_id = 1985015 AND location_id = 19854"));
            context.Database.Migrate();
            AssertMissionRewardItems(connection, present: true);
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1407 AND level = 4"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1992 AND objective_id = 8 AND binding_id = 0 AND destroying_hit_only = 0"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19854"));

            // BootcampObjectiveIndicators gives 1990's objectives the world markers the footage shows.
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 1990 AND indicator_id IN (430, 431)"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_indicator WHERE mission_id = 1990 AND objective_id = 1 AND pos_y = 131.3"));

            // WildernessHubReceptiveReception seeds the Alia Das chain's first mission: Solis gives it, Apirka ends it,
            // its first objective waits on the Enhance shrine, and "Too Close For Comfort" gates it.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 42 AND package_id = 168"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id = 43 AND package_id = 112"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1069 AND giver_id = 42 AND reciver_id = 43 AND level = 4"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1069 AND is_required = 1 AND ordinal BETWEEN 1 AND 3"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1069 AND objective_id = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1069"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1069 AND objective_id = 1 AND kind = 8 AND placement_id = 10"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 1069 AND or_group = 0 AND required_mission_id = 1407 AND required_state = 4"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1069 AND ((type = 3 AND credits = 4000) OR (type = 1 AND credits = 600))"));

            // Forming Alliances restores the early chain with a class-keyed collection
            // counter and a mission-only Thrax Soldier drop; each estimate is in its manifest.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 479 AND giver_id = 43 AND reciver_id = 43 AND level = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 479 AND objective_id = 1 AND is_required = 1 AND ordinal = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 479 AND required_mission_id = 1069 AND required_state = 4"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 427 AND required_mission_id = 479 AND required_state = 4"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id = 1390 AND required_mission_id = 479 AND required_state = 4"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN (1392,1393) AND required_mission_id = 1390 AND required_state = 4"));
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1392,1393) AND ((type = 3 AND credits = 8000) OR (type = 1 AND credits = 800))"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390 AND completed_objective_id = 2 AND revealed_objective_id = 8"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390 AND completed_objective_id = 8 AND revealed_objective_id = 11"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1390 AND ((completed_objective_id = 2 AND revealed_objective_id IN (4,10)) OR (completed_objective_id = 3 AND revealed_objective_id IN (8,10)))"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_counter WHERE mission_id = 479 AND objective_id = 1 AND counter_id = 0 AND target_value = 12"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 479 AND objective_id = 1 AND kind = 9 AND creature_id = 3 AND item_template_id = 2285 AND drop_chance = 50"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate_armor WHERE id = 13738 AND armor_value = 154"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 479 AND ((type = 3 AND credits = 4000) OR (type = 1 AND credits = 400) OR (type = 4 AND item_template_id = 13738 AND quantity = 1))"));

            // TooCloseForComfortLevel: TaRapedia's pre-shutdown page records 1407 at level 4.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1407 AND level = 4"));

            // WorldDefectsFix: Bagby and Galloway in Treeback Camp at TaRapedia's own x,y,z; the Foreas Base Warnet
            // Queen gone; Valerie Corman, Ranger Jorai and Lieutenant Epp at their readings with the navmesh floor;
            // the second Whitaker drawn by nothing; Liu's comment naming the hospital she stands in.
            const string WorldDefectsFixed =
                "SELECT (SELECT COUNT(*) FROM content_placement WHERE id = 199106 AND map_context_id = 1397 AND pos_x = -337.2 AND pos_y = 103.6 AND pos_z = 353.7)" +
                " + (SELECT COUNT(*) FROM content_placement WHERE id = 199107 AND map_context_id = 1397 AND pos_x = -122.2 AND pos_y = 100.3 AND pos_z = 128.2)" +
                " + (1 - (SELECT COUNT(*) FROM spawnpool WHERE id = 520046))" +
                " + (SELECT COUNT(*) FROM spawnpool WHERE id = 510137 AND map_context_id = 1244 AND pos_x = -772.7 AND pos_y = 140.17 AND pos_z = 774.9)" +
                " + (SELECT COUNT(*) FROM spawnpool WHERE id = 510133 AND map_context_id = 1244 AND pos_x = -294 AND pos_y = 172.707 AND pos_z = -577)" +
                " + (SELECT COUNT(*) FROM spawnpool WHERE id = 510068 AND map_context_id = 1761 AND pos_x = -180 AND pos_y = 235.229 AND pos_z = -244)" +
                " + (SELECT COUNT(*) FROM spawnpool WHERE id = 510085 AND creature_1_Id = 510085 AND creature_1_min_count = 0 AND creature_1_max_count = 0)" +
                " + (SELECT COUNT(*) FROM creature WHERE id = 199510 AND comment = 'Lieutenant Liu (Eir Crater Field Hospital)')" +
                " + (SELECT COUNT(*) FROM content_placement WHERE id = 199510 AND comment = 'Lieutenant Liu (Eir Crater Field Hospital) (TaRapedia /loc)')";
            Assert.AreEqual(9L, Scalar(connection, WorldDefectsFixed));
            // The creature and stats rows of the Warnet Queen stay; only the pool that drew it goes.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 520046 AND name_id = 461"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1788 AND reciver_id = 199106"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1789 AND reciver_id = 199107"));
            // Its rollback puts every row back exactly: the two officers where WorldFloorSweep left them on the
            // overworld, the pool re-inserted as Add_boss_spawns wrote it, the preloader's single-precision values.
            context.GetService<IMigrator>().Migrate(BootcampReinforcementPadHoldMigration);
            Assert.AreEqual(0L, Scalar(connection, WorldDefectsFixed));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199106 AND map_context_id = 1244 AND pos_x = -337.2 AND pos_y = 137.794 AND pos_z = 353.7"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 199107 AND map_context_id = 1244 AND pos_x = -122.2 AND pos_y = 140.442 AND pos_z = 128.2"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 520046 AND map_context_id = 1148 AND respown_time = 500 AND creature_1_Id = 520046 AND creature_1_min_count = 1 AND creature_1_max_count = 1 AND ABS(pos_x + 42.3) < 0.0001 AND ABS(pos_y - 116.3) < 0.0001 AND ABS(pos_z - 478.7) < 0.0001"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510137 AND pos_x = -515 AND ABS(pos_y - 141.62) < 0.0001 AND pos_z = 701.5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510133 AND ABS(pos_x + 235.1) < 0.0001 AND ABS(pos_y - 178.17) < 0.0001 AND ABS(pos_z + 543.8) < 0.0001"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510068 AND pos_x = -238.3 AND pos_y = 233.758 AND pos_z = -253.5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 510085 AND creature_1_min_count = 1 AND creature_1_max_count = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature c JOIN content_placement p ON p.id = c.id WHERE c.id = 199510 AND c.comment = 'Lieutenant Liu (Irendas Penal Colony)' AND p.comment = 'Lieutenant Liu (Irendas Penal Colony) (TaRapedia /loc)'"));
            context.Database.Migrate();
            Assert.AreEqual(9L, Scalar(connection, WorldDefectsFixed));
            // TordenConversationMissions: nine Torden conversation missions on NPCs the world already places (936 held,
            // its giver is off the outpost's navmesh). 1014 runs Provost (2) then Norton (1); 1014 and 1064 are gated on
            // the seeded 887 and 1063; 1014 has no reward row; 1326/1330 offer four inferred consumable choices each.
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 936"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 1745 AND giver_id = 199085 AND reciver_id = 199505 AND level = 15"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id = 526 AND giver_id = 510068 AND reciver_id = 510068 AND level = 35"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1326, 1330) AND level = 20"));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330) AND is_required = 1 AND ordinal IS NOT NULL"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1014 AND objective_id = 2 AND ordinal = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1014 AND objective_id = 1 AND ordinal = 2 AND revealed_on_accept = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1014 AND completed_objective_id = 2 AND revealed_objective_id = 1"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE (mission_id = 1014 AND required_mission_id = 887 OR mission_id = 1064 AND required_mission_id = 1063) AND required_state = 4"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1014, 936)"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id = 1064 AND ((type = 3 AND credits = 50000) OR (type = 1 AND credits = 5000))"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1326, 1330) AND type = 5 AND credits = 0 AND quantity IN (2, 4)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (526, 648, 802, 1064, 1070, 1745) AND type NOT IN (1, 3)"));
            // Rolled back, the missions and their rows go and the client skeleton objectives return with NULL flags.
            context.GetService<IMigrator>().Migrate(BootcampReinforcementPadHoldMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330)"));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN (1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330) AND ordinal IS NULL AND is_required IS NULL AND revealed_on_accept IS NULL"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1014 AND objective_id = 2 AND comment = 'Report to CID Headquarters.'"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE id IN (1745, 526, 648, 802, 1064, 1070, 1326, 1330)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id = 1014"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN (1014, 1064)"));
            context.Database.Migrate();
            Assert.AreEqual(9L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE id IN (1745, 526, 648, 802, 1014, 1064, 1070, 1326, 1330)"));
            // LiaisonLogosMissions: eleven Liaison Logos missions on the shrines the world places - Langerman (133) four at
            // his observed level 15, Standley (134) two, Noonan (199004) four, Arizpe (199085) one - each giver also its
            // receiver, one required objective bound by LogosRecovered to its logos row, 1639/1640 gated on 1069, and only
            // the reconciled TaRapedia amounts (six experience rows, two credit rows, no item).
            const string LiaisonLogos = "(1633, 1634, 1635, 1638, 1639, 1640, 1643, 1644, 1646, 1647, 1652)";
            Assert.AreEqual(11L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission WHERE id IN {LiaisonLogos} AND giver_id = reciver_id AND group_type = 1 AND category_id = 10000001 AND shareable = 0 AND radio_completeable = 0"));
            Assert.AreEqual(11L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission WHERE (id IN (1633, 1638, 1639, 1640) AND giver_id = 133 AND level = 15) OR (id IN (1634, 1635) AND giver_id = 134 AND level = 10) OR (id IN (1643, 1644, 1646, 1647) AND giver_id = 199004 AND level = 10) OR (id = 1652 AND giver_id = 199085 AND level = 25)"));
            Assert.AreEqual(11L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN {LiaisonLogos} AND is_required = 1 AND ordinal = 1 AND revealed_on_accept = 1"));
            Assert.AreEqual(11L, Scalar(connection,
                "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE binding_id = 0 AND kind = 8 AND counter_id = 255 AND area_id = 0 AND creature_id = 0 AND (" +
                "(mission_id = 1633 AND objective_id = 3 AND placement_id = 2) OR (mission_id = 1638 AND objective_id = 2 AND placement_id = 10) OR " +
                "(mission_id = 1639 AND objective_id = 6 AND placement_id = 23) OR (mission_id = 1640 AND objective_id = 4 AND placement_id = 1) OR " +
                "(mission_id = 1634 AND objective_id = 4 AND placement_id = 49) OR (mission_id = 1635 AND objective_id = 2 AND placement_id = 53) OR " +
                "(mission_id = 1643 AND objective_id = 7 AND placement_id = 3) OR (mission_id = 1644 AND objective_id = 8 AND placement_id = 7) OR " +
                "(mission_id = 1646 AND objective_id = 10 AND placement_id = 15) OR (mission_id = 1647 AND objective_id = 11 AND placement_id = 18) OR " +
                "(mission_id = 1652 AND objective_id = 16 AND placement_id = 46))"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_transition WHERE mission_id IN {LiaisonLogos}"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_conversation WHERE mission_id IN {LiaisonLogos}"));
            Assert.AreEqual(8L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id IN {LiaisonLogos}"));
            Assert.AreEqual(8L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_reward WHERE item_template_id = 0 AND quantity = 0 AND (" +
                "(type = 3 AND ((id = 1633 AND credits = 4000) OR (id IN (1638, 1639) AND credits = 2500) OR (id = 1640 AND credits = 3000) OR (id IN (1634, 1635) AND credits = 4500))) OR " +
                "(type = 1 AND ((id = 1635 AND credits = 900) OR (id = 1652 AND credits = 1800))))"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN (1639, 1640) AND or_group = 0 AND required_mission_id = 1069 AND required_state = 4"));
            Assert.AreEqual(2L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN {LiaisonLogos}"));
            // Its rollback takes every row back out and returns the client skeleton objectives with NULL flags.
            context.GetService<IMigrator>().Migrate(WorldDefectsFixMigration);
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission WHERE id IN {LiaisonLogos}"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_reward WHERE id IN {LiaisonLogos}"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id IN {LiaisonLogos}"));
            Assert.AreEqual(0L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_prerequisite WHERE mission_id IN {LiaisonLogos}"));
            Assert.AreEqual(11L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id IN {LiaisonLogos} AND is_required IS NULL AND ordinal IS NULL AND revealed_on_accept IS NULL AND comment LIKE 'Acquire Logos Information: %'"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective WHERE mission_id = 1652 AND objective_id = 16 AND comment = 'Acquire Logos Information: Ground'"));
            context.Database.Migrate();
            Assert.AreEqual(11L, Scalar(connection, $"SELECT COUNT(*) FROM npc_mission WHERE id IN {LiaisonLogos}"));

            // SolisCavernsPlacement binds the named Solis pool (184) to pool 92's original X/Z/rotation on the
            // probed floor, and empties pool 92 so the body is not duplicated. Inferred identity, not a retail row.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 184 AND pos_x = 786.8711 AND pos_y = 287.32 AND pos_z = 581.46875 AND rotation = 3 AND creature_1_Id = 42"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM spawnpool WHERE id = 92 AND creature_1_min_count = 0 AND creature_1_max_count = 0 AND creature_1_Id = 52"));

            // RecruitLoadoutFlags: the four new-character items are neither sellable nor tradable.
            Assert.AreEqual(4L, Scalar(connection, "SELECT COUNT(*) FROM itemtemplate WHERE id IN (122854, 122855, 122856, 122875) AND has_sellable_flag = 0 AND not_tradable_flag = 1"));

            // BootcampEquipCrateGear: Gearing Up's crate-gear step matches the crate set 19858, not the worn Recruit outfit.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM npc_mission_objective_binding WHERE mission_id = 1992 AND objective_id = 2 AND binding_id = 0 AND equip_match = 2 AND item_set_id = 19858"));

            // BootcampFirstLoginYaw: the measured 345 degree heading as original-client yaw 165 degrees.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_location WHERE id = 19851 AND rotation = 2.879793"));

            // BootcampConradCorpsePlacement and BootcampBombHullPlacement move the two S5 usables to reachable faces.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198676 AND pos_x = -98 AND pos_y = 85.39 AND pos_z = 67.2"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198677 AND pos_x = -221.95 AND pos_z = -70.5"));

            // BootcampDeSimoneCampPlacement moves the 1992 receiver / 1994 giver to the camp-pylon handoff area.
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198657 AND pos_x = 390.5 AND pos_y = 119.55 AND pos_z = 156"));

            // BootcampCampGunnerCompanion and BootcampCampArcherShamanCompanions: the three named level-3 Forean
            // Initiates at the camp pylon, combat companions (behavior 4) on 1994 while condition 198914 holds.
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (198516, 198517, 198518) AND faction = 1 AND level = 3 AND max_hp = 555 AND run_speed = 9 AND walk_speed = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198516 AND class_id = 6239 AND name_id = 7938 AND action1 = 27"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198517 AND class_id = 7036 AND name_id = 7986 AND action1 = 28"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198518 AND class_id = 7035 AND name_id = 7890 AND action1 = 46"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature_action WHERE id = 46 AND action_id = 1 AND action_arg_id = 146 AND cooldown = 2500 AND min_damage = 15 AND max_damage = 28"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE slot_id = 13 AND ((id = 198516 AND class_id = 6238) OR (id = 198517 AND class_id = 10529) OR (id = 198518 AND class_id = 10533))"));
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_condition WHERE condition_id = 198914 AND kind = 2 AND mission_id = 1994 AND state = 1"));
            Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198688, 198689, 198690) AND map_context_id = 1985 AND behavior = 4 AND escort_mission_id = 1994 AND present_condition_id = 198914"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198688 AND creature_id = 198516 AND pos_x = 385.2 AND pos_y = 119.5 AND pos_z = 152.3"));

            // Add_map_region (upstream PR #91) creates the region table and preloads 373 volumes on 58 maps:
            // the Wilderness ones (context 1220) cover Alia Das and the caverns.
            Assert.AreEqual(373L, Scalar(connection, "SELECT COUNT(*) FROM map_region"));
            Assert.AreEqual(21L, Scalar(connection, "SELECT COUNT(*) FROM map_region WHERE map_context_id = 1220"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM map_region WHERE map_context_id = 1220 AND region_id = 18 AND shape = 1 AND underground = 2 AND enabled = 1"));

            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_item_set WHERE initial_ammo <> 0 AND item_set_id = 19858 AND item_template_id = 13713 AND initial_ammo = 20"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_item_set WHERE initial_ammo <> 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198652 AND hit_points = 1 AND restore_ms = 930"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id = 198653 AND hit_points = 100 AND restore_ms = 930"));
            context.GetService<IMigrator>().Migrate(BootcampCrateLoadedRifleMigration);
            Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (198652, 198653) AND hit_points = 100 AND restore_ms = 930"));
            context.GetService<IMigrator>().Migrate(ContentItemSetInitialAmmoMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_item_set WHERE initial_ammo <> 0"));
            context.GetService<IMigrator>().Migrate(BootcampMcAllisterWalkMigration);
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM pragma_table_info('content_item_set') WHERE name = 'initial_ammo'"));
            context.Database.Migrate();

            // Execute the new schema/data rollback before the older content rollbacks.
            context.GetService<IMigrator>().Migrate(BootcampEquipCrateGearMigration);
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 38 AND class_id = 6163"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 132 AND class_id = 29423"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature_appearance WHERE id = 132"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198500 AND walk_speed = 0 AND run_speed = 0"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM content_rule WHERE id = 1985014 AND event = 6 AND mission_id = 1990"));
            // Unlike single-precision FLOAT, DOUBLE retains even the largest former uint.
            context.Database.ExecuteSqlRaw("UPDATE creature SET run_speed = 4294967295 WHERE id = 198500");
            context.Database.Migrate();
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id = 198500 AND walk_speed = 2.5 AND run_speed = 4294967295"));
            context.Database.ExecuteSqlRaw("UPDATE creature SET run_speed = 0 WHERE id = 198500");

            // Rolling W2 back removes every class-gear row and restores the NULL-flag objective skeleton it replaced.
            context.GetService<IMigrator>().Migrate(WildernessArrivalTrainingDayMigration);
            // Rolled this far back, everything this batch created is gone. The missions themselves are not checked
            // here: 422 and 429 are seeded by WildernessGiverFix, which this rollback undoes as well, so the table does
            // not hold them at this depth. Their values in the forward direction are asserted above, where the loader's
            // own requirements live.
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM creature WHERE id IN (199910, 199911)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM npc_package WHERE id IN (199910, 199911)"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM content_placement WHERE id IN (199910, 199911)"));

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
                new[] { BootcampS1Migration, ObjectiveColumnsMigration, ObjectiveSkeletonMigration, BootcampS2Migration, BootcampFixNpcAppearanceMigration, KraftwerksMigration, BootcampS3Migration, BootcampS4Migration, BootcampS5Migration, BootcampS6Migration, BootcampFixRogersTurnInMigration, WildernessArrivalTrainingDayMigration, WildernessClassGearMigration, AddMapRegionMigration, BootcampAreaVerticalExtentMigration, WildernessHubReceptiveReceptionMigration, BootcampObjectiveIndicatorsMigration, BootcampScriptedMovesMigration, ContentRuleActionDamageMigration, BootcampDetonationDamageMigration, BootcampObjectiveAreaRadiusMigration, BootcampCaveInTriggerRadiusMigration, BootcampObjectiveAreaHeightMigration, BootcampRemainingTriggerHeightMigration, BootcampPlacementGroundSnapMigration, BootcampPlatformTopCorrectionMigration, BootcampEscortDestinationGroundMigration, WildernessHubConscientiousObjectorMigration, WildernessHubConscientiousObjectorPathMigration, WildernessHubConversationChainMigration, WildernessHubConversationChainRewardsMigration, WildernessHubKillObjectiveMigration, DivideConversationNpcMigration, PalisadesConversationNpcMigration, PlateauConversationNpcMigration, MarshesConversationNpcMigration, MiresConversationNpcMigration, PlainsConversationNpcMigration, AddActionsMigration, InclineConversationNpcMigration, TordenNpcGroundSnapMigration, WildernessMortarByNumbersMigration, AddCreatureClassFlagMigration, WildernessCollectionDropMigration, WildernessGiverFixMigration, MiresReconstructedSpeciesMigration, AddCreatureLootMigration, ContentPlacementEscortMigration, WildernessEscortMilpasMigration, MissionAreaLinksMigration, WildernessPinholeNpcMigration, WorldPlacementFloorSnapMigration, WildernessDialogueBindingMigration, BootcampTargetDummyLaneMigration, EntityClassClientFidelityMigration, BootcampCrateUncommonGearMigration, WildernessMortarCreatureMigration, WildernessMortarFireMigration, DevilsDenFransiscoMigration, BootcampTargetDummyFrontMigration, ContentNpcAppearanceMigration, QuestNpcDialogueBatchMigration, TarapediaGiverAuditMigration, TarapediaMissingNpcsMigration, TarapediaMachineClassMigration, BootcampRetryIndicatorsMigration, AddMapMarkerMigration, AddRecipeMigration, RegenerateItemTemplateMigration, AddServiceNpcsMigration, AddMissionNpcsMigration, AddClassTrainersMigration, AddBossSpawnsMigration, PlaceDropshipPadsMigration, FixLogosShrinesMigration, PlaceRemainingLogosMigration, AddSkillCharacterMigration, RetuneWeaponToolTypeMigration, EllathaNpcPositionsMigration, EllathaWorldNpcsMigration, WorldFloorSweepMigration, QuestGiverBriefingFixMigration, PropOverlapFixMigration, TarapediaNpcPositionsMigration, WorldSweepCorrectionsMigration, TarapediaMissingNpcBatchMigration, PhostBenonFloorMigration, TarapediaLastNpcsMigration, QuestWiringFixesMigration, CodexPlacementFixesMigration, MissionSpeakersMigration, AppearanceSlotRepairMigration, SeedLogosMissionsMigration, MissionPropSpeakersMigration, WeaponRowRepairMigration, ContentRuleBarkMigration, ClientNamesForItemsMigration, CodexNpcCorrectionsMigration, RecruitLoadoutFlagsMigration, BootcampEquipCrateGearMigration, CreatureFractionalMovementRatesMigration, BootcampMcAllisterWalkMigration, ContentItemSetInitialAmmoMigration, BootcampCrateLoadedRifleMigration, BootcampPracticeDummyHealthMigration, BootcampFirstLoginYawMigration, BootcampCrateRifleRangeMigration, MoawiDialogueClassMigration, MissionSpeakerDialogueClassesMigration, BootcampRifleMeleeMigration, BootcampConradCorpsePlacementMigration, BootcampBombHullPlacementMigration, WildernessHubReceptiveGateMigration, WildernessHubReceptiveLevelMigration, MissionItemDropChanceMigration, WildernessHubFormingAlliancesMigration, WildernessHubConscientiousGateMigration, WildernessHubConscientiousBranchesMigration, SolisCavernsPlacementMigration, BootcampDeSimoneCampPlacementMigration, BootcampCampGunnerCompanionMigration, BootcampCampArcherShamanCompanionsMigration, BootcampCourtyardForeanWarriorMigration, BootcampRetryReinforcementWalkMigration, BootcampLightningHitCreditMigration, TooCloseForComfortLevelMigration, BootcampReinforcementPadHoldMigration, EarlyReadyMissionsMigration, TordenConversationMissionsMigration, MissionRewardItemsMigration, WorldDefectsFixMigration, LiaisonLogosMissionsMigration },
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
                // AccountRaceUnlocks: one (account, race) row per earned hybrid; BackfillRaceUnlocks found no
                // completed 1861/1851/1899 here, so it grants nothing (RaceUnlockTests covers a positive backfill).
                Assert.IsTrue(TableExists(connection, "account_race_unlock"));
                Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM account_race_unlock"));
                Assert.AreEqual(2L, Scalar(connection, "SELECT COUNT(*) FROM pragma_table_info('account_race_unlock') WHERE name IN ('account_id', 'race_id') AND pk > 0"));
                // ItemInstanceMetadata: three nullable columns, so existing items keep their template defaults.
                Assert.AreEqual(3L, Scalar(connection, "SELECT COUNT(*) FROM pragma_table_info('items') WHERE name IN ('loot_modules', 'tradable_override', 'sellable_override') AND \"notnull\" = 0"));
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
            Assert.IsFalse(TableExists(connection, "account_race_unlock"));
            Assert.AreEqual(0L, Scalar(connection, "SELECT COUNT(*) FROM pragma_table_info('items') WHERE name IN ('loot_modules', 'tradable_override', 'sellable_override')"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM character_mission WHERE character_id = 101 AND mission_id = 429 AND change_time = 5"));
            Assert.AreEqual(1L, Scalar(connection, "SELECT COUNT(*) FROM character_mission_objective WHERE character_id = 101 AND mission_id = 429 AND status = 1"));
        }
    }
}
