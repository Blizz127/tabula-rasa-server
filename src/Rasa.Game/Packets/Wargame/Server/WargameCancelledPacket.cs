namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameCancelled(wargameId), opcode 605, on entity 23. Clears
    /// bActive, hides the tracker and shows PM_WARGAME_CANCELLED ("Your Wargame has been
    /// cancelled."), with no winner and no big text. It is the only ending the client offers that
    /// declares nobody - which is what a duel that stops without being fought out needs.
    /// </summary>
    public class WargameCancelledPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameCancelled;

        public uint WargameId { get; set; }

        public WargameCancelledPacket(uint wargameId)
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
