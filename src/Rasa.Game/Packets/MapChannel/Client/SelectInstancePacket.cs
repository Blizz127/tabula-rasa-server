namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.OnGotoInstance(mapId, startGroup) (line 495, method id 687): SendCallActorMethod('SelectInstance',
    /// (mapId, startGroup)) - "Tell the server which instance they've chosen to go to". mapId is the instanceId of the
    /// chosen ChooseInstanceList entry; startGroup is whatever that entry carried (None or a start-group name).
    /// </summary>
    public class SelectInstancePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectInstance;

        public uint MapInstanceId { get; set; }
        public string StartGroup { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            MapInstanceId = pr.ReadUInt();

            switch (pr.PeekType())
            {
                case PythonType.String:
                    StartGroup = pr.ReadString();
                    break;
                case PythonType.UnicodeString:
                    StartGroup = pr.ReadUnicodeString();
                    break;
                default:
                    pr.ReadUnkStruct();
                    break;
            }
        }
    }
}
