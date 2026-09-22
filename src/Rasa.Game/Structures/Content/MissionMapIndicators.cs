using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Structures.Content
{
    using Data;
    using Structures.World;

    /// <summary>What an objective's derived indicator was read from. Diagnostic only; nothing of this reaches the client.</summary>
    public enum MissionIndicatorSource : byte
    {
        /// <summary>An <c>npc_mission_objective_indicator</c> row. Stored indicators always win over derived ones.</summary>
        Stored = 0,

        /// <summary>The placement of a creature carrying one of the objective's conversation packages.</summary>
        Conversation = 1,

        /// <summary>The centre of an <see cref="ObjectiveBindingKind.AreaEntered"/> binding's area, with its radius.</summary>
        Area = 2,

        /// <summary>The <c>logos</c> row of a <see cref="ObjectiveBindingKind.LogosRecovered"/> binding's shrine.</summary>
        Logos = 3,

        /// <summary>The content placement a use/loot/hit/state binding names.</summary>
        Placement = 4,

        /// <summary>A spawn pool that holds the creature a kill binding counts.</summary>
        Kill = 5
    }

    /// <summary>
    /// The world positions an objective's completion route can be read from: everything the derivation in
    /// <see cref="MissionMapIndicators"/> needs, and nothing else, so it can be built in a test from plain rows.
    /// </summary>
    public sealed class MissionIndicatorWorld
    {
        public static readonly MissionIndicatorWorld Empty = new MissionIndicatorWorld();

        public IReadOnlyDictionary<uint, ContentAreaEntry> Areas { get; init; } = new Dictionary<uint, ContentAreaEntry>();
        public IReadOnlyDictionary<uint, ContentPlacementEntry> Placements { get; init; } = new Dictionary<uint, ContentPlacementEntry>();
        public IReadOnlyDictionary<uint, LogosEntry> Logos { get; init; } = new Dictionary<uint, LogosEntry>();
        public IReadOnlyList<SpawnPoolEntry> SpawnPools { get; init; } = new List<SpawnPoolEntry>();

        /// <summary>
        /// Creature placements by the creature they stand for, in placement id order. A creature may be placed
        /// more than once (the same quartermaster on two maps).
        /// </summary>
        public IReadOnlyDictionary<uint, IReadOnlyList<ContentPlacementEntry>> PlacementsByCreature { get; init; }
            = new Dictionary<uint, IReadOnlyList<ContentPlacementEntry>>();

        /// <summary>
        /// <c>npc_package</c> read the way CreatureManager reads it (the row id is the creature, package_id is the
        /// package it answers for), plus the placements whose own npc_package_id overrides that binding.
        /// </summary>
        public IReadOnlyDictionary<uint, IReadOnlyList<uint>> CreaturesByPackage { get; init; }
            = new Dictionary<uint, IReadOnlyList<uint>>();

        /// <summary>Placements that carry an npc_package_id of their own, by that package.</summary>
        public IReadOnlyDictionary<uint, IReadOnlyList<ContentPlacementEntry>> PlacementsByPackage { get; init; }
            = new Dictionary<uint, IReadOnlyList<ContentPlacementEntry>>();

        public static MissionIndicatorWorld Build(
            IEnumerable<ContentAreaEntry> areas,
            IEnumerable<ContentPlacementEntry> placements,
            IEnumerable<LogosEntry> logos,
            IEnumerable<SpawnPoolEntry> spawnPools,
            IEnumerable<NpcPackageEntry> npcPackages)
        {
            var placementList = (placements ?? Enumerable.Empty<ContentPlacementEntry>()).OrderBy(placement => placement.Id).ToList();

            return new MissionIndicatorWorld
            {
                Areas = (areas ?? Enumerable.Empty<ContentAreaEntry>()).GroupBy(area => area.Id).ToDictionary(group => group.Key, group => group.First()),
                Placements = placementList.ToDictionary(placement => placement.Id),
                Logos = (logos ?? Enumerable.Empty<LogosEntry>()).GroupBy(row => row.Id).ToDictionary(group => group.Key, group => group.First()),
                SpawnPools = (spawnPools ?? Enumerable.Empty<SpawnPoolEntry>()).OrderBy(pool => pool.Id).ToList(),
                PlacementsByCreature = placementList
                    .Where(placement => (ContentPlacementKind)placement.Kind == ContentPlacementKind.Creature && placement.CreatureId != 0)
                    .GroupBy(placement => placement.CreatureId)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<ContentPlacementEntry>)group.ToList()),
                PlacementsByPackage = placementList
                    .Where(placement => placement.NpcPackageId != 0)
                    .GroupBy(placement => placement.NpcPackageId)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<ContentPlacementEntry>)group.ToList()),
                CreaturesByPackage = (npcPackages ?? Enumerable.Empty<NpcPackageEntry>())
                    .Where(package => package.PackageId != 0)
                    .GroupBy(package => package.PackageId)
                    .ToDictionary(group => group.Key, group => (IReadOnlyList<uint>)group.Select(package => package.Id).Distinct().OrderBy(id => id).ToList())
            };
        }
    }

    /// <summary>
    /// Derives the map and radar markers of a mission's objectives from where the world already says those
    /// objectives are finished. Nothing here is stored: it is read once when the content layer loads and
    /// travels in the mission info the client already receives.
    ///
    /// What the client does with them (1.16.5.0 disassembly, 2026-09-13 research copy):
    ///
    ///   * <c>client/ui/mapwindow.py HandleUpdateMissionIndicators</c> (line 426) walks the whole mission log,
    ///     skips every objective whose objStatus is COMPLETED or FAILED, and calls <c>_CreateMissionMapMarker</c>
    ///     for each entry of that objective's indicatorList - one <c>uimapmarker.MISSION_INDICATOR</c> widget per
    ///     indicator, placed by world X and Z. So: many indicators per objective are supported, and an indicator
    ///     disappears by itself the moment its objective completes.
    ///   * <c>client/ui/radarwindow.py</c> (line 1230) draws the same list as radar pips, with an arrow at the rim
    ///     for one that is off the radar.
    ///   * Both gate visibility on <c>client.gameui.GetMissionTracking()</c>: a marker is drawn but hidden unless
    ///     its mission is tracked in the mission log.
    ///   * <c>client/missionlog.py _UpdateIndicators</c> (line 544) additionally spawns a world-space proxy entity
    ///     carrying the OVERHEAD_MISSION_INDICATOR effect, but only for an indicator whose bShow3DEffect is set.
    ///     Derived indicators leave it clear, which is also what the boot camp's own stored rows do
    ///     (BootcampObjectiveIndicatorRows): map and radar, no beam in the world.
    ///   * <c>mapwindow.OnMissionMarkerHighlighted</c> (line 758) reads the tooltip from
    ///     <c>BuildObjectiveIndicator(indicatorId)</c> when indicatorId is not None, and from
    ///     <c>BuildObjectiveNameText(missionId, objectiveId)</c> when it is. Derived indicators send None, so the
    ///     tooltip is the objective's own text rather than a canned label out of the client's
    ///     missionobjectiveindicator table that would have to be guessed.
    ///   * A radius over 1 makes the map draw a circle of 2 * radius around the marker while it is highlighted
    ///     (same function, line 789), which is why area objectives send the trigger's own radius and everything
    ///     else sends 0 - the same as the stored boot-camp rows.
    ///
    /// What the client cannot do, and so is not attempted here: the indicator tuple carries no map, and
    /// <c>mapwindow._PlaceWidget</c> (line 1042) places every marker with the offset and scale of whatever map is
    /// open. An indicator for another map would therefore be drawn at a wrong spot on this one, so indicators
    /// carry the map they belong to and are filtered to the player's own map when the mission info is written
    /// (PlayerMission.ToMissionInfo). The mission log is resent on every map entry
    /// (MapChannelManager -> MissionManager.SendMissionStatusInfo), so the set follows the player.
    /// </summary>
    public static class MissionMapIndicators
    {
        /// <summary>
        /// Spawn pools a kill objective may mark. A creature that spawns in more places than this on one map is
        /// ambient rather than a landmark, and marking all of them would paper the map; the first pools in id
        /// order are marked and the rest are left off.
        /// </summary>
        public const int MaxKillMarkers = 8;

        /// <summary>Markers one objective may carry, whatever it is bound to.</summary>
        public const int MaxPerObjective = 16;

        /// <summary>Two derived positions closer than this on the same map are the same marker.</summary>
        private const double DuplicateMetres = 1.0;

        /// <summary>
        /// Every objective of the definition that has a sourced position, by objective id. Objectives with no
        /// route to a position (an equip objective, a counter with no binding, a conversation whose package no
        /// creature in this world answers for) are simply absent.
        /// </summary>
        public static Dictionary<uint, List<MissionIndicator>> Derive(Mission mission, MissionIndicatorWorld world)
        {
            var derived = new Dictionary<uint, List<MissionIndicator>>();

            if (mission == null || world == null)
                return derived;

            foreach (var objective in mission.Objectives.Keys.OrderBy(id => id))
            {
                var indicators = new List<MissionIndicator>();

                foreach (var candidate in Candidates(mission, objective, world))
                {
                    if (indicators.Count >= MaxPerObjective)
                        break;

                    if (indicators.Any(existing => existing.MapContextId == candidate.MapContextId &&
                                                   Vector3.Distance(existing.Position, candidate.Position) <= DuplicateMetres))
                        continue;

                    indicators.Add(candidate);
                }

                if (indicators.Count > 0)
                    derived[objective] = indicators;
            }

            return derived;
        }

        private static IEnumerable<MissionIndicator> Candidates(Mission mission, uint objectiveId, MissionIndicatorWorld world)
        {
            // Conversation first: an objective finished by talking to someone is the one the player most needs
            // pointing at, and the server completes it through any of the objective's conversation rows whatever
            // their convoType (MissionManager.CompleteNpcObjective -> Mission.HasObjectiveConversation).
            foreach (var package in mission.ObjectiveConversations
                         .Where(conversation => conversation.ObjectiveId == objectiveId && conversation.NpcPackageId != 0)
                         .Select(conversation => conversation.NpcPackageId)
                         .Distinct()
                         .OrderBy(package => package))
                foreach (var indicator in PackagePositions(package, world))
                    yield return indicator;

            foreach (var binding in mission.Bindings.Where(binding => binding.ObjectiveId == objectiveId).OrderBy(binding => binding.BindingId))
                foreach (var indicator in BindingPositions(binding, world))
                    yield return indicator;
        }

        /// <summary>
        /// Where a creature answering for this conversation package stands. A placement's own npc_package_id
        /// overrides the creature's npc_package row (CreatureManager.CreatePlacedCreature), so those placements
        /// are read first; otherwise the package names its creature through the npc_package table and that
        /// creature's placements and spawn pools are used.
        /// </summary>
        private static IEnumerable<MissionIndicator> PackagePositions(uint package, MissionIndicatorWorld world)
        {
            if (world.PlacementsByPackage.TryGetValue(package, out var overriding))
                foreach (var placement in overriding)
                    yield return At(placement.MapContextId, placement.PosX, placement.PosY, placement.PosZ, MissionIndicatorSource.Conversation);

            if (!world.CreaturesByPackage.TryGetValue(package, out var creatures))
                yield break;

            foreach (var creatureId in creatures)
                foreach (var indicator in CreaturePositions(creatureId, world, MissionIndicatorSource.Conversation))
                    yield return indicator;
        }

        private static IEnumerable<MissionIndicator> BindingPositions(NpcMissionObjectiveBindingEntry binding, MissionIndicatorWorld world)
        {
            switch ((ObjectiveBindingKind)binding.Kind)
            {
                case ObjectiveBindingKind.AreaEntered:
                    if (world.Areas.TryGetValue(binding.AreaId, out var area))
                        // The trigger's own centre and radius: the map draws the circle the player has to stand in.
                        yield return At(area.MapContextId, area.PosX, area.PosY, area.PosZ, MissionIndicatorSource.Area, area.Radius);
                    break;

                case ObjectiveBindingKind.LogosRecovered:
                    // A shrine is not a content placement; the binding's placement_id holds the logos row id
                    // (ObjectiveBindingKind.LogosRecovered, DynamicObjectManager.LogosRecovery).
                    if (world.Logos.TryGetValue(binding.PlacementId, out var logos))
                        yield return At(logos.MapContextId, logos.PosX, logos.PosY, logos.PosZ, MissionIndicatorSource.Logos);
                    break;

                case ObjectiveBindingKind.UseCompleted:
                case ObjectiveBindingKind.LootAll:
                case ObjectiveBindingKind.PlacementState:
                case ObjectiveBindingKind.Hit:
                    if (world.Placements.TryGetValue(binding.PlacementId, out var placement))
                        yield return At(placement.MapContextId, placement.PosX, placement.PosY, placement.PosZ, MissionIndicatorSource.Placement);
                    break;

                case ObjectiveBindingKind.Kill:
                {
                    // A kill binding naming one placement is a named body standing in one spot; otherwise the
                    // only sourced position for the targets is where this world spawns them.
                    if (binding.PlacementId != 0 && world.Placements.TryGetValue(binding.PlacementId, out var placed))
                    {
                        yield return At(placed.MapContextId, placed.PosX, placed.PosY, placed.PosZ, MissionIndicatorSource.Kill);
                        break;
                    }

                    if (binding.CreatureId == 0)
                        break;

                    var marked = 0;

                    foreach (var pool in world.SpawnPools.Where(pool => Holds(pool, binding.CreatureId)))
                    {
                        if (marked++ >= MaxKillMarkers)
                            break;

                        yield return At(pool.MapContextId, pool.PosX, pool.PosY, pool.PosZ, MissionIndicatorSource.Kill);
                    }

                    break;
                }

                // An equip objective is finished out of the player's own pack; nothing in the world holds a
                // position for it, so it is left unmarked rather than pointed somewhere invented.
                case ObjectiveBindingKind.Equip:
                default:
                    break;
            }
        }

        private static IEnumerable<MissionIndicator> CreaturePositions(uint creatureId, MissionIndicatorWorld world, MissionIndicatorSource source)
        {
            if (world.PlacementsByCreature.TryGetValue(creatureId, out var placements))
                foreach (var placement in placements)
                    yield return At(placement.MapContextId, placement.PosX, placement.PosY, placement.PosZ, source);

            foreach (var pool in world.SpawnPools.Where(pool => Holds(pool, creatureId)))
                yield return At(pool.MapContextId, pool.PosX, pool.PosY, pool.PosZ, source);
        }

        private static bool Holds(SpawnPoolEntry pool, uint creatureId) =>
            creatureId != 0 && (pool.Creature1Id == creatureId || pool.Creature2Id == creatureId || pool.Creature3Id == creatureId ||
                                pool.Creature4Id == creatureId || pool.Creature5Id == creatureId || pool.Creature6Id == creatureId);

        private static MissionIndicator At(uint mapContextId, double x, double y, double z, MissionIndicatorSource source, double radius = 0)
        {
            return new MissionIndicator
            {
                MapContextId = mapContextId,
                Position = new Vector3((float)x, (float)y, (float)z),
                Radius = radius,
                // None, so the map tooltip falls back to the objective's own text
                // (mapwindow.OnMissionMarkerHighlighted line 797).
                IndicatorId = null,
                // Map and radar only; the world-space beam is a stored-row decision (missionlog._UpdateIndicators).
                Show3DEffect = false,
                Source = source
            };
        }
    }
}
