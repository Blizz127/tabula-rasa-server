namespace Rasa.Structures
{
    // One current player ability, separate from the legacy object/weapon queue.
    // Deadlines use the server's monotonic millisecond clock.
    public class AbilityExecution
    {
        public ActionData Action { get; }
        public MapChannel Map { get; }
        public Actor OriginalTarget { get; }
        public long WindupEndsAt { get; }
        public long RecoveryEndsAt { get; }
        public long ReuseEndsAt { get; }
        public bool Resolved { get; set; }

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
    }
}
