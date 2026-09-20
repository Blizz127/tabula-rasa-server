namespace Rasa.Repositories.World
{
    using MissionContent;
    using UnitOfWork;

    public interface IWorldUnitOfWork : IUnitOfWork
    {
        IMapMarkerRepository MapMarkers { get; }
        IActionRepository Actions { get; }
        IEquipmentRepository Equipment { get; }
        ICreatureRepository Creatures { get; }
        IEntityClassRepository EntityClasses { get; }
        IFootlockerRepository Footlockers { get; }
        ILogosRepository Logoses { get; }
        IMapInfoRepository MapInfos { get; }
        IMissionContentRepository MissionContent { get; }
        IMapLinkRepository MapLinks { get; }
        IKraftwerksRepository Kraftwerks { get; }
        IMapRegionRepository MapRegions { get; }
        INpcMissionRepository NpcMissions { get; }
        INpcMissionObjectiveRepository NpcMissionObjectives { get; }
        INpcMissionRewardRepository NpcMissionRewards { get; }
        INpcPackageRepository NpcPackages { get; }
        IPlayerRandomNameRepository RandomNames { get; }
        ISpawnpoolRepository Spawnpools { get; }
        ITeleporterRepository Teleporters { get; }
    }
}
