namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// SetOwnerId (884), called on an OwnableControlPoint entity (augmentation 82):
    /// client/augmentations/ownablecontrolpoint.py Recv_SetOwnerId(ownerId), line 60. The client stores the id, and if
    /// the point already shows a state package it swaps to the one for the new owner
    /// (OwnerIdToStatePkgId: -1 none, 0 other clan, 1 Bane, 2 AFS; see ControlPointOwner).
    /// </summary>
    public class SetOwnerIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetOwnerId;

        public int OwnerId { get; }

        public SetOwnerIdPacket(int ownerId)
        {
            OwnerId = ownerId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(OwnerId);
        }
    }
}
