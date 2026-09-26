namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    public class RequestTooltipForModuleIdPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestTooltipForModuleId;

        public int ModuleId { get; set; }

        public override void Read(PythonReader pr)
        {
            // Original gameui.OnRequestTooltipForModuleId sends (moduleId,).
            if (pr.ReadTuple() != 1 || pr.PeekType() != PythonType.Int)
                throw new InvalidClientMessageException();
            ModuleId = pr.ReadInt();
        }
    }
}
