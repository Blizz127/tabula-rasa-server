namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestActionInterruptPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestActionInterrupt;

        public ActionId ActionId { get; set; }
        public uint ActionArgId { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 2)
                throw new InvalidClientMessageException();
            ActionId = (ActionId)pr.ReadInt();
            ActionArgId = pr.ReadUInt();
        }
    }
}
