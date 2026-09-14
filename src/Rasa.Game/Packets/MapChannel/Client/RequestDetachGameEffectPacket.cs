namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    /// <summary>
    /// client/augmentations/actor.py AttemptInterruptAction and ToggleCrouched:
    /// SendCallActorMethod('RequestDetachGameEffect', (effect.effectId,)). Sent for the
    /// player's GESTURE_EFFECT when they move, act or crouch out of a looping gesture.
    /// The effect id is the int the server sent in GameEffectAttached.
    /// </summary>
    public class RequestDetachGameEffectPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestDetachGameEffect;

        public int EffectId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 1 || pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            EffectId = pr.ReadInt();
        }
    }
}
