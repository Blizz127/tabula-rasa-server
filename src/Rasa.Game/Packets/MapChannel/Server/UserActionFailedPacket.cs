namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class UserActionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UserActionFailed;
        public ActionId ActionId { get; }
        public int ActionArgId { get; }

        public UserActionFailedPacket(ActionId actionId, int actionArgId)
        {
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            // The original client accepts None for the optional localized message.
            // It still resolves the matching pending action; see the evidence doc.
            pw.WriteTuple(3);
            pw.WriteInt((int)ActionId);
            pw.WriteInt(ActionArgId);
            pw.WriteNoneStruct();
        }
    }
}
