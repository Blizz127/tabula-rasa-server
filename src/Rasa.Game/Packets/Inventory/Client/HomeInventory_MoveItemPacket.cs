namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class HomeInventory_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.HomeInventory_MoveItem;

        public uint SrcSlot { get; set; }
        public uint DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            var move = InventoryMoveReader.Read(pr);
            SrcSlot = move.source;
            DestSlot = move.destination;
            Quantity = move.quantity;
        }
    }
}
