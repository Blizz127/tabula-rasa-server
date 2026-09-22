namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameTied(wargameId), opcode 634, on entity 23. Clears bActive,
    /// sets isTied, hides the tracker, and for a DUEL shows PM_WARGAME_TIED ("The Wargame has
    /// ended in a tie.") and the big PM_WARGAME_BIGTEXT_DUEL_TIE ("Duel Wargame: Tie Game!").
    /// </summary>
    public class WargameTiedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameTied;

        public uint WargameId { get; set; }

        public WargameTiedPacket(uint wargameId)
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
