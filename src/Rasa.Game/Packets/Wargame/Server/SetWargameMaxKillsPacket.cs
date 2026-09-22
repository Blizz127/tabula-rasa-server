namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_SetWargameMaxKills(wargameId, maxKills), opcode 631, on entity 23.
    /// Stores maxKills on the status and refreshes the tracker, which shows the score against it.
    /// The client never ends a wargame on its own when the count is reached; the server does.
    /// </summary>
    public class SetWargameMaxKillsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetWargameMaxKills;

        public uint WargameId { get; set; }
        public int MaxKills { get; set; }

        public SetWargameMaxKillsPacket(uint wargameId, int maxKills)
        {
            WargameId = wargameId;
            MaxKills = maxKills;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteInt(MaxKills);
        }
    }
}
