namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class TransferCreditToLockboxPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TransferCreditToLockbox;

        public int Ammount { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 1)
                throw new InvalidClientMessageException();
            var tag = pr.Reader.ReadByte();
            --pr.Reader.BaseStream.Position;
            if ((tag & 0xF0) == 0x20 && tag != 0x20 && tag != 0x2F)
                throw new InvalidClientMessageException();
            var amount = pr.PeekType() switch
            {
                PythonType.Int => (long)pr.ReadInt(),
                PythonType.Long => pr.ReadLong(),
                _ => throw new InvalidClientMessageException()
            };
            if (amount < int.MinValue || amount > int.MaxValue)
                throw new InvalidClientMessageException();
            Ammount = (int)amount;
        }
    }
}
