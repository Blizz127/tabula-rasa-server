using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Data;
using Rasa.Memory;

namespace Rasa.Structures
{
    // shared/damageinfo.py DamageInfo.ClientInfo's twelve-field snapshot.
    public sealed class DamageInfoData
    {
        public DamageType DamageType { get; }
        public uint Reflected { get; }
        public uint Filtered { get; }
        public uint Absorbed { get; }
        public uint Resisted { get; }
        public long FinalAmount { get; }
        public int IsCritical { get; }
        public int DeathBlow { get; }
        public uint CoverModifier { get; }
        public int WasImmune { get; }
        // Despite their original names, AnnounceGameEffectAttach consumes type IDs.
        public IReadOnlyList<uint> TargetEffectIds { get; }
        public IReadOnlyList<uint> SourceEffectIds { get; }

        private DamageInfoData(HitData hit)
        {
            DamageType = hit.DamageType;
            Reflected = hit.Reflected;
            Filtered = hit.Filtered;
            Absorbed = hit.Absorbed;
            Resisted = hit.Resisted;
            FinalAmount = hit.FinalAmt;
            IsCritical = hit.IsCritical;
            DeathBlow = hit.DeathBlow;
            CoverModifier = hit.CoverModifier;
            WasImmune = hit.WasImune;
            TargetEffectIds = Array.AsReadOnly(hit.TargetEffectIds?.ToArray() ?? Array.Empty<uint>());
            SourceEffectIds = Array.AsReadOnly(hit.SourceEffectIds?.ToArray() ?? Array.Empty<uint>());
        }

        public static DamageInfoData FromHit(HitData hit)
        {
            if (hit == null)
                throw new ArgumentNullException(nameof(hit));
            return new DamageInfoData(hit);
        }

        public void Write(PythonWriter writer)
        {
            writer.WriteTuple(12);
            writer.WriteUInt((uint)DamageType);
            writer.WriteUInt(Reflected);
            writer.WriteUInt(Filtered);
            writer.WriteUInt(Absorbed);
            writer.WriteUInt(Resisted);
            writer.WriteLong(FinalAmount);
            writer.WriteInt(IsCritical);
            writer.WriteInt(DeathBlow);
            writer.WriteUInt(CoverModifier);
            writer.WriteInt(WasImmune);
            writer.WriteList(TargetEffectIds.Count);
            foreach (var typeId in TargetEffectIds)
                writer.WriteUInt(typeId);
            writer.WriteList(SourceEffectIds.Count);
            foreach (var typeId in SourceEffectIds)
                writer.WriteUInt(typeId);
        }
    }
}
