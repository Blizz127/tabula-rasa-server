using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ActorInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActorInfo;

        public List<CharacterState> StateIds = new List<CharacterState>();
        public ulong TrackingTarget { get; set; }
        public double Yaw { get; set; }
        public double MovementMode { get; set; }
        /// <summary>
        /// A posture state id, not a state type: the client's Recv_ActorInfo looks it up with GetStateFromId and acts
        /// only when it is CROUCHED (14). The server tracks no posture apart from the actor's state, so an actor is
        /// crouched when that state says so and standing otherwise.
        /// </summary>
        public CharacterState DesiredPostureId { get; set; }
        public bool CombatMode { get; set; }

        public ActorInfoPacket(Actor actor)
        {
            StateIds.Add(actor.State);
            TrackingTarget = actor.Target;
            Yaw = actor.Rotation;
            MovementMode = actor.MovementSpeed;
            DesiredPostureId = actor.State == CharacterState.Crouched ? CharacterState.Crouched : CharacterState.Standing;
            CombatMode = actor.InCombatMode;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteList(StateIds.Count);
            foreach (var state in StateIds)
                pw.WriteInt((int)state);            // stateIds
            pw.WriteDouble(Yaw);                    // yaw
            pw.WriteULong(TrackingTarget);          // trackingTarget
            pw.WriteDouble(MovementMode);           // movementMod
            pw.WriteInt((int)DesiredPostureId);     // desiredPostureId
            pw.WriteBool(CombatMode);               // isHoldingCombatMode
        }
    }
}
