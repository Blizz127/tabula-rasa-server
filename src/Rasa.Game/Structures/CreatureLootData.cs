namespace Rasa.Structures
{
    using System.Collections.Generic;
    using Rasa.Structures.World;

    /// <summary>
    /// A creature's loot rows, in the shape the original server's creature_type_loot table had: each row is an item
    /// template, the percentage chance it drops, and the stack size it drops in. Loaded once from the world database
    /// at startup (CreatureManager.CreatureInit) so a kill only rolls.
    ///
    /// A creature with no rows falls back to the emulator's stand-in drop rather than paying out nothing, because the
    /// per-type tables for every other creature were in the lost server data (GAP-CREATURE-LOOT).
    /// </summary>
    public class CreatureLootData
    {
        public List<CreatureLootEntry> Rows { get; } = new List<CreatureLootEntry>();

        public void Add(CreatureLootEntry entry) => Rows.Add(entry);

        public bool Any => Rows.Count > 0;
    }
}
