namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Memory;

    /// <summary>
    /// PerformRecovery whose hitdata is (entityId, effectTypeId) per entity - Reconstruction's DoAbility
    /// (client/actions/abilities/reconstruction.py) announces that effect on each: HELP on the squad, HARM on enemies.
    /// </summary>
    public class EffectListRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public IReadOnlyList<(ulong EntityId, int EffectTypeId)> Entries { get; }

        public EffectListRecovery(ActionId actionId, uint actionArgId, IEnumerable<(ulong EntityId, int EffectTypeId)> entries)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            Entries = entries.ToList();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
            pw.WriteList(Entries.Count);
            foreach (var (entityId, _) in Entries)
                pw.WriteULong(entityId);
            pw.WriteList(0);
            pw.WriteList(0);
            pw.WriteList(Entries.Count);
            foreach (var (entityId, effectTypeId) in Entries)
            {
                pw.WriteTuple(2);
                pw.WriteULong(entityId);
                pw.WriteInt(effectTypeId);
            }
        }
    }
}
