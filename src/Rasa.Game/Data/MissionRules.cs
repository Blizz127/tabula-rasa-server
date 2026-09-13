using System.Collections.Generic;

namespace Rasa.Data
{
    /// <summary>
    /// Mission-log limits the original 1.16.5.0 client enforces itself
    /// (shared/gameconstants.pyo). The server mirrors them so it never accepts a
    /// request the original client could not have sent.
    /// </summary>
    public static class MissionRules
    {
        // MAX_MISSION_COUNT
        public const int MaxMissionCount = 30;

        // MAX_CONVERSATION_RANGE, checked by Actor.IsInConversationRange before Converse is sent.
        public const float ConversationRange = 5f;

        // Emulator slack for movement that arrives after the client's own range check.
        public const float ConversationRangeTolerance = 2f;

        // NON_ABANDONABLE_MISSIONS: the client disables Abandon for these ids.
        public static readonly IReadOnlyCollection<uint> NonAbandonableMissions = new HashSet<uint> { 1990, 2010, 2011 };
    }
}
