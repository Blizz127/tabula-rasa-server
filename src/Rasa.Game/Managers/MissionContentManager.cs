using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Config;
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures.Content;

    /// <summary>
    /// Loads and validates the reconstructed-content layer (areas, placements, rules and the
    /// mission rows that bind objectives to them). Content with a gap is withheld; a context or
    /// mission without content rows keeps today's behaviour.
    /// </summary>
    public class MissionContentManager
    {
        private static MissionContentManager _instance;
        private static readonly object InstanceLock = new object();
        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;

        public static MissionContentManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new MissionContentManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        public MissionContentManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
        }

        public ContentValidation Content { get; private set; } = MissionContentCatalog.Empty.Validate(new NoContentReferences());

        private Func<BootcampConfig> _bootcamp = () => null;

        // Read on every use, so the switch always reflects the server's current configuration.
        public BootcampConfig Bootcamp => _bootcamp() ?? new BootcampConfig();

        internal void Load(Func<BootcampConfig> bootcamp)
        {
            Load(bootcamp, new LoadedContentReferences(_gameUnitOfWorkFactory), MissionManager.Instance.LoadedMissions);
        }

        public void Load(Func<BootcampConfig> bootcamp, IContentReferences references, IReadOnlyDictionary<uint, Structures.Mission> missions,
            ContentCapabilities capabilities = null)
        {
            _bootcamp = bootcamp ?? (() => null);

            MissionContentCatalog catalog;
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateWorld())
                catalog = MissionContentCatalog.Load(unitOfWork.MissionContent);

            // Offerability of a mission is judged on its own definition, not on content gaps of a previous load.
            foreach (var mission in missions.Values)
                mission.ContentGaps.Clear();

            Content = catalog.Validate(references, capabilities);

            foreach (var gap in Content.Gaps)
                Logger.WriteLog(LogType.Initialize, $"Content withheld: {gap}");

            foreach (var (missionId, gaps) in Content.MissionGaps)
            {
                if (!missions.TryGetValue(missionId, out var mission))
                    continue;

                mission.ContentGaps.AddRange(gaps);
                Logger.WriteLog(LogType.Initialize, $"Mission {missionId} is not offered, content withheld: {string.Join("; ", gaps)}");
            }

            Logger.WriteLog(LogType.Initialize, $"Loaded {Content.LiveRules.Count()} content rules ({catalog.RowCount} content rows, {Content.Gaps.Count} gaps)");
            Logger.WriteLog(LogType.Initialize, $"Boot camp entry: {Bootcamp.EntryMode}");
        }

        /// <summary>
        /// Where a new character of this account starts, or null for the default Wilderness start.
        /// </summary>
        public Structures.World.ContentLocationEntry NewCharacterStart(uint accountId)
        {
            return BootcampEntryGate.StartLocation(Bootcamp, accountId, Content);
        }

        private sealed class NoContentReferences : IContentReferences
        {
            public bool MapContextExists(uint mapContextId) => false;
            public bool MissionExists(uint missionId) => false;
            public bool ObjectiveExists(uint missionId, uint objectiveId) => false;
            public uint MissionGiver(uint missionId) => 0;
            public bool MissionOfferable(uint missionId) => false;
            public bool CreatureExists(uint creatureId) => false;
            public bool EntityClassExists(uint entityClassId) => false;
            public bool ItemTemplateExists(uint itemTemplateId) => false;
            public bool LogosExists(uint logosId) => false;
            public bool HasLegacyWorldObjects(uint mapContextId) => false;
        }

        /// <summary>
        /// References resolved against the data the other managers loaded before content.
        /// </summary>
        private sealed class LoadedContentReferences : IContentReferences
        {
            private readonly Dictionary<uint, uint> _logosContexts;

            public LoadedContentReferences(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
            {
                using var unitOfWork = gameUnitOfWorkFactory.CreateWorld();
                _logosContexts = unitOfWork.Logoses.GetLogos().ToDictionary(logos => logos.Id, logos => logos.MapContextId);
            }

            public bool MapContextExists(uint mapContextId) => MapChannelManager.Instance.MapChannelArray.ContainsKey(mapContextId);

            public bool MissionExists(uint missionId) => MissionManager.Instance.LoadedMissions.ContainsKey(missionId);

            public bool ObjectiveExists(uint missionId, uint objectiveId) =>
                MissionManager.Instance.LoadedMissions.TryGetValue(missionId, out var mission) && mission.Objectives.ContainsKey(objectiveId);

            public uint MissionGiver(uint missionId) =>
                MissionManager.Instance.LoadedMissions.TryGetValue(missionId, out var mission) ? mission.MissionGiver : 0;

            public bool MissionOfferable(uint missionId) =>
                MissionManager.Instance.LoadedMissions.TryGetValue(missionId, out var mission) && mission.IsDispensable;

            public bool CreatureExists(uint creatureId) => CreatureManager.Instance.LoadedCreatures.ContainsKey(creatureId);

            public bool EntityClassExists(uint entityClassId) =>
                EntityClassManager.Instance.LoadedEntityClasses.ContainsKey((EntityClasses)entityClassId);

            public bool ItemTemplateExists(uint itemTemplateId) =>
                ItemManager.Instance.ItemTemplateItemClass.TryGetValue(itemTemplateId, out var classId) &&
                EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(classId, out var entityClass) &&
                entityClass.ItemTemplates.ContainsKey(itemTemplateId);

            public bool LogosExists(uint logosId) => _logosContexts.ContainsKey(logosId);

            public bool HasLegacyWorldObjects(uint mapContextId) =>
                SpawnPoolManager.Instance.LoadedSpawnPools.Values.Any(pool => pool.MapContextId == mapContextId) ||
                _logosContexts.Values.Contains(mapContextId) ||
                (MapChannelManager.Instance.MapChannelArray.TryGetValue(mapContextId, out var mapChannel) && mapChannel.FootLockers.Count > 0);
        }
    }

    public static class BootcampEntryGate
    {
        /// <summary>
        /// The boot-camp start for a new character, or null when the character starts at the default
        /// Wilderness position: entry disabled, account not allow-listed, entry not implemented, or no
        /// live start location.
        /// </summary>
        public static Structures.World.ContentLocationEntry StartLocation(BootcampConfig config, uint accountId, ContentValidation content)
            => StartLocation(config, accountId, content, MissionContentRules.BootcampEntryImplemented);

        public static Structures.World.ContentLocationEntry StartLocation(BootcampConfig config, uint accountId, ContentValidation content, bool entryImplemented)
        {
            if (config == null || content == null)
                return null;

            switch (config.EntryMode)
            {
                case BootcampEntryMode.AllNewCharacters:
                    break;
                case BootcampEntryMode.AllowListedAccounts:
                    if (config.AccountIds == null || !config.AccountIds.Contains(accountId))
                        return null;
                    break;
                default:
                    return null;
            }

            if (!entryImplemented)
                return null;

            return content.Catalog.Locations.Values
                .Where(location => (ContentLocationPurpose)location.Purpose == ContentLocationPurpose.NewCharacterStart &&
                                   !content.WithheldLocations.Contains(location.Id))
                .OrderBy(location => location.Id)
                .FirstOrDefault();
        }
    }
}
