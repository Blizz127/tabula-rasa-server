namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;
    using Protocol;

    /// <summary>
    /// client/augmentations/clonecredit.pyo CloneCredit.InventoryUse (line 19, 1.16.5.0, sha256 f55ce8c8...):
    /// SendCallActorMethod('RequestUseCloneCredit', (self.entityId,)) - the right-click "Use" of a Clone Credit item.
    /// Entity ids reach the client as Python longs, so the one argument is a long.
    /// </summary>
    public class RequestUseCloneCreditPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestUseCloneCredit;

        public ulong EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.PeekType() != PythonType.Tuple || pr.ReadTuple() != 1 || pr.PeekType() != PythonType.Long)
                throw new InvalidClientMessageException();
            EntityId = pr.ReadULong();
        }
    }
}
