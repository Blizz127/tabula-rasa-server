namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.Recv_SetKillStreak(count): drives the experience bar's streak display
    /// (ID_HUD_KILLSTREAK_X2..X6) and, when count is above zero, the Kill Streak tutorial.
    /// </summary>
    public sealed class SetKillStreakPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetKillStreak;

        public int Count { get; }

        public SetKillStreakPacket(int count)
        {
            Count = count;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Count);
        }
    }
}
