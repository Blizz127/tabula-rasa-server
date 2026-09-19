namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/augmentations/usable.py Recv_LockToActor(actorId): the usable is in use by that actor (IsAlreadyInUse
    /// for everyone else, and the usable window's "locked" text); 0 releases it and removes the actor's effect.
    /// </summary>
    public class LockToActorPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockToActor;

        public ulong ActorId { get; }

        public LockToActorPacket(ulong actorId) => ActorId = actorId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(ActorId);
        }
    }

    /// <summary>
    /// Recv_UseInterruptible(actorId, *args): the locked actor has started an interruptible use; the client plays the
    /// usable's interruptible effect (usabledata.specialFX (class, state, state)). Ignored unless LockToActor named
    /// that actor first.
    /// </summary>
    public class UseInterruptiblePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UseInterruptible;

        public ulong ActorId { get; }

        public UseInterruptiblePacket(ulong actorId) => ActorId = actorId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(ActorId);
        }
    }

    /// <summary>Recv_UseInterrupted(actorId): the use was interrupted; the client removes the actor's effect.</summary>
    public class UseInterruptedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UseInterrupted;

        public ulong ActorId { get; }

        public UseInterruptedPacket(ulong actorId) => ActorId = actorId;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(ActorId);
        }
    }
}
