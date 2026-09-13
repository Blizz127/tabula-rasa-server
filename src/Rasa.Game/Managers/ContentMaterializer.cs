using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Structures;
    using Structures.Content;
    using Structures.World;

    /// <summary>
    /// Spawns reconstructed-content placements into a map channel. S1 materializes the stationary creature
    /// placements of shared contexts once, at load; per-character instances and usable objects belong to
    /// later slices, and their rows stay withheld until those slices exist.
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
        }

        /// <summary>
        /// The live creature placements a channel of this context should hold, in id order. Usable
        /// placements are skipped until their slice seeds them; they cannot be live before that anyway.
        /// </summary>
        public static IReadOnlyList<ContentPlacementEntry> PlacementsToSpawn(ContentValidation content, uint contextId)
        {
            return content.LivePlacements
                .Where(placement => placement.MapContextId == contextId &&
                                    (ContentPlacementKind)placement.Kind == ContentPlacementKind.Creature)
                .OrderBy(placement => placement.Id)
                .ToList();
        }
    }
}
