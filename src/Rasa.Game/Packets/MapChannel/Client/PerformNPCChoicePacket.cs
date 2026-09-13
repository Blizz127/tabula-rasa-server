namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // client/missionlog.pyo PerformNPCChoice: (npcId, missionId, objectiveId, playerFlagId, choiceIdx)
    public class PerformNPCChoicePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformNPCChoice;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public uint PlayerFlagId { get; set; }
        public int ChoiceIdx { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            ObjectiveId = pr.ReadUInt();
            PlayerFlagId = pr.ReadUInt();
            ChoiceIdx = pr.ReadInt();
        }
    }
}
