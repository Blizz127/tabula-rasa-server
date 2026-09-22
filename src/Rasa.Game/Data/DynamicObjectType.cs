namespace Rasa.Data
{
    public enum DynamicObjectType
    {
        ControlPoint        = 1,
        LocalTeleporter     = 2,
        Lockbox             = 3,
        Waypoint            = 4,
        Wormhole            = 5,
        MapTrigger          = 6,
        DropshipTeleporter  = 7,
        Logos               = 8,
        // A reconstructed-content usable placement (content_placement kind 2).
        // Dispatch is by the server-side usable kind of its placement row.
        ContentUsable       = 9,
        // A crafting station (kraftwerks table; KraftwerksManager).
        Kraftwerks          = 10,
        // A clan's lockbox (entity class 10000063, augmentation 76 CLANLOCKBOX). It shares the
        // footlocker table and the use path, but it is not a personal footlocker: it holds the
        // clan's inventory and it announces its own state.
        ClanLockbox         = 11
    }
}
