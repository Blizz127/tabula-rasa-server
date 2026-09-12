namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class PersonalInventory_DestroyItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PersonalInventory_DestroyItem;

        public ulong EntityId { get; set; }
        public uint Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 2 || pr.PeekType() != PythonType.Long)
                throw new InvalidClientMessageException();
            EntityId = pr.ReadULong();
            var quantity = pr.PeekType() switch
            {
                PythonType.Int => (long)pr.ReadInt(),
                PythonType.Long => pr.ReadLong(),
                _ => throw new InvalidClientMessageException()
            };
            if (quantity < 0 || quantity > uint.MaxValue)
                throw new InvalidClientMessageException();
            Quantity = (uint)quantity;
        }
    }
}
