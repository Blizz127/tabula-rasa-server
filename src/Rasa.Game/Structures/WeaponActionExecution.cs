using Rasa.Data;

namespace Rasa.Structures
{
    public sealed class WeaponActionExecution
    {
        public ActionId ActionId { get; }
        public uint ArgumentId { get; }
        public Item Weapon { get; }
        public MapChannel Map { get; }
        public long WindupEndsAt { get; }
        public long RecoveryEndsAt { get; private set; }
        public long ReuseEndsAt { get; private set; }
        public bool ClientRequested { get; }
        public bool Resolved { get; set; }

        public WeaponActionExecution(ActionId actionId, uint argumentId, Item weapon, MapChannel map,
            long windupEndsAt, long recoveryEndsAt, long reuseEndsAt, bool clientRequested)
        {
            ActionId = actionId;
            ArgumentId = argumentId;
            Weapon = weapon;
            Map = map;
            WindupEndsAt = windupEndsAt;
            RecoveryEndsAt = recoveryEndsAt;
            ReuseEndsAt = reuseEndsAt;
            ClientRequested = clientRequested;
        }

        public void BeginReloadRecovery(long resolvedAt)
        {
            // Reload does not predict DoAction locally. Its recovery starts with
            // the server resolution, including when windup processing was late.
            ReuseEndsAt = resolvedAt + (ReuseEndsAt - WindupEndsAt);
            RecoveryEndsAt = resolvedAt + (RecoveryEndsAt - WindupEndsAt);
        }
    }
}
