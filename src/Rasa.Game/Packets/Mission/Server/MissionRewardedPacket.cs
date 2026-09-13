namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_MissionRewarded(missionId)
    public class MissionRewardedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionRewarded;

        public uint MissionId { get; set; }

        public MissionRewardedPacket(uint missionId)
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
