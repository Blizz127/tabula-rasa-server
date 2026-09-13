namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    // client/missionlog.pyo CompleteNPCMission: (npcId, missionId, selectionIdx, rating).
    // selectionIdx is None or the 0-based reward choice; rating is None unless
    // the (disabled) mission-rating UI is on. Neither is a boolean.
    public class CompleteNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteNPCMission;

        public ulong EntityId { get; set; }          // npcId
        public uint MissionId { get; set; }          // missionId
        public int? SelectionIdx { get; set; }       // selectionIdx
        public int? Rating { get; set; }             // rating

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadULong();
            MissionId = pr.ReadUInt();
            SelectionIdx = ReadOptionalInt(pr);
            Rating = ReadOptionalInt(pr);
        }

        internal static int? ReadOptionalInt(PythonReader pr)
        {
            if (pr.PeekType() == PythonType.Structs)
            {
                pr.ReadNoneStruct();
                return null;
            }

            return pr.ReadInt();
        }
    }
}
