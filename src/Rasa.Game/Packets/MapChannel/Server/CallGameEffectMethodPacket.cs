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
        public IReadOnlyList<(ulong TargetId, HitData Hit)> Damage { get; }

        public static CallGameEffectMethodPacket AnnounceDamage(int effectId, IReadOnlyList<(ulong TargetId, HitData Hit)> damage)
            => new CallGameEffectMethodPacket(effectId, "AnnounceDamage", damage);

        private CallGameEffectMethodPacket(int effectId, string methodName, IReadOnlyList<(ulong TargetId, HitData Hit)> damage)
        {
            EffectId = effectId;
            MethodName = methodName;
            Damage = damage;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(EffectId);
            pw.WriteString(MethodName);
            pw.WriteTuple(1);                   // args
            pw.WriteList(Damage.Count);         // damageData
            foreach (var (targetId, hit) in Damage)
            {
                pw.WriteTuple(2);
                pw.WriteULong(targetId);
                DamageInfoData.FromHit(hit).Write(pw);
            }
        }
    }
}
