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
        ContentUsable       = 9
    }
}
