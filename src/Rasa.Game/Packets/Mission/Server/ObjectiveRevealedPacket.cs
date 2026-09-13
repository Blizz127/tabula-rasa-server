namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    // client/missionlog.pyo Recv_ObjectiveRevealed(missionId, objectiveId, missionInfo):
    // the client replaces its whole entry with missionInfo, so the full log entry is sent.
    public class ObjectiveRevealedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveRevealed;

        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public ObjectiveRevealedPacket(uint missionId, uint objectiveId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            MissionInfo = missionInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
            pw.WriteStruct(MissionInfo);
        }
    }
}
