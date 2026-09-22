namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_DisplayWargameTimer(wargameId, timeMs), opcode 628, on entity 23.
    ///
    /// timeMs is a remaining duration, not a deadline: the handler computes
    /// <c>status.endTime = gameclient.Time() + int(timeMs / 1000)</c> and then refreshes the
    /// tracker, whose GetRemainingTime is endTime - Time(). Dividing before adding means the
    /// client keeps whole seconds only, so anything below 1000 ms reads as no time left.
    /// </summary>
    public class DisplayWargameTimerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayWargameTimer;

        public uint WargameId { get; set; }
        public int TimeMs { get; set; }

        public DisplayWargameTimerPacket(uint wargameId, int timeMs)
        {
            WargameId = wargameId;
            TimeMs = timeMs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteInt(TimeMs);
        }
    }
}
