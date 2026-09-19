using System.Linq;

namespace Rasa.Managers
{
    using Structures;

    /// <summary>
    /// Damage the holder's effects change: Rage's damage bonus out, and its resistance in - a rating added to the
    /// holder's resistance against every damage type, which DamageResistance converts as the client does.
    /// </summary>
    public static class DamageModifiers
    {
        public static int Outgoing(Actor source, int damage)
        {
            var bonus = source?.ActiveEffects.Values.Sum(effect => effect.DamageBonusPercent) ?? 0;
            return bonus == 0 ? damage : (int)(damage * (100L + bonus) / 100);
        }

        public static int ResistRating(Actor target)
            => target?.ActiveEffects.Values.Sum(effect => effect.ResistRating) ?? 0;
    }
}
