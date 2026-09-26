namespace Rasa.Packets.Communicator.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// The admin broadcast: communicator.Recv_AdminMessage(msg, filterId), method id 24.
    ///
    /// 1.16.5.0 client, trpython.zip:client/communicator.pyo (sha256 8dbfe228…fab9), source line 1046:
    /// it runs msg through the profanity filter, builds uielementlanguage ID_CHAT_MESSAGE_HEADER_GM (4146,
    /// English "ADMIN MESSAGE: ", German "ADMIN-NACHRICHT: ") and posts header + msg as a chat line under
    /// filterId (offsets 0-67). So the prefix is the client's own text and only the payload is ours; the
    /// shutdown footage shows the same English payload behind both headers. The payload is a unicode string
    /// because the handler concatenates it with the unicode header.
    ///
    /// filterId is the server's to choose. SYSTEM_GM (10000046) is the one the client gives Chat_Yellow by
    /// default (clientmessagesettings.FillInDefaultColors, line 185) and subscribes the default General tab
    /// to (CreateDefaultTabs, line 100): the footage's admin lines are yellow in the General tab, while
    /// SYSTEM_GENERAL lines such as "You may not issue a command at this time." are white.
    /// Provenance: docs/evidence/shutdown-broadcast.json.
    /// </summary>
    public class AdminMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AdminMessage;

        public string Message { get; set; }
        public uint FilterId { get; set; }

        public AdminMessagePacket(string msg, MsgFilterId filterId = MsgFilterId.GameMaster)
            : this(msg, (uint)filterId)
        {
        }

        public AdminMessagePacket(string msg, uint filterId)
        {
            Message = msg;
            FilterId = filterId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUnicodeString(Message);
            pw.WriteUInt(FilterId);
        }
    }
}
