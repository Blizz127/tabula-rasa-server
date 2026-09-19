using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Rasa.Context.World
{
    using Configuration;
    using Configuration.ContextSetup;
    using Extensions;
    using Services.DbContext;
    using Structures.World;

    public abstract class WorldContext : RasaDbContextBase
    {
        private readonly IOptions<DatabaseConfiguration> _databaseConfiguration;
        private readonly IDbContextPropertyModifier _dbContextPropertyModifier;

        protected WorldContext(IOptions<DatabaseConfiguration> databaseConfiguration,
            IDbContextConfigurationService dbContextConfigurationService,
            IDbContextPropertyModifier dbContextPropertyModifier)
            : base(databaseConfiguration, dbContextConfigurationService)
        {
            _databaseConfiguration = databaseConfiguration;
            _dbContextPropertyModifier = dbContextPropertyModifier;
        }
        public DbSet<ActionEntry> ActionEntries { get; set; }
        public DbSet<ActionLevelEntry> ActionLevelEntries { get; set; }
        public DbSet<ActionCostEntry> ActionCostEntries { get; set; }
        public DbSet<ActionPropertyEntry> ActionPropertyEntries { get; set; }
        public DbSet<ActionItemRequirementEntry> ActionItemRequirementEntries { get; set; }
        public DbSet<ArmorClassEntry> ArmorClassEntries { get; set; }
        public DbSet<ContentAreaEntry> ContentAreaEntries { get; set; }
        public DbSet<ContentConditionEntry> ContentConditionEntries { get; set; }
        public DbSet<ContentItemSetEntry> ContentItemSetEntries { get; set; }
        public DbSet<ContentLocationEntry> ContentLocationEntries { get; set; }
        public DbSet<ContentMapSettingEntry> ContentMapSettingEntries { get; set; }
        public DbSet<ContentPlacementEntry> ContentPlacementEntries { get; set; }
        public DbSet<ContentRuleEntry> ContentRuleEntries { get; set; }
        public DbSet<ContentRuleActionEntry> ContentRuleActionEntries { get; set; }
        public DbSet<CreatureEntry> CreatureEntries { get; set; }
        public DbSet<CreatureActionEntry> CreatureActionEntries { get; set; }
        public DbSet<CreatureAppearanceEntry> CreatureAppearanceEntries { get; set; }
        public DbSet<CreatureStatEntry> CreatureStatEntries { get; set; }
        public DbSet<CreatureClassFlagEntry> CreatureClassFlagEntries { get; set; }
        public DbSet<ExperienceForLevelEntry> ExperienceForLevelEntries { get; set; }
        public DbSet<EntityClassEntry> EntityClassEntries { get; set; }
        public DbSet<EquipableClassEntry> EquipableClassEntries { get; set; }
        public DbSet<FootlockerEntry> FootlockerEntries { get; set; }
        public DbSet<ItemClassEntry> ItemClassEntries { get; set; }
        public DbSet<ItemTemplateEntry> ItemTemplateEntries { get; set; }
        public DbSet<ItemTemplateActionEntry> ItemTemplateActionEntries { get; set; }
        public DbSet<ItemTemplateArmorEntry> ItemTemplateArmorEntries { get; set; }
        public DbSet<ItemTemplateItemClassEntry> ItemTemplateItemClassEntries { get; set; }
        public DbSet<ItemTemplateRequirementEntry> ItemTemplateRequirementEntries { get; set; }
        public DbSet<ItemTemplateRequirementRaceEntry> ItemTemplateRequirementRaceEntries { get; set; }
        public DbSet<ItemTemplateRequirementSkillEntry> ItemTemplateRequirementSkillEntries { get; set; }
        public DbSet<ItemTemplateResistanceEntry> ItemTemplateResistanceEntries { get; set; }
        public DbSet<ItemTemplateWeaponEntry> ItemTemplateWeaponEntries { get; set; }
        public DbSet<LogosEntry> LogosEntries { get; set; }
        public DbSet<MapInfoEntry> MapInfoEntries { get; set; }
        public DbSet<MapLinkEntry> MapLinkEntries { get; set; }
        public DbSet<KraftwerksEntry> KraftwerksEntries { get; set; }
        public DbSet<MapRegionEntry> MapRegionEntries { get; set; }
        public DbSet<CreatureLootEntry> CreatureLootEntries { get; set; }
        public DbSet<NpcMissionEntry> NpcMissionEntries { get; set; }
        public DbSet<NpcMissionObjectiveEntry> NpcMissionObjectiveEntries { get; set; }
        public DbSet<NpcMissionObjectiveBindingEntry> NpcMissionObjectiveBindingEntries { get; set; }
        public DbSet<NpcMissionObjectiveCounterEntry> NpcMissionObjectiveCounterEntries { get; set; }
        public DbSet<NpcMissionObjectiveIndicatorEntry> NpcMissionObjectiveIndicatorEntries { get; set; }
        public DbSet<NpcMissionObjectiveTimerEntry> NpcMissionObjectiveTimerEntries { get; set; }
        public DbSet<NpcMissionPrerequisiteEntry> NpcMissionPrerequisiteEntries { get; set; }
        public DbSet<NpcMissionObjectiveConversationEntry> NpcMissionObjectiveConversationEntries { get; set; }
        public DbSet<NpcMissionObjectiveTransitionEntry> NpcMissionObjectiveTransitionEntries { get; set; }
        public DbSet<NpcMissionRewardEntry> NpcMissionRewardEntries { get; set; }
        public DbSet<NpcPackageEntry> NpcPackageEntries { get; set; }
        public DbSet<RandomNameEntry> RandomNameEntries { get; set; }
        public DbSet<SpawnPoolEntry> SpawnPoolEntries { get; set; }
        public DbSet<TeleporterEntry> TeleporterEntries { get; set; }
        public DbSet<VendorEntry> VendorEntries { get; set; }
        public DbSet<VendorItemEntry> VendorItemEntries { get; set; }
        public DbSet<WeaponClassEntry> WeaponClassEntries { get; set; }

        protected override DatabaseConnectionConfiguration GetDatabaseConnectionConfiguration()
        {
            return _databaseConfiguration.Value.World;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            SetupExperienceForLevel(modelBuilder);
            SetupRandomName(modelBuilder);
            SetupItemTemplateItemClass(modelBuilder);
            SetupNpcMissionObjectives(modelBuilder);
            SetupMissionContent(modelBuilder);
            SetupCreatureClassFlag(modelBuilder);
        }

        private void SetupMissionContent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NpcMissionPrerequisiteEntry>()
                .HasKey(e => new { e.MissionId, e.OrGroup, e.RequiredMissionId });
            modelBuilder.Entity<NpcMissionObjectiveBindingEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId, e.BindingId });
            modelBuilder.Entity<NpcMissionObjectiveCounterEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId, e.CounterId });
            modelBuilder.Entity<NpcMissionObjectiveTimerEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId });
            modelBuilder.Entity<NpcMissionObjectiveIndicatorEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId, e.IndicatorIndex });
            modelBuilder.Entity<ContentConditionEntry>()
                .HasKey(e => new { e.ConditionId, e.OrGroup, e.TermIndex });
            modelBuilder.Entity<ContentRuleActionEntry>()
                .HasKey(e => new { e.RuleId, e.Sequence });
            modelBuilder.Entity<ContentItemSetEntry>()
                .HasKey(e => new { e.ItemSetId, e.ItemTemplateId });
        }

        private void SetupNpcMissionObjectives(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NpcMissionObjectiveEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId });
            modelBuilder.Entity<NpcMissionObjectiveConversationEntry>()
                .HasKey(e => new { e.MissionId, e.ObjectiveId, e.NpcPackageId, e.PlayerFlagId, e.ConvoType });
            modelBuilder.Entity<NpcMissionObjectiveTransitionEntry>()
                .HasKey(e => new { e.MissionId, e.CompletedObjectiveId, e.RevealedObjectiveId });
        }

        /// <summary>
        /// A class carries several flags, so the row is the pair. Composite keys cannot be
        /// declared with [Key] attributes - EF refuses the model outright - so it is set here.
        /// </summary>
        private static void SetupCreatureClassFlag(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CreatureClassFlagEntry>()
                .HasKey(e => new { e.ClassId, e.FlagId });
        }

        private void SetupExperienceForLevel(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Level)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ExperienceForLevelEntry>()
                .Property(e => e.Experience)
                .AsUnsignedBigInt(_dbContextPropertyModifier, 20);
        }

        private void SetupRandomName(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Type)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>()
                .Property(e => e.Gender)
                .AsUnsignedTinyInt(_dbContextPropertyModifier, 3);

            modelBuilder.Entity<RandomNameEntry>().HasKey(e => new { e.Name, e.Type, e.Gender });
        }

        private void SetupItemTemplateItemClass(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemTemplateId)
                .AsIdColumn(_dbContextPropertyModifier)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);

            modelBuilder.Entity<ItemTemplateItemClassEntry>()
                .Property(e => e.ItemClass)
                .AsUnsignedInt(_dbContextPropertyModifier, 11);
        }
    }
}
