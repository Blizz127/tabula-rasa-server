namespace Rasa.Packets.MapChannel.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// client/physicalentity.py Recv_CallGameEffectMethod(effectId, methodName, args) calls Recv_&lt;methodName&gt;(entity,
    /// *args) on the attached effect. BaseGameEffect.Recv_AnnounceDamage(effectTarget, damageData) takes damageData as a
    /// list of (targetId, rawInfo) and announces each hit; an effect with tickOnDamage (Scourge's) also marks the tick.
    /// </summary>
    public class CallGameEffectMethodPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CallGameEffectMethod;

        public int EffectId { get; }
        public string MethodName { get; }
        public IReadOnlyList<(ulong TargetId, HitData Hit)> Damage { get; private set; }
        public IReadOnlyList<(ulong EntityId, int Amount)> Healing { get; private set; }
        private readonly System.Action<PythonWriter> _writeArgs;

        public static CallGameEffectMethodPacket AnnounceDamage(int effectId, IReadOnlyList<(ulong TargetId, HitData Hit)> damage)
            => new CallGameEffectMethodPacket(effectId, "AnnounceDamage", pw =>
            {
                pw.WriteTuple(1);
                pw.WriteList(damage.Count);
                foreach (var (targetId, hit) in damage)
                {
                    pw.WriteTuple(2);
                    pw.WriteULong(targetId);
                    DamageInfoData.FromHit(hit).Write(pw);
                }
            }) { Damage = damage };

        /// <summary>ConversionEffect.Recv_AnnounceHealing(effectTarget, healData): healData is a list of (entityId, healAmount).</summary>
        public static CallGameEffectMethodPacket AnnounceHealing(int effectId, IReadOnlyList<(ulong EntityId, int Amount)> healing)
            => new CallGameEffectMethodPacket(effectId, "AnnounceHealing", pw =>
            {
                pw.WriteTuple(1);
                pw.WriteList(healing.Count);
                foreach (var (entityId, amount) in healing)
                {
                    pw.WriteTuple(2);
                    pw.WriteULong(entityId);
                    pw.WriteInt(amount);
                }
            }) { Healing = healing };

        /// <summary>ReflectionEffect.Recv_AnnounceReflect(effectTarget, entityId, rawInfo, delayMs).</summary>
        public static CallGameEffectMethodPacket AnnounceReflect(int effectId, ulong entityId, HitData hit, int delayMs)
            => new CallGameEffectMethodPacket(effectId, "AnnounceReflect", pw =>
            {
                pw.WriteTuple(3);
                pw.WriteULong(entityId);
                DamageInfoData.FromHit(hit).Write(pw);
                pw.WriteInt(delayMs);
            }) { Damage = new[] { (entityId, hit) } };

        private CallGameEffectMethodPacket(int effectId, string methodName, System.Action<PythonWriter> writeArgs)
        {
            EffectId = effectId;
            MethodName = methodName;
            _writeArgs = writeArgs;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString(MethodName);
            _writeArgs(pw);
        }
    }
}
