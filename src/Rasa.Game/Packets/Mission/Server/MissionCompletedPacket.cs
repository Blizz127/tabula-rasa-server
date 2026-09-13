namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_MissionCompleted(missionId)
    public class MissionCompletedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCompleted;

        public uint MissionId { get; set; }

        public MissionCompletedPacket(uint missionId)
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
