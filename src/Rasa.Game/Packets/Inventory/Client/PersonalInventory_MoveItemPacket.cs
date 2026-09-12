namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;

    public class PersonalInventory_MoveItemPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PersonalInventory_MoveItem;

        public int SrcSlot { get; set; }
        public int DestSlot { get; set; }
        public int Quantity { get; set; }

        public override void Read(PythonReader pr)
        {
            var move = InventoryMoveReader.Read(pr);
            SrcSlot = (int)move.source;
            DestSlot = (int)move.destination;
            Quantity = move.quantity;
        }
    }
}
