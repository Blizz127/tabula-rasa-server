namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UpdateHitPointsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateHitPoints;

        public uint CurrentHitPoints { get; set; }

        public UpdateHitPointsPacket(uint currentHitPoints)
        {
            CurrentHitPoints = currentHitPoints;
        }

        // client/augmentations/usable.pyo Recv_UpdateHitPoints(curHitPoints)
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(CurrentHitPoints);
        }
    }
}
