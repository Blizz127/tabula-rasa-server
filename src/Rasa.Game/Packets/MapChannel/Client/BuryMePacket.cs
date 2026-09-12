namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public sealed class BuryMePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.BuryMe;

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 0)
                throw new InvalidClientMessageException();
        }
    }
}
