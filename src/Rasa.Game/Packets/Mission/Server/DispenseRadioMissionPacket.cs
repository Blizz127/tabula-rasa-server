namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// missionlog.Recv_DispenseRadioMission(missionId, missionInfo, bForceDialog). A forced offer opens the
    /// "Headquarters" conversation window at once and cannot be declined; closing it accepts
    /// (conversationwindow.Hide → OnAcceptMissionBtn → AssignRadioMission). An unforced offer only adds a
    /// status-updater indicator.
    /// </summary>
    public class DispenseRadioMissionPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DispenseRadioMission;

        public uint MissionId { get; }
        public MissionInfo MissionInfo { get; }
        public bool Forced { get; }

        public DispenseRadioMissionPacket(uint missionId, MissionInfo missionInfo, bool forced)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
            Forced = forced;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MissionId);
            MissionOfferWriter.Write(pw, MissionInfo);
            pw.WriteBool(Forced);
        }
    }
}
