namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestEquipArmorPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipArmor;

        public uint SrcSlot { get; set; }        // Source Slot
        public InventoryType SrcInventory { get; set; }   // Source Inventory
        public uint DestSlot { get; set; }       // Destination Slot

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 3 || pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            SrcSlot = pr.ReadUInt();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            SrcInventory = (InventoryType)pr.ReadInt();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            DestSlot = pr.ReadUInt();
        }
    }
}
