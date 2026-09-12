namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestEquipWeaponPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestEquipWeapon;

        public uint SrcSlot { get; set; }
        public InventoryType InventoryType { get; set; }
        public uint DestSlot { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 3 || pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            SrcSlot = pr.ReadUInt();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            InventoryType = (InventoryType)pr.ReadInt();
            if (pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            DestSlot = pr.ReadUInt();
        }
    }
}
