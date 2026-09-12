namespace Rasa.Data
{
    /// <summary>
    /// What a `game_account.level` buys. Stored as a byte, so the gaps are deliberate: a rank can
    /// be slotted between two of these later without renumbering anything already in a database.
    ///
    /// The split is by what a command can cost you if the account is not in the right hands.
    /// Observer cannot change anything. GameMaster can move itself and dress the world, all of
    /// which a restart undoes. Admin hands out progression or changes who someone is, which it
    /// does not.
    /// </summary>
    public enum GmLevel : byte
    {
        /// <summary>An ordinary player. No dot commands at all.</summary>
        Player = 0,

        /// <summary>Look, don't touch: positions, distances, what is nearby, the GM UI flag.</summary>
        Observer = 1,

        /// <summary>Move yourself, spawn and drive scenery and creatures, drive your own client.</summary>
        GameMaster = 5,

        /// <summary>Grant progression, rename a player, reload server data.</summary>
        Admin = 10
    }
}
