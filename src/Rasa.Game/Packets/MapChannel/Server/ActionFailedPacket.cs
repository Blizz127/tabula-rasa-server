namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionFailed;
        public ActionId ActionId { get; }
        public uint ActionArgId { get; }

        public ActionFailedPacket(ActionId actionId, uint actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            // Recv_ActionFailed cancels the matching current action.
            pw.WriteTuple(2);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
        }
    }
}
