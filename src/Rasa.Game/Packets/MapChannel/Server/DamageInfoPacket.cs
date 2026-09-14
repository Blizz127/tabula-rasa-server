namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class DamageInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DamageInfo;

        public bool CanBeDamaged { get; set; }
        public bool CanBeRepaired { get; set; }
        public uint TotalHitPoints { get; set; }
        public uint CurrentHitPoints { get; set; }

        public DamageInfoPacket(bool canBeDamaged, bool canBeRepaired, uint totalHitPoints, uint currentHitPoints)
        {
            CanBeDamaged = canBeDamaged;
            CanBeRepaired = canBeRepaired;
            TotalHitPoints = totalHitPoints;
            CurrentHitPoints = currentHitPoints;
        }

        // client/augmentations/usable.pyo Recv_DamageInfo(canBeDamaged, canBeRepaired, totHitPoints, curHitPoints)
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteBool(CanBeDamaged);
            pw.WriteBool(CanBeRepaired);
            pw.WriteUInt(TotalHitPoints);
            pw.WriteUInt(CurrentHitPoints);
        }
    }
}
