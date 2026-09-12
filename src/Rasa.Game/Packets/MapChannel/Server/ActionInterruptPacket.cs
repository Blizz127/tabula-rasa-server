namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ActionInterruptPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ActionInterrupt;
        public ulong SourceId { get; }
        public ActionId ActionId { get; }
        public uint ActionArgId { get; }

        public ActionInterruptPacket(ulong sourceId, ActionId actionId, uint actionArgId)
        {
            SourceId = sourceId;
            ActionId = actionId;
            ActionArgId = actionArgId;
        }

        public override void Write(PythonWriter pw)
        {
            // Recv_ActionInterrupt(sourceId, actionId, actionArgId).
            pw.WriteTuple(3);
            pw.WriteULong(SourceId);
            pw.WriteUInt((uint)ActionId);
            pw.WriteUInt(ActionArgId);
        }
    }
}
