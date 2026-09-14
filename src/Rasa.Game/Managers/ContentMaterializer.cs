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

            // Placements with a presence condition wait for their owner (RefreshPresence).
            foreach (var placement in PlacementsToSpawn(content, contextId))
                if (placement.PresentConditionId == 0)
                    SpawnCreature(mapChannel, placement);

            // So do usables whose state at materialization depends on the owner (alternate_state_condition_id).
            foreach (var placement in UsablesToSpawn(content, contextId))
                if (placement.PresentConditionId == 0 && placement.AlternateStateConditionId == 0)
                    SpawnUsable(mapChannel, placement, content, null, 0);
        }

        private static void SpawnCreature(MapChannel mapChannel, ContentPlacementEntry placement)
        {
            var creature = CreatureManager.Instance.CreatePlacedCreature(placement, mapChannel);

            if (creature == null)
                return;

            creature.ContentPlacementId = placement.Id;
            CellManager.Instance.AddToWorld(mapChannel, creature);
        }

        /// <summary>
        /// Spawns or removes the owner-conditioned placements of a private instance so that exactly those
        /// whose present_condition_id holds in the owner's committed state exist (build plan S3 step 6:
        /// the boss while its objective is incomplete, Youngblood once it is done). A dead creature
        /// whose condition still holds is left for the corpse lifecycle; rebuilding restores it on the
        /// next entry. Shared contexts have no owner and are never refreshed.
        /// </summary>
        public static void RefreshPresence(Client client, ContentValidation content, long now = -1)
        {
            var player = client?.Player;
            var channel = player?.MapChannel;
            if (channel?.MapInfo == null || !channel.IsPrivateInstance || channel.OwnerCharacterId != player.Id)
                return;

            var contextId = channel.MapInfo.MapContextId;
            var state = new ContentState(player);

            bool Present(ContentPlacementEntry placement) =>
                content.Catalog.Conditions.TryGetValue(placement.PresentConditionId, out var condition) && state.Evaluate(condition);

            var creatures = channel.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).ToList();
            foreach (var placement in PlacementsToSpawn(content, contextId).Where(placement => placement.PresentConditionId != 0))
            {
                var existing = creatures.FirstOrDefault(creature => creature.ContentPlacementId == placement.Id);
                var present = Present(placement);

                if (present && existing == null)
                    SpawnCreature(channel, placement);
                else if (!present && existing != null && existing.State != CharacterState.Dead)
                    CellManager.Instance.RemoveCreatureFromWorld(channel, existing);
            }

            foreach (var placement in UsablesToSpawn(content, contextId).Where(placement => placement.PresentConditionId != 0 || placement.AlternateStateConditionId != 0))
            {
                var existing = channel.DynamicObjects.FirstOrDefault(obj =>
                    channel.ContentUsables.TryGetValue(obj.EntityId, out var id) && id == placement.Id);
                var present = placement.PresentConditionId == 0 || Present(placement);

                if (present && existing == null)
                    SpawnUsable(channel, placement, content, state, now < 0 ? System.Environment.TickCount64 : now);
                else if (!present && existing != null)
                {
                    channel.ContentUsables.Remove(existing.EntityId);
                    channel.DynamicObjects.Remove(existing);
                    CellManager.Instance.RemoveFromWorld(channel, existing);
                }
            }
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

        /// <summary>
        /// Spawns a usable in its initial state, or in its alternate state when the owner's committed state satisfies
        /// alternate_state_condition_id. A bomb rebuilt armed (the planted-bomb fact of a character who left while the
        /// fuse burned) gets a fresh fuse, so the detonation still completes exactly once (build plan S5).
        /// </summary>
        private static void SpawnUsable(MapChannel mapChannel, ContentPlacementEntry placement, ContentValidation content, ContentState ownerState, long now)
        {
            var initialState = placement.InitialState;

            if (placement.AlternateStateConditionId != 0 && ownerState != null &&
                content.Catalog.Conditions.TryGetValue(placement.AlternateStateConditionId, out var alternate) && ownerState.Evaluate(alternate))
                initialState = placement.AlternateState;

            var usable = new DynamicObject
            {
                EntityClassId = (EntityClasses)placement.EntityClassId,
                Position = new Vector3((float)placement.PosX, (float)placement.PosY, (float)placement.PosZ),
                Rotation = placement.Rotation,
                MapContextId = placement.MapContextId,
                DynamicObjectType = DynamicObjectType.ContentUsable,
                StateId = (UseObjectState)initialState,
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

            if ((ContentUsableKind)placement.UsableKind == ContentUsableKind.Bomb && initialState == 114 && placement.FuseMs > 0)
            {
                usable.FuseAt = now + placement.FuseMs;
                usable.ArmedByCharacterId = mapChannel.OwnerCharacterId ?? 0;
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
