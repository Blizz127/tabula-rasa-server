namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class LogoutTimeRemainingPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogoutTimeRemaining;

        public int TimeRemainingMilliseconds { get; }

        public LogoutTimeRemainingPacket(int timeRemainingMilliseconds = LogoutCountdown.DurationMilliseconds)
        {
            TimeRemainingMilliseconds = timeRemainingMilliseconds;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(TimeRemainingMilliseconds);
        }
    }
}
