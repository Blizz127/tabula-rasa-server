using System;

namespace Rasa.Structures
{
    public static class AttributePointAllocation
    {
        public static int GetAvailablePoints(Manifestation player)
        {
            // Invalid legacy allocations must not create additional spendable points.
            if (player.Level < 1 || player.SpentBody < 0 || player.SpentMind < 0 || player.SpentSpirit < 0)
                return 0;

            var spent = (long)player.SpentBody + player.SpentMind + player.SpentSpirit;
            return (int)Math.Max(0, 3L * (player.Level - 1) - spent);
        }

        public static bool TryAllocate(Manifestation player, int body, int mind, int spirit)
        {
            // Validate the whole request before changing any attribute. Use a wide sum
            // so large client values cannot wrap around the earned-point check.
            if (body < 0 || mind < 0 || spirit < 0)
                return false;

            var requested = (long)body + mind + spirit;
            if (requested == 0 || requested > GetAvailablePoints(player))
                return false;

            player.SpentBody += body;
            player.SpentMind += mind;
            player.SpentSpirit += spirit;
            return true;
        }
    }
}
