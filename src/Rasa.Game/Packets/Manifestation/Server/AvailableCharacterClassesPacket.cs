using System.Collections.Generic;

namespace Rasa.Packets.Manifestation.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// manifestation.Recv_AvailableCharacterClasses(classIds): a non-empty list raises "Tier Selection
    /// Available - See Trainer"; TierAdvancementInfo carries the same list silently.
    /// </summary>
    public class AvailableCharacterClassesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; }

        public IReadOnlyList<uint> ClassIds { get; }

        public AvailableCharacterClassesPacket(IReadOnlyList<uint> classIds, bool silent = false)
        {
            ClassIds = classIds;
            Opcode = silent ? GameOpcode.TierAdvancementInfo : GameOpcode.AvailableCharacterClasses;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(ClassIds.Count);
            foreach (var classId in ClassIds)
                pw.WriteUInt(classId);
        }
    }
}
