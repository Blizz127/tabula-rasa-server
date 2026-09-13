namespace Rasa.Data
{
    /// <summary>
    /// Emulator meaning of npc_mission_reward.type (the table had no rows or
    /// consumer before). Currency and experience rows carry a positive amount in
    /// the credits column, at most one row per type; item rows use
    /// item_template_id and a non-zero quantity. Selectable rows are offered in
    /// table order and the client's selectionIdx picks one of them. Any other row
    /// makes the mission definition incomplete, so it is not offered.
    /// </summary>
    public enum NpcMissionRewardType : byte
    {
        Credits        = 1,
        Prestige       = 2,
        Experience     = 3,
        FixedItem      = 4,
        SelectableItem = 5
    }
}
