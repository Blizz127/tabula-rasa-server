namespace Rasa.Data
{
    /// <summary>
    /// The map screen's marker kinds, from the client's own <c>generated.client.uimapmarker</c>.
    ///
    /// Only the first four appear in <c>factionedmarkers</c> and so only those have a state the
    /// server sends; the rest are drawn from the static tables or from live entities - region and
    /// point-of-interest labels, the other teleporter kinds, party and team members, vendors and
    /// footlockers - and the client colours them itself.
    /// </summary>
    public static class MapMarkerType
    {
        public const uint ControlPoint = 1;
        public const uint WaypointTeleporter = 2;
        public const uint Hospital = 3;
        public const uint CraftingStation = 4;
        public const uint SafeZone = 19;
    }

    // ControlPointOwnershipType lives in ControlPointData.cs here, with the same values.
}
