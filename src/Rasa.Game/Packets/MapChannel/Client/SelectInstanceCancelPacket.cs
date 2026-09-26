namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.OnGotoInstanceCancel() (line 502, method id 688): "Tell the server we no longer want to zone out".
    /// Sent when the instance window is closed in WINDOW_MODE_INSTANCE (waypointwindow.Hide line 523).
    /// </summary>
    public class SelectInstanceCancelPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectInstanceCancel;

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
        }
    }
}
