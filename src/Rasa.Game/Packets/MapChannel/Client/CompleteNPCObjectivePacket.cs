namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // client/missionlog.pyo CompleteNPCObjective: (npcId, missionId, objectiveId, playerFlagId),
    // sent when the Continue button of an objective-completion conversation is pressed.
    public class CompleteNPCObjectivePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteNPCObjective;

        public ulong EntityId { get; set; }
        public uint MissionId { get; set; }
        public uint ObjectiveId { get; set; }
        public uint PlayerFlagId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            ObjectiveId = pr.ReadUInt();
            PlayerFlagId = pr.ReadUInt();
        }
    }
}
