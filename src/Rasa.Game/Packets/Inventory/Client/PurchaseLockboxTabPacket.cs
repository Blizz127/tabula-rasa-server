namespace Rasa.Packets.Inventory.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class PurchaseLockboxTabPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PurchaseLockboxTab;

        public int TabId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 1 || pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            TabId = pr.ReadInt();
        }
    }
}
