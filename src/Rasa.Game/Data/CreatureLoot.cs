using System;
using System.Collections.Generic;

namespace Rasa.Data
{
    using Structures;

    /// <summary>
    /// The creature loot roll, in the shape the original server's creature_type_loot table used.
    ///
    /// Each row is an item template, a percentage chance and a stack size range. Every row is rolled
    /// independently, the way the original table's per-row chance column implies: the junk rows carry the high
    /// chances (12% for a fistful of cartridges, 5% for a med pack) and the equipment rows are rare (0.5% each), so
    /// one kill usually drops one thing, sometimes nothing.
    ///
    /// Kept apart from the loot dispenser so the semantics can be tested without a client, a map or a corpse.
    ///
    /// Rows now come from two sources: the emulator's seven rows (analogues, OD-96) and the rates counted in the
    /// footage ledger (docs/evidence/creature-loot-footage-ledger.json, CreatureLootFootage): Thrax Skull and
    /// standard-grade ammunition on the Thrax infantry, Boargar Ear on the Young Forest Boargar.
    /// </summary>
    public static class CreatureLoot
    {
        /// <summary>
        /// Whether a creature without loot rows gets the stand-in drop. <b>OD-110, pending owner review.</b>
        ///
        /// The stand-in - three Standard Grade Cartridges on a coin flip - is InfiniteRasa's emulator placeholder, not
        /// an original rule for any creature: the footage ledger shows real corpses giving creature-specific junk, any of
        /// the five standard-grade ammunition types in stacks of 33-281, schematics and gear. It is kept, labelled an
        /// analogue, because the alternative leaves every creature the evidence does not yet cover with nothing to drop
        /// but mission items (no corpse income, no ammunition off a corpse). Setting this to false is that alternative;
        /// nothing else changes. Creatures with rows (the Thrax infantry, the Young Forest Boargar, and anything later
        /// evidence covers) never use it.
        /// </summary>
        public const bool StandInDropEnabled = true;

        /// <summary>The stand-in: Standard Grade Cartridges (template 28), three of them (analogue, OD-110).</summary>
        public const uint StandInItemTemplateId = 28, StandInCount = 3;

        /// <summary>
        /// The stand-in drop for a creature with no loot rows: a coin flip (nextInt(0, 2)) for three cartridges, or
        /// nothing at all when the stand-in is switched off (OD-110).
        /// </summary>
        public static List<(uint ItemTemplateId, uint Count)> StandInDrop(Func<int, int, int> nextInt, bool enabled = StandInDropEnabled)
        {
            var drops = new List<(uint, uint)>();
            if (enabled && nextInt(0, 2) > 0)
                drops.Add((StandInItemTemplateId, StandInCount));
            return drops;
        }

        public static List<(uint ItemTemplateId, uint Count)> Roll(CreatureLootData data, Func<double> nextDouble,
            Func<int, int, int> nextInt)
        {
            var drops = new List<(uint, uint)>();
            if (data == null || !data.Any)
                return drops;

            foreach (var row in data.Rows)
            {
                if (row.ItemTemplateId == 0)
                    continue;

                // chance is the percentage the original float column held
                if (nextDouble() * 100.0 >= row.Chance)
                    continue;

                var min = row.StacksizeMin == 0 ? 1u : row.StacksizeMin;
                var max = row.StacksizeMax < min ? min : row.StacksizeMax;
                var count = min == max ? min : (uint)nextInt((int)min, (int)max + 1);
                drops.Add((row.ItemTemplateId, count));
            }

            return drops;
        }
    }
}
