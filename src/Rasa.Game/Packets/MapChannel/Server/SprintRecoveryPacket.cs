namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SprintRecoveryPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;
        public uint Rank { get; }
        public ulong TargetId { get; }

        public SprintRecoveryPacket(uint rank, ulong targetId)
        {
            Rank = rank;
            TargetId = targetId;
        }

        public override void Write(PythonWriter pw)
        {
            // TargetedAction.DoAction receives four collections. Sprint's self hit
            // announces its attached effect; its DoAbility hook consumes no hit data.
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId.AaRecruitSprint);
            pw.WriteUInt(Rank);
            pw.WriteList(1);
            pw.WriteULong(TargetId);
            pw.WriteList(0); // misses
            pw.WriteList(0); // missdata
            pw.WriteList(0); // hitdata: no damage payload for Sprint
        }
    }
}
