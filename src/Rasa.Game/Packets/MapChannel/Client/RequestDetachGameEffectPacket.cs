namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

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
