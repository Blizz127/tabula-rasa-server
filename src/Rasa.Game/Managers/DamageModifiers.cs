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

        /// <summary>
        /// Shield Extender: the shield the target stands under takes its percent of the hit, out of what the bubble has
        /// left; a bubble that runs dry breaks. Returns the damage that gets through.
        /// </summary>
        public static int ThroughShield(Actor target, int damage)
        {
            var shield = target?.ActiveEffects.Values.Select(effect => effect.Shield).FirstOrDefault(pool => pool != null && pool.Remaining > 0);
            if (shield == null || damage <= 0)
                return damage;
            var absorbed = System.Math.Min(damage * shield.Percent / 100, shield.Remaining);
            shield.Remaining -= absorbed;
            if (shield.Remaining <= 0)
                shield.Broken?.Invoke();
            return damage - absorbed;
        }

        /// <summary>Smoke screen: ranged hits (anything but a melee swing) lose the holder's reduction percent.</summary>
        public static int ThroughSmoke(Actor target, int damage, bool melee)
        {
            if (melee || target == null)
                return damage;
            var reduction = System.Math.Min(100, target.ActiveEffects.Values.Sum(effect => effect.RangedReductionPercent));
            return reduction == 0 ? damage : damage * (100 - reduction) / 100;
        }

        /// <summary>Conversion: the holder takes its percent more from every hit.</summary>
        public static int Taken(Actor target, int damage)
        {
            var more = target?.ActiveEffects.Values.Sum(effect => effect.DamageTakenPercent) ?? 0;
            return more == 0 ? damage : damage * (100 + more) / 100;
        }

        /// <summary>Viral Conversion: virulent damage the source deals becomes the effect's type.</summary>
        public static Data.DamageType? DealtType(Actor source, Data.DamageType? type)
        {
            if (type != Data.DamageType.Virulent || source == null)
                return type;
            return source.ActiveEffects.Values.Select(effect => effect.ConvertVirulentTo).FirstOrDefault(to => to != null) ?? type;
        }

        /// <summary>
        /// Polarity Field: a creature's rating against the hit's type (negative: vulnerable), converted with the client's
        /// own multiplier - (100 - rating) / 100 below zero.
        /// </summary>
        public static int AgainstCreature(Actor target, int damage, Data.DamageType? type)
        {
            if (target == null || type == null || damage <= 0)
                return damage;
            var rating = target.ActiveEffects.Values.Where(effect => effect.VulnerableType == type).Sum(effect => effect.VulnerableRating);
            return rating == 0 ? damage : (int)System.Math.Round(damage * Data.DamageResistance.GetDamageMultiplier(rating), System.MidpointRounding.AwayFromZero);
        }

        public static int ResistRating(Actor target)
            => target?.ActiveEffects.Values.Sum(effect => effect.ResistRating) ?? 0;
    }
}
