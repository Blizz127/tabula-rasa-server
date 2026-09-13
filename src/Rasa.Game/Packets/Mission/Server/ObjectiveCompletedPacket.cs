namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_ObjectiveCompleted(missionId, objectiveId)
    public class ObjectiveCompletedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveCompleted;

        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }

        public ObjectiveCompletedPacket(uint missionId, uint objectiveId)
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
