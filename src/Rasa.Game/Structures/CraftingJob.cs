using System.Collections.Generic;

namespace Rasa.Structures
{
    /// <summary>
    /// One item a Kraftwerks is making for a player: what comes out, how long is left, which
    /// crafting page asked for it. Mirrors the client's ItemStatus (shared/crafting.py).
    /// </summary>
    public class CraftingJob
    {
        public ulong ResultItemId { get; set; }
        public uint ResultClassId { get; set; }
        public uint ResultItemTemplateId { get; set; }
        public uint Count { get; set; }
        public double TimeLeftSeconds { get; set; }
        public uint CraftingPage { get; set; }
        public uint QualityId { get; set; }
        public List<uint> LootModuleIds { get; set; } = new List<uint>();
    }
}
