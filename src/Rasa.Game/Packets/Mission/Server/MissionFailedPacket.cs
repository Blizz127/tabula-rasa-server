namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_MissionFailed(missionId)
    public class MissionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionFailed;

        public uint MissionId { get; set; }

        public MissionFailedPacket(uint missionId)
        {
            MissionId = missionId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(MissionId);
        }
    }
}
