using System.Collections.Generic;
using Rasa.Memory;

namespace Rasa.Structures
{
    // ArcEffect.OnAnnounceAttach and StormEffect.OnTick receive the same pair.
    public sealed class LightningArcHit
    {
        public ulong TargetEntityId { get; }
        public DamageInfoData Damage { get; }

        public LightningArcHit(ulong targetEntityId, HitData damage)
        {
            TargetEntityId = targetEntityId;
            Damage = DamageInfoData.FromHit(damage);
        }

        public void Write(PythonWriter writer)
        {
            writer.WriteTuple(2);
            writer.WriteULong(TargetEntityId);
            Damage.Write(writer);
        }

        public static void WriteList(PythonWriter writer, IReadOnlyList<LightningArcHit> hits)
        {
            writer.WriteList(hits.Count);
            foreach (var hit in hits)
                hit.Write(writer);
        }
    }
}
