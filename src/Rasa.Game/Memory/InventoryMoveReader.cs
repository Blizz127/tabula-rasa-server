namespace Rasa.Memory
{
    using Packets.Protocol;

    internal static class InventoryMoveReader
    {
        public static (uint source, uint destination, int quantity) Read(PythonReader reader)
        {
            if (reader.PeekType() != PythonType.Tuple || reader.ReadTuple() != 3 || reader.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            var source = reader.ReadInt();
            if (reader.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            var destination = reader.ReadInt();
            var tag = reader.Reader.ReadByte();
            --reader.Reader.BaseStream.Position;
            if ((tag & 0xF0) == 0x20 && tag != 0x20 && tag != 0x2F)
                throw new InvalidClientMessageException();
            var quantity = reader.PeekType() switch
            {
                PythonType.Int => (long)reader.ReadInt(),
                PythonType.Long => reader.ReadLong(),
                _ => throw new InvalidClientMessageException()
            };
            if (source < 0 || destination < 0 || quantity < 0 || quantity > int.MaxValue)
                throw new InvalidClientMessageException();
            return ((uint)source, (uint)destination, (int)quantity);
        }
    }
}
