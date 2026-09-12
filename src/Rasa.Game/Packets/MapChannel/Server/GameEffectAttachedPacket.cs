namespace Rasa.Packets.MapChannel.Server
{
    using System;
    using Data;
    using Memory;

    public class GameEffectAttachedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectAttached;

        public int EffectTypeId { get; set; }
        public int EffectId { get; set; }
        public uint EffectLevel { get; set; }
        public ulong SourceId { get; set; }
        public bool Announced { get; set; }
        // tooltip
        public double? Duration { get; set; } // Seconds; absent for an open-ended tooltip.
        public int? DamageType { get; set; }
        public int? AttrId { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsBuff { get; set; }
        public bool? IsDebuff { get; set; }
        public bool? IsNegativeEffect { get; set; }
        public double[] EffectArguments { get; set; } = Array.Empty<double>();
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6 + EffectArguments.Length);
            pw.WriteInt(EffectTypeId);      //typeId
            pw.WriteInt(EffectId);          //effectId
            pw.WriteUInt(EffectLevel);       //level
            pw.WriteULong(SourceId);          //sourceId
            pw.WriteBool(Announced);        //announce
            var count = (Duration.HasValue ? 1 : 0) + (DamageType.HasValue ? 1 : 0) +
                (AttrId.HasValue ? 1 : 0) + (IsActive.HasValue ? 1 : 0) +
                (IsBuff.HasValue ? 1 : 0) + (IsDebuff.HasValue ? 1 : 0) +
                (IsNegativeEffect.HasValue ? 1 : 0);
            pw.WriteDictionary(count);
            if (Duration.HasValue) { pw.WriteString("duration"); pw.WriteDouble(Duration.Value); }
            if (DamageType.HasValue) { pw.WriteString("damageType"); pw.WriteInt(DamageType.Value); }
            if (AttrId.HasValue) { pw.WriteString("attrId"); pw.WriteInt(AttrId.Value); }
            if (IsActive.HasValue) { pw.WriteString("isActive"); pw.WriteBool(IsActive.Value); }
            if (IsBuff.HasValue) { pw.WriteString("isBuff"); pw.WriteBool(IsBuff.Value); }
            if (IsDebuff.HasValue) { pw.WriteString("isDebuff"); pw.WriteBool(IsDebuff.Value); }
            if (IsNegativeEffect.HasValue) { pw.WriteString("isNegativeEffect"); pw.WriteBool(IsNegativeEffect.Value); }
            // These are variadic arguments, not a nested list. Sprint expects one scalar.
            foreach (var argument in EffectArguments)
                pw.WriteDouble(argument);
        }
    }
}
