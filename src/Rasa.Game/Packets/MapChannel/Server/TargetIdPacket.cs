namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// What this actor is aiming at. Actor.Recv_TargetId hands straight to SetTargetId, and both subclasses
    /// override it: Creature looks its classId up in generated/client/bonetracking.creaturedata, adds that bone
    /// tracker to the body and calls body.SyncBoneTrackerToTarget(trackerId, connectionpoint.DAMAGE1, targetId),
    /// so the weapon bone physically follows the target; Manifestation calls UpdateAccuracyRates and
    /// UpdateBoneTracking(bForce = True). Nothing in the shipped UI subscribes to the ACTOR_TARGET_CHANGED that
    /// the base implementation posts, so the aiming is the whole visible effect.
    /// (verify/dis/trpython-client-augmentations-actor.pyo.dis :: Actor.Recv_TargetId first=3247, SetTargetId
    ///  first=3251; trpython-client-augmentations-creature.pyo.dis :: Creature.SetTargetId first=186;
    ///  trpython-client-augmentations-manifestation.pyo.dis :: Manifestation.SetTargetId first=261)
    ///
    /// No target is None, not 0. Both overrides branch on `targetId is not None`, and only the None branch
    /// removes the tracker - a 0 would leave the weapon synced to an entity that does not exist.
    /// </summary>
    public sealed class TargetIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TargetId;

        /// <summary>The entity aimed at, or 0 for no target, which goes out as None.</summary>
        public ulong TargetEntityId { get; }

        public TargetIdPacket(ulong targetEntityId)
        {
            TargetEntityId = targetEntityId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);

            if (TargetEntityId == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteULong(TargetEntityId);
        }
    }
}
