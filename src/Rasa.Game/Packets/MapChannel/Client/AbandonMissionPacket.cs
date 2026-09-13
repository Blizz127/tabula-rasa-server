namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // client/missionlog.pyo AbandonMission: (missionId,)
    public class AbandonMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AbandonMission;

        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            MissionId = pr.ReadUInt();
        }
    }
}
