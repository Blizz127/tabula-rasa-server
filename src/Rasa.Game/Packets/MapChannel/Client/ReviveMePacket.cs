namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public sealed class ReviveMePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ReviveMe;
        public int? GraveyardId { get; private set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 1)
                throw new InvalidClientMessageException();

            if (pr.PeekType() == PythonType.Int)
                GraveyardId = pr.ReadInt();
            else if (pr.PeekType() == PythonType.Structs && pr.ReadUnkStruct() == PythonStruct.None)
                GraveyardId = null;
            else
                throw new InvalidClientMessageException();
        }
    }
}
