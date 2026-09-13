namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    // client/missionlog.pyo Recv_ObjectiveActivated(missionId, objectiveId)
    public class ObjectiveActivatedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveActivated;

        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }

        public ObjectiveActivatedPacket(uint missionId, uint objectiveId)
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
