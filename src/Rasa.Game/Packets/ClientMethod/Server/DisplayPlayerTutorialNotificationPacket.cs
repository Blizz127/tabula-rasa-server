namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// clientmethod.Recv_DisplayPlayerTutorialNotification(tutorialId), sent to SysEntity.ClientMethodId.
    /// </summary>
    public class DisplayPlayerTutorialNotificationPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayPlayerTutorialNotification;

        public uint TutorialId { get; }

        public DisplayPlayerTutorialNotificationPacket(uint tutorialId)
        {
            TutorialId = tutorialId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(TutorialId);
        }
    }
}
