namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class UpdateHealthPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateHealth;

        public ActorAttributes Health { get; }
        public ulong WhoId { get; set; }

        public UpdateHealthPacket(ActorAttributes health, ulong whoId)
        {
            // The queued update must survive later damage, death or regeneration.
            Health = new ActorAttributes(health.AttributeId, health.NormalMax, health.CurrentMax,
                health.Current, health.RefreshAmount, health.RefreshPeriod);
            WhoId = whoId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteInt(Health.Current);
            pw.WriteInt(Health.CurrentMax);
            pw.WriteInt(Health.RefreshAmount);
            pw.WriteULong(WhoId);
        }
    }
}
