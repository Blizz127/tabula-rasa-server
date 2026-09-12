namespace Rasa.Packets.MapChannel.Server.PerformRecovery
{
    using System;
    using System.Linq;
    using Data;
    using Memory;
    using Structures;

    public class WeaponAttackRecovery : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        private readonly ActionId _actionId;
        private readonly uint _actionArgId;
        private readonly ulong[] _hitEntities;
        private readonly (ulong EntityId, DamageInfoData Damage)[] _hits;

        public WeaponAttackRecovery(Missile missile)
        {
            if (missile == null)
                throw new ArgumentNullException(nameof(missile));
            _actionId = missile.ActionId;
            _actionArgId = missile.ActionArgId;
            _hitEntities = missile.Args.HitEntities.ToArray();
            _hits = missile.Args.HitData.Select(hit =>
                (hit.EntityId, DamageInfoData.FromHit(hit))).ToArray();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt((uint)_actionId);   // actionId
            pw.WriteUInt(_actionArgId);      // actionargId
            pw.WriteList(_hitEntities.Length);    // Hits
            foreach (var entity in _hitEntities)
                pw.WriteULong(entity);
            pw.WriteList(0);                // misses
            pw.WriteList(0);                // misses data
            pw.WriteList(_hits.Length);
            foreach (var hit in _hits)
            {
                pw.WriteTuple(3);
                pw.WriteULong(hit.EntityId);         // target entityid
                hit.Damage.Write(pw);
                pw.WriteTuple(1);                   // OnHitData
                pw.WriteList(0);
            }
        }
    }
}
