namespace Rasa.Packets.MapChannel.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;

    public class ActionReuseTimesPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionReuseTimes;
        public IReadOnlyList<(ActionId ActionId, long RemainingMilliseconds)> ReuseTimes { get; }

        public ActionReuseTimesPacket(IReadOnlyList<(ActionId ActionId, long RemainingMilliseconds)> reuseTimes)
        {
            ReuseTimes = reuseTimes;
        }

        public override void Write(PythonWriter pw)
        {
            // The original client keys reuse by action ID, shared across ranks.
            pw.WriteTuple(1);
            pw.WriteList(ReuseTimes.Count);
            foreach (var entry in ReuseTimes)
            {
                pw.WriteTuple(2);
                pw.WriteUInt((uint)entry.ActionId);
                pw.WriteLong(entry.RemainingMilliseconds);
            }
        }
    }
}
