namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Memory;

    /// <summary>
    /// PerformRecovery whose hitdata is the list of entity ids itself: tacticalevasion.py DoAbility calls
    /// entitymanager.GetEntities(self._hitdata) and announces its mag flash on each.
    /// </summary>
    public class IdListRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public IReadOnlyList<ulong> Ids { get; }

        public IdListRecovery(ActionId actionId, uint actionArgId, IEnumerable<ulong> ids)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Ids = ids.ToList();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
            pw.WriteList(Ids.Count);
            foreach (var id in Ids)
                pw.WriteULong(id);
            pw.WriteList(0);
            pw.WriteList(0);
            pw.WriteList(Ids.Count);
            foreach (var id in Ids)
                pw.WriteULong(id);
        }
    }
}
