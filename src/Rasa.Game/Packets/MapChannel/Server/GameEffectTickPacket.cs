namespace Rasa.Packets.MapChannel.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;
    using Structures;

    /// <summary>
    /// client/physicalentity.py Recv_GameEffectTick(effectId, *args) calls the attached effect's Tick(entity, *args).
    /// A DamageOverTime effect (Ruin's DecayEffect) reads one argument, damageData: a list of (targetId, rawInfo) whose
    /// rawInfo is the damage record a weapon hit carries. Rage's RageSourceEffect reads targetIds: the entities it
    /// buffed this pulse.
    /// </summary>
    public class GameEffectTickPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffectTick;

        public int EffectId { get; }
        public IReadOnlyList<(ulong TargetId, HitData Hit)> Damage { get; }
        public IReadOnlyList<ulong> TargetIds { get; }

        public GameEffectTickPacket(int effectId, IReadOnlyList<(ulong TargetId, HitData Hit)> damage)
        {
            EffectId = effectId;
            Damage = damage;
        }

        public GameEffectTickPacket(int effectId, IReadOnlyList<ulong> targetIds)
        {
            EffectId = effectId;
            TargetIds = targetIds;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(EffectId);
            if (Damage != null)
            {
                pw.WriteList(Damage.Count);
                foreach (var (targetId, hit) in Damage)
                {
                    pw.WriteTuple(2);
                    pw.WriteULong(targetId);
                    DamageInfoData.FromHit(hit).Write(pw);
                }
            }
            else
            {
                pw.WriteList(TargetIds.Count);
                foreach (var id in TargetIds)
                    pw.WriteULong(id);
            }
        }
    }
}
