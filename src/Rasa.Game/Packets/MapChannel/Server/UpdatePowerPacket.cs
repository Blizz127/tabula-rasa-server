namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdatePowerPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdatePower;

        public ActorAttributes Power { get; }
        public ulong WhoId { get; set; }

        public UpdatePowerPacket(ActorAttributes power, ulong whoId)
        {
            // Packets are queued: retain this update even if the actor changes again.
            Power = new ActorAttributes(power.AttributeId, power.NormalMax, power.CurrentMax,
                power.Current, power.RefreshAmount, power.RefreshPeriod);
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Power.Current);
            pw.WriteInt(Power.CurrentMax);
            pw.WriteInt(Power.RefreshAmount);
            pw.WriteULong(WhoId);
        }
    }
}
