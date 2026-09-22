namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameChallengeRefused(wargameId, yourPartyRefused), opcode 637,
    /// on entity 23. Sent to the challenger when the challenge is declined.
    ///
    /// The handler does one thing: _CloseWargameChallengeUI(wargameId), which destroys both
    /// challenge message boxes and clears the status-updater indicators. It never reads
    /// yourPartyRefused and never shows text, so the refusal line has to arrive separately
    /// through DisplayWargameMessage. yourPartyRefused belongs to the squad flow - for a duel
    /// there is no party, so it is always false.
    /// </summary>
    public class WargameChallengeRefusedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameChallengeRefused;

        public uint WargameId { get; set; }
        public bool YourPartyRefused { get; set; }

        public WargameChallengeRefusedPacket(uint wargameId, bool yourPartyRefused = false)
        {
            WargameId = wargameId;
            YourPartyRefused = yourPartyRefused;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteBool(YourPartyRefused);
        }
    }
}
