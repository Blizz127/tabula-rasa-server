namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // missionlog.AssignRadioMission: SendCallUserMethod('AssignRadioMission', (missionId,))
    public class AssignRadioMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignRadioMission;

        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            MissionId = (uint)pr.ReadInt();
        }
    }
}
