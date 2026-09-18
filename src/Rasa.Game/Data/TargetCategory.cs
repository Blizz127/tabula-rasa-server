namespace Rasa.Data
{
    /// <summary>
    /// What the client may do with an entity as a target. These are the client's own values, from
    /// generated/client/targetdata.pyo, and they are not factions: <c>TargetCategoryPacket</c> used to
    /// carry <see cref="Factions"/>, which can only say 0 or 1 and so could never say Object.
    ///
    /// The client will not target an entity whose category it has not been told. <c>targeting.py</c>'s
    /// <c>SetDirectTarget</c> asks the entity for <c>GetTargetCategory()</c> and returns False when it is
    /// None, before it would have sent <c>SetTargetId</c> - so the server never learns of a target and
    /// every shot resolves against entity 0. <c>Recv_TargetCategory</c> is defined on the actor
    /// augmentation (so every creature) and on inertdestroyable (the boot camp's dummies).
    /// </summary>
    public enum TargetCategory
    {
        Hostile = 0,
        Friendly = 1,
        Object = 2,
        Neutral = 3,
        Decoration = 4,
        DecorationProxy = 5,
        Ignore = 6
    }
}
