namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // client/missionlog.pyo RewardNPCMission: (npcId, missionId, selectionIdx, rating)
    public class RewardNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RewardNPCMission;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public int? SelectionIdx { get; set; }
        public int? Rating { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            SelectionIdx = CompleteNPCMissionPacket.ReadOptionalInt(pr);
            Rating = CompleteNPCMissionPacket.ReadOptionalInt(pr);
        }
    }
}
