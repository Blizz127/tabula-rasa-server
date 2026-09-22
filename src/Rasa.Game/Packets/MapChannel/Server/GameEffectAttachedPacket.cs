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
        /// <summary>Further tooltip values, named as the effect's tooltip template names them (dmgMin, interval, dmgMod).</summary>
        public System.Collections.Generic.Dictionary<string, double> TooltipValues { get; set; } = new System.Collections.Generic.Dictionary<string, double>();
        
        public override void Write(PythonWriter pw)
        {
            WriteEffectData(pw, true);
        }

        /// <summary>
        /// The same announcement with a different tooltip countdown, for a catch-up send: the client reads the
        /// tooltip's duration as an offset from the moment it arrives, not from when the effect began
        /// (BaseGameEffect.SetTooltipDict: __expireTime = gameclient.Time() + duration, first=482).
        /// </summary>
        public GameEffectAttachedPacket WithDuration(double? duration)
        {
            var copy = (GameEffectAttachedPacket)MemberwiseClone();
            copy.Duration = duration;
            return copy;
        }

        /// <summary>
        /// One effect, as both this packet and an element of the bulk GameEffects catch-up write it. The bulk form
        /// is this tuple minus the announce flag, because its elements go straight to AttachGameEffect(typeId,
        /// effectId, level, sourceId, tooltipDict, *args) while Recv_GameEffectAttached takes announce in between
        /// and drops it before making the same call.
        /// (verify/dis/trpython-client-physicalentity.pyo.dis :: Recv_GameEffectAttached first=601 args=('self',
        ///  'typeId', 'effectId', 'level', 'sourceId', 'announce', 'tooltipDict'); AttachGameEffect first=488)
        /// </summary>
        public void WriteEffectData(PythonWriter pw, bool includeAnnounce)
        {
            pw.WriteTuple((includeAnnounce ? 6 : 5) + EffectArguments.Length);
            pw.WriteInt(EffectTypeId);      //typeId
            pw.WriteInt(EffectId);          //effectId
            pw.WriteUInt(EffectLevel);       //level
            pw.WriteULong(SourceId);          //sourceId
            if (includeAnnounce)
                pw.WriteBool(Announced);    //announce
            var count = (Duration.HasValue ? 1 : 0) + (DamageType.HasValue ? 1 : 0) +
                (AttrId.HasValue ? 1 : 0) + (IsActive.HasValue ? 1 : 0) +
                (IsBuff.HasValue ? 1 : 0) + (IsDebuff.HasValue ? 1 : 0) +
                (IsNegativeEffect.HasValue ? 1 : 0) + TooltipValues.Count;
            pw.WriteDictionary(count);
            if (Duration.HasValue) { pw.WriteString("duration"); pw.WriteDouble(Duration.Value); }
            if (DamageType.HasValue) { pw.WriteString("damageType"); pw.WriteInt(DamageType.Value); }
            if (AttrId.HasValue) { pw.WriteString("attrId"); pw.WriteInt(AttrId.Value); }
            if (IsActive.HasValue) { pw.WriteString("isActive"); pw.WriteBool(IsActive.Value); }
            if (IsBuff.HasValue) { pw.WriteString("isBuff"); pw.WriteBool(IsBuff.Value); }
            if (IsDebuff.HasValue) { pw.WriteString("isDebuff"); pw.WriteBool(IsDebuff.Value); }
            if (IsNegativeEffect.HasValue) { pw.WriteString("isNegativeEffect"); pw.WriteBool(IsNegativeEffect.Value); }
            foreach (var (name, value) in TooltipValues)
            {
                pw.WriteString(name);
                if (value == Math.Floor(value) && Math.Abs(value) < int.MaxValue)
                    pw.WriteInt((int)value);
                else
                    pw.WriteDouble(value);
            }
            // These are variadic arguments, not a nested list. Sprint expects one scalar.
            foreach (var argument in EffectArguments)
                pw.WriteDouble(argument);
        }
    }
}
