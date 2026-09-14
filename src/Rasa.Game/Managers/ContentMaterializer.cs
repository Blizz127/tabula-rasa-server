using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Structures;
    using Structures.Content;
    using Structures.World;

    /// <summary>
    /// Spawns reconstructed-content placements into a map channel. Creature placements materialize
    /// once per shared channel; usable placements are per-client DynamicObjects whose enabled state
    /// follows that client's committed mission/objective state (the crate's usable_condition_id).
    /// </summary>
    public static class ContentMaterializer
    {
        public static void Materialize(MapChannel mapChannel, ContentValidation content)
        {
            var contextId = mapChannel.MapInfo?.MapContextId ?? 0;

            foreach (var placement in PlacementsToSpawn(content, contextId))
            {
                var creature = CreatureManager.Instance.CreatePlacedCreature(placement, mapChannel);

                if (creature != null)
                    CellManager.Instance.AddToWorld(mapChannel, creature);
            }

            foreach (var placement in UsablesToSpawn(content, contextId))
                SpawnUsable(mapChannel, placement);
        }

        /// <summary>
        /// The live creature placements a channel of this context should hold, in id order.
        /// </summary>
        public static IReadOnlyList<ContentPlacementEntry> PlacementsToSpawn(ContentValidation content, uint contextId)
        {
            return content.LivePlacements
                .Where(placement => placement.MapContextId == contextId &&
                                    (ContentPlacementKind)placement.Kind == ContentPlacementKind.Creature)
                .OrderBy(placement => placement.Id)
                .ToList();
        }

        /// <summary>
        /// The live usable placements a channel of this context should hold, in id order.
        /// </summary>
        public static IReadOnlyList<ContentPlacementEntry> UsablesToSpawn(ContentValidation content, uint contextId)
        {
            return content.LivePlacements
                .Where(placement => placement.MapContextId == contextId &&
                                    (ContentPlacementKind)placement.Kind == ContentPlacementKind.Usable)
                .OrderBy(placement => placement.Id)
                .ToList();
        }

        private static void SpawnUsable(MapChannel mapChannel, ContentPlacementEntry placement)
        {
            var usable = new DynamicObject
            {
                EntityClassId = (EntityClasses)placement.EntityClassId,
                Position = new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ),
                Rotation = placement.Rotation,
                MapContextId = placement.MapContextId,
                DynamicObjectType = DynamicObjectType.ContentUsable,
                StateId = (UseObjectState)placement.InitialState,
                WindupTime = placement.WindupMs,
                Comment = $"content:{placement.Id}"
            };

            mapChannel.DynamicObjects.Add(usable);
            mapChannel.ContentUsables[usable.EntityId] = placement.Id;

            if (placement.HitPoints > 0)
            {
                usable.HitPoints = placement.HitPoints;
                usable.MaxHitPoints = placement.HitPoints;
            }

            // Register as an object entity so combat targeting resolves it. IsInWorld
            // keeps the legacy DynamicObjectWorker from re-adding it every tick
            // (content usables materialize once and stay).
            CellManager.Instance.AddToWorld(mapChannel, usable);
            usable.IsInWorld = true;
        }

        /// <summary>
        /// The UsableInfo a client should currently see for this placement: enabled while its
        /// usable condition holds in that client's committed state. missionActivated carries
        /// the binding mission of the condition (the client's glow behavior; synthesis R12).
        /// </summary>
        public static (bool Enabled, uint MissionActivated) UsableStateFor(
            ContentValidation content, ContentState state, ContentPlacementEntry placement)
        {
            if (placement.UsableConditionId == 0)
                return (true, 0);

            var condition = content.Catalog.Conditions.TryGetValue(placement.UsableConditionId, out var terms) ? terms : null;
            if (condition == null)
                return (false, 0);

            var missionId = condition.Select(term => term.MissionId).FirstOrDefault(id => id != 0);
            return (state.Evaluate(condition), missionId);
        }

        /// <summary>
        /// (Re)introduces every live usable of the client's map with its per-client state.
        /// Called on world entry and after commits that can change a usable's condition.
        /// </summary>
        public static void RefreshFor(Client client, ContentValidation content)
        {
            var player = client.Player;
            if (player?.MapChannel == null)
                return;

            var contextId = player.MapChannel.MapInfo?.MapContextId ?? 0;
            var state = new ContentState(player);

            foreach (var placement in UsablesToSpawn(content, contextId))
            {
                var usable = player.MapChannel.DynamicObjects.FirstOrDefault(obj =>
                    obj.DynamicObjectType == DynamicObjectType.ContentUsable &&
                    player.MapChannel.ContentUsables.TryGetValue(obj.EntityId, out var id) && id == placement.Id);

                if (usable == null)
                    continue;

                var (enabled, missionActivated) = UsableStateFor(content, state, placement);

                if (enabled == usable.IsEnabled && missionActivated == usable.ActivateMission)
                    continue;

                usable.IsEnabled = enabled;
                usable.ActivateMission = missionActivated;

                // The client's glow detaches only on disable; when staying enabled the
                // state packet refreshes the glow target (synthesis R12).
                if (!enabled)
                {
                    DynamicObjectManager.Instance.SetUsable(client, usable.EntityId, false, usable);
                    continue;
                }

                DynamicObjectManager.Instance.SetUsable(client, usable.EntityId, true, usable);
            }
        }
    }
}
