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
    /// </summary>
    public static class CreatureLoot
    {
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
