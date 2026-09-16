using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Data
{
    using Rasa.Structures.World;

    /// <summary>
    /// The escort rule, kept apart from the runtime so it can be tested without a map.
    ///
    /// An escort objective ("Take Milpas to Apirka") is an area objective like any other, except that reaching the
    /// place is not enough: the creature being escorted has to be there as well. The original's escort objectives read
    /// exactly that way, and the client's own objective text for them is the escort sentence itself.
    /// </summary>
    public static class ContentEscort
    {
        /// <summary>The distance an escort may fall behind before it is sent after its player, squared.</summary>
        public const float EscortFollowDistanceSquared = 6.0f * 6.0f;

        /// <summary>True when every escort of the mission is inside the area (or the mission has no escorts).</summary>
        public static bool AllInside(IReadOnlyList<Vector3> escorts, ContentAreaEntry area)
        {
            if (escorts == null || escorts.Count == 0)
                return true;

            foreach (var escort in escorts)
                if (!Inside(escort, area))
                    return false;

            return true;
        }

        public static bool Inside(Vector3 position, ContentAreaEntry area)
        {
            if (area == null)
                return true;

            var horizontal = System.Math.Sqrt(
                (position.X - area.PosX) * (position.X - area.PosX) +
                (position.Z - area.PosZ) * (position.Z - area.PosZ));

            if (horizontal > area.Radius)
                return false;

            // A sphere bounds vertically by its radius; a vertical cylinder by its half height.
            var halfHeight = area.Shape == 2 && area.HalfHeight > 0 ? area.HalfHeight : area.Radius;
            return System.Math.Abs(position.Y - area.PosY) <= halfHeight;
        }
    }
}
