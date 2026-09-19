namespace Rasa.Structures
{
    // One current player ability, separate from the legacy object/weapon queue.
    // Deadlines use the server's monotonic millisecond clock.
    public class AbilityExecution
    {
        public ActionData Action { get; }
        public MapChannel Map { get; }
        public Actor OriginalTarget { get; }
        // A destroyable content placement target (not an Actor); null for creature targets.
        public DynamicObject OriginalContentTarget { get; }
        public long WindupEndsAt { get; }
        public long RecoveryEndsAt { get; }
        public long ReuseEndsAt { get; }
        public bool Resolved { get; set; }
        /// <summary>The client action-table row a table-driven damage ability resolves with; null for Lightning.</summary>
        public ActionLevelInfo Level { get; set; }

        public AbilityExecution(ActionData action, MapChannel map, Actor target,
            long windupEndsAt, long recoveryEndsAt, long reuseEndsAt)
        {
            Action = action;
            Map = map;
            OriginalTarget = target;
            WindupEndsAt = windupEndsAt;
            RecoveryEndsAt = recoveryEndsAt;
            ReuseEndsAt = reuseEndsAt;
        }

        public AbilityExecution(ActionData action, MapChannel map, Actor target, DynamicObject contentTarget,
            long windupEndsAt, long recoveryEndsAt, long reuseEndsAt)
        {
            Action = action;
            Map = map;
            OriginalTarget = target;
            OriginalContentTarget = contentTarget;
            WindupEndsAt = windupEndsAt;
            RecoveryEndsAt = recoveryEndsAt;
            ReuseEndsAt = reuseEndsAt;
        }
    }
}
