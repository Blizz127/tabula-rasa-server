using Rasa.Data;

namespace Rasa.Structures
{
    public sealed class WeaponAttackExecution
    {
        public ActionId ActionId { get; }
        public uint ArgumentId { get; }
        public Item Weapon { get; }
        public MapChannel Map { get; }
        public Actor Target { get; }
        public uint AmmoCost { get; }
        public long WindupEndsAt { get; }
        public long RecoveryEndsAt { get; }
        public long ReuseEndsAt { get; }
        public bool StartsReuse { get; }
        public bool ClientRequested { get; }
        public bool Resolved { get; set; }

        public WeaponAttackExecution(ActionId actionId, uint argumentId, Item weapon, MapChannel map,
            Actor target, uint ammoCost, long now, WeaponActionTiming timing, bool clientRequested)
        {
            ActionId = actionId;
            ArgumentId = argumentId;
            Weapon = weapon;
            Map = map;
            Target = target;
            AmmoCost = ammoCost;
            WindupEndsAt = now + timing.WindupMilliseconds;
            RecoveryEndsAt = WindupEndsAt + timing.RecoveryMilliseconds;
            ReuseEndsAt = RecoveryEndsAt + timing.ReuseMilliseconds;
            StartsReuse = timing.StartReuseTimerOnPerform;
            ClientRequested = clientRequested;
        }
    }
}
