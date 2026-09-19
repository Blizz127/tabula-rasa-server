using System.Diagnostics.CodeAnalysis;

namespace Rasa.Repositories.World
{
    using Context.World;
    using MissionContent;
    using UnitOfWork;

    public class WorldUnitOfWork : UnitOfWork, IWorldUnitOfWork
    {
        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Required for DI")]
        public WorldUnitOfWork(WorldContext dbContext,
            IActionRepository actionRepository,
            IEquipmentRepository equipmentRepository,
            ICreatureRepository creatureRepository,
            IEntityClassRepository entityClassRepository,
            IFootlockerRepository footlockerRepository,
            ILogosRepository logosRepository,
            IMapInfoRepository mapInfoRepository,
            IMissionContentRepository missionContentRepository,
            IMapLinkRepository mapLinkRepository,
            IKraftwerksRepository kraftwerksRepository,
            IMapRegionRepository mapRegionRepository,
            INpcMissionRepository npcMissionRepository,
            INpcMissionObjectiveRepository npcMissionObjectiveRepository,
            INpcMissionRewardRepository npcMissionRewardRepository,
            INpcPackageRepository npcPackageRepository,
            IPlayerRandomNameRepository randomNameRepository,
            ISpawnpoolRepository spawnpoolRepository,
            ITeleporterRepository teleporterRepository)
                : base(dbContext)
        {
            Actions = actionRepository;
            Equipment = equipmentRepository;
            Creatures = creatureRepository;
            EntityClasses = entityClassRepository;
            Footlockers = footlockerRepository;
            Logoses = logosRepository;
            MapInfos = mapInfoRepository;
            MissionContent = missionContentRepository;
            MapLinks = mapLinkRepository;
            Kraftwerks = kraftwerksRepository;
            MapRegions = mapRegionRepository;
            NpcMissions = npcMissionRepository;
            NpcMissionObjectives = npcMissionObjectiveRepository;
            NpcMissionRewards = npcMissionRewardRepository;
            NpcPackages = npcPackageRepository;
            RandomNames = randomNameRepository;
            Spawnpools = spawnpoolRepository;
            Teleporters = teleporterRepository;
        }

        public IActionRepository Actions { get; }
        public IEquipmentRepository Equipment { get; }
        public ICreatureRepository Creatures { get; }
        public IEntityClassRepository EntityClasses { get; }
        public IFootlockerRepository Footlockers { get; }
        public ILogosRepository Logoses { get; }
        public IMapInfoRepository MapInfos { get; }
        public IMissionContentRepository MissionContent { get; }
        public IMapLinkRepository MapLinks { get; }
        public IKraftwerksRepository Kraftwerks { get; }
        public IMapRegionRepository MapRegions { get; }
        public INpcMissionRepository NpcMissions { get; }
        public INpcMissionObjectiveRepository NpcMissionObjectives { get; }
        public INpcMissionRewardRepository NpcMissionRewards { get; }
        public INpcPackageRepository NpcPackages { get; }
        public IPlayerRandomNameRepository RandomNames { get; }
        public ISpawnpoolRepository Spawnpools { get; }
        public ITeleporterRepository Teleporters { get; }
    }
}
