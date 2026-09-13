namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;
    using Protocol;

    /// <summary>
    /// Radio and shared mission requests from client/missionlog.pyo. The server
    /// has no radio or shared mission support yet, so the argument tuple is only
    /// consumed; without a packet type the message would fail to decode and the
    /// client would be disconnected.
    /// </summary>
    public abstract class UnsupportedMissionRequestPacket : ClientPythonPacket
    {
        public override void Read(PythonReader pr)
        {
            SkipValue(pr);
        }

        private static void SkipValue(PythonReader pr)
        {
            switch (pr.PeekType())
            {
                case PythonType.Structs:
                    pr.ReadUnkStruct();
                    break;
                case PythonType.Int:
                    pr.ReadInt();
                    break;
                case PythonType.Long:
                    pr.ReadLong();
                    break;
                case PythonType.Double:
                    pr.ReadDouble();
                    break;
                case PythonType.String:
                    pr.ReadString();
                    break;
                case PythonType.UnicodeString:
                    pr.ReadUnicodeString();
                    break;
                case PythonType.Dictionary:
                    var entries = pr.ReadDictionary();
                    for (var i = 0; i < entries * 2; i++)
                        SkipValue(pr);
                    break;
                case PythonType.List:
                    var items = pr.ReadList();
                    for (var i = 0; i < items; i++)
                        SkipValue(pr);
                    break;
                case PythonType.Tuple:
                    var fields = pr.ReadTuple();
                    for (var i = 0; i < fields; i++)
                        SkipValue(pr);
                    break;
                default:
                    // Handled by the client reader as a malformed message.
                    throw new InvalidClientMessageException();
            }
        }
    }

    // (playerId, missionId)
    public class AssignSharedMissionPacket : UnsupportedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignSharedMission;
    }

    // (missionId, selectionIdx, rating)
    public class CompleteRadioMissionPacket : UnsupportedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteRadioMission;
    }

    // (playerId, missionId)
    public class DeclineSharedMissionPacket : UnsupportedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DeclineSharedMission;
    }

    // (missionId, selectionIdx, rating)
    public class RewardRadioMissionPacket : UnsupportedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RewardRadioMission;
    }

    // (missionId,)
    public class ShareMissionPacket : UnsupportedMissionRequestPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ShareMission;
    }
}
