namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Memory;

    /// <summary>
    /// PerformRecovery for Cure: cure.py DoAbility unpacks its hitdata as (revivedIds, guardedIds), announces the revive
    /// on the first and the debuff guard on the second.
    /// </summary>
    public class CureRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public IReadOnlyList<ulong> Reached { get; }
        public IReadOnlyList<ulong> Revived { get; }
        public IReadOnlyList<ulong> Guarded { get; }

        public CureRecovery(ActionId actionId, uint actionArgId, IEnumerable<ulong> reached, IEnumerable<ulong> revived, IEnumerable<ulong> guarded)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Reached = reached.ToList();
            Revived = revived.ToList();
            Guarded = guarded.ToList();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
            pw.WriteList(Reached.Count);
            foreach (var id in Reached)
                pw.WriteULong(id);
            pw.WriteList(0);
            pw.WriteList(0);
            pw.WriteTuple(2);                   // hitdata = (revivedIds, guardedIds)
            pw.WriteList(Revived.Count);
            foreach (var id in Revived)
                pw.WriteULong(id);
            pw.WriteList(Guarded.Count);
            foreach (var id in Guarded)
                pw.WriteULong(id);
        }
    }
}
