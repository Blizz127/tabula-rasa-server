namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_RemoveFromWargame(wargameId), opcode 715, on entity 23. Clears
    /// bActive, hides the tracker, and - only if the id was known - shows PM_WARGAME_YOU_LEFT
    /// ("You have left the Wargame.").
    ///
    /// The text is what fixes its use: this is the packet for the player who took themselves out,
    /// not for either side of a decided duel. The player left behind is told with
    /// WargameCancelled and PM_WARGAME_PLAYER_LEFT.
    /// </summary>
    public class RemoveFromWargamePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemoveFromWargame;

        public uint WargameId { get; set; }

        public RemoveFromWargamePacket(uint wargameId)
        {
            WargameId = wargameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(WargameId);
        }
    }
}
