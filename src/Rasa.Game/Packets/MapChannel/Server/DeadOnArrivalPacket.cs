namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public sealed class DeadOnArrivalPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DeadOnArrival;
        public bool CanRevive { get; }

        public DeadOnArrivalPacket(bool canRevive)
        {
            CanRevive = canRevive;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(CanRevive ? 1 : 0);
        }
    }
}
