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
    public partial class MissionContentManager
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
            Load(bootcamp, new LoadedContentReferences(_gameUnitOfWorkFactory), MissionManager.Instance.LoadedMissions,
                null, LoadIndicatorWorld());
        }

        /// <summary>
        /// The world rows the objective markers are read from: the shrines, the spawn pools and the npc_package
        /// bindings the content catalog does not carry, plus the catalog's own areas and placements. Read once,
        /// at load, from the world database.
        ///
        /// Areas and placements are taken as they are rather than filtered to the live ones: a binding that
        /// references a withheld row makes its mission a gap, and BuildRuntime attaches only live bindings, so a
        /// withheld area or placement is never reachable from an objective in the first place.
        /// </summary>
        private Structures.Content.MissionIndicatorWorld LoadIndicatorWorld()
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateWorld();

            return Structures.Content.MissionIndicatorWorld.Build(
                unitOfWork.MissionContent.GetAreas(),
                unitOfWork.MissionContent.GetPlacements(),
                unitOfWork.Logoses.GetLogos(),
                unitOfWork.Spawnpools.Get(),
                unitOfWork.NpcPackages.Get());
        }

        /// <param name="indicatorWorld">
        /// Where the derived objective markers are read from, or null to derive none. The server passes the world
        /// database; a test passes the rows it is about.
        /// </param>
        public void Load(Func<BootcampConfig> bootcamp, IContentReferences references, IReadOnlyDictionary<uint, Structures.Mission> missions,
            ContentCapabilities capabilities = null, Structures.Content.MissionIndicatorWorld indicatorWorld = null)
        {
            _bootcamp = bootcamp ?? (() => null);

            MissionContentCatalog catalog;
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateWorld())
                catalog = MissionContentCatalog.Load(unitOfWork.MissionContent);

            // Offerability of a mission is judged on its own definition, not on content gaps of a previous load.
            foreach (var mission in missions.Values)
                mission.ContentGaps.Clear();

            // The catalog judges a mission offerable through DefinitionGaps, which counts
            // completion bindings; attach the loaded bindings before validating, so a
            // definition complete except for its content bindings is not falsely withheld.
            // BuildRuntime re-attaches the live subset after validation.
            foreach (var mission in missions.Values)
                mission.Bindings.Clear();
            foreach (var binding in catalog.Bindings)
                if (missions.TryGetValue(binding.MissionId, out var mission))
                    mission.Bindings.Add(binding);

            Content = catalog.Validate(references, capabilities);
            BuildRuntime(missions);

            // Prerequisites gate each player's offer at dispense time (per-player state),
            // but the loader must know them; attach the live ones to their definitions.
            foreach (var mission in missions.Values)
            {
                mission.Prerequisites.Clear();
                mission.Timers.Clear();
                mission.Counters.Clear();
                mission.Indicators.Clear();
                mission.DerivedIndicators.Clear();
            }

            foreach (var prerequisite in Content.LivePrerequisites)
                if (missions.TryGetValue(prerequisite.MissionId, out var mission))
                    mission.Prerequisites.Add(prerequisite);

            foreach (var timer in Content.LiveTimers)
                if (missions.TryGetValue(timer.MissionId, out var mission))
                    mission.Timers[timer.ObjectiveId] = timer;

            foreach (var counter in Content.LiveCounters.OrderBy(counter => counter.CounterId))
                if (missions.TryGetValue(counter.MissionId, out var mission))
                {
                    if (!mission.Counters.TryGetValue(counter.ObjectiveId, out var counters))
                        mission.Counters[counter.ObjectiveId] = counters = new List<Rasa.Structures.World.NpcMissionObjectiveCounterEntry>();
                    counters.Add(counter);
                }

            foreach (var indicator in Content.LiveIndicators)
                if (missions.TryGetValue(indicator.MissionId, out var mission))
                {
                    if (!mission.Indicators.TryGetValue(indicator.ObjectiveId, out var indicators))
                        mission.Indicators[indicator.ObjectiveId] = indicators = new List<Rasa.Structures.World.NpcMissionObjectiveIndicatorEntry>();
                    indicators.Add(indicator);
                }

            // The markers the world can be read for: one per objective completion route with a sourced position.
            // Bindings are attached by now (BuildRuntime above), which is what a binding-bound objective is read
            // from. Objectives that already carry a stored row keep it; the derived list is the fallback
            // (PlayerMission.IndicatorList).
            if (indicatorWorld != null)
            {
                var derivedObjectives = 0;
                var derivedMarkers = 0;
                var bySource = new SortedDictionary<Structures.Content.MissionIndicatorSource, int>();

                foreach (var mission in missions.Values)
                {
                    foreach (var (objectiveId, markers) in Structures.Content.MissionMapIndicators.Derive(mission, indicatorWorld))
                    {
                        if (mission.Indicators.ContainsKey(objectiveId))
                            continue;

                        mission.DerivedIndicators[objectiveId] = markers;
                        derivedObjectives++;
                        derivedMarkers += markers.Count;

                        foreach (var marker in markers)
                            bySource[marker.Source] = bySource.TryGetValue(marker.Source, out var count) ? count + 1 : 1;
                    }
                }

                var breakdown = string.Join(", ", bySource.Select(entry => $"{entry.Key} {entry.Value}"));
                Logger.WriteLog(LogType.Initialize,
                    $"Derived {derivedMarkers} objective map markers for {derivedObjectives} objectives ({breakdown})");
            }

            // A shared context holds its placements for the life of the server; per-character channels
            // materialize their own when they are created (a later slice).
            foreach (var mapChannel in MapChannelManager.Instance.MapChannelArray.Values)
                if (Content.Catalog.InstancingFor(mapChannel.MapInfo?.MapContextId ?? 0) == MapInstancing.Shared)
                    ContentMaterializer.Materialize(mapChannel, Content);

            foreach (var gap in Content.Gaps)
                Logger.WriteLog(LogType.Initialize, $"Content withheld: {gap}");

            foreach (var (missionId, gaps) in Content.MissionGaps)
            {
                if (!missions.TryGetValue(missionId, out var mission))
                    continue;

                mission.ContentGaps.AddRange(gaps);
                Logger.WriteLog(LogType.Initialize, $"Mission {missionId} is not offered, content withheld: {string.Join("; ", gaps)}");
            }

            // Final offerability report: DefinitionGaps minus the content gaps already
            // logged above. Bindings are attached by now, so this is the mission's
            // actual state for the rest of the server's life.
            foreach (var mission in missions.Values)
            {
                var definitionGaps = mission.DefinitionGaps().Where(gap => !mission.ContentGaps.Contains(gap)).ToList();
                if (definitionGaps.Count > 0)
                    Logger.WriteLog(LogType.Initialize, $"Mission {mission.MissionId} is not offered, definition incomplete: {string.Join("; ", definitionGaps)}");
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
