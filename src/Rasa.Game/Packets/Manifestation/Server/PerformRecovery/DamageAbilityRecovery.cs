namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// PerformRecovery for a damage ability other than Lightning: (actionId, actionArgId, hits, misses, missdata,
    /// hitdata). client/actions/abilities/damagebase.py DoAbility indexes hitdata with the position in hits and
    /// unpacks each entry as (rawInfo, data); rawInfo is the same damage record a weapon hit carries, and data goes
    /// to OnAbility, which none of the DamageBase modules read - so it is None, where Lightning sends its arcs.
    /// </summary>
    public class DamageAbilityRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public ActionId ActionId { get; }
        public uint ActionArgId { get; }
        public IReadOnlyList<ulong> HitEntities { get; }
        public IReadOnlyList<HitData> Hits { get; }

        public DamageAbilityRecovery(ActionId actionId, uint actionArgId, IEnumerable<ulong> hitEntities, IEnumerable<HitData> hits)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
            HitEntities = hitEntities.ToList();
            Hits = hits.ToList();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
            pw.WriteList(HitEntities.Count);
            foreach (var entity in HitEntities)
                pw.WriteULong(entity);
            pw.WriteList(0);                    // misses
            pw.WriteList(0);                    // missdata
            pw.WriteList(Hits.Count);
            foreach (var hit in Hits)
            {
                pw.WriteTuple(2);
                DamageInfoData.FromHit(hit).Write(pw);
                pw.WriteNoneStruct();           // data, read by no DamageBase module
            }
        }
    }
}
