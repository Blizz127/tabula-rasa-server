namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_ObjectiveFailed(missionId, objectiveId)
    public class ObjectiveFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveFailed;

        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }

        public ObjectiveFailedPacket(uint missionId, uint objectiveId)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
        }
    }
}
