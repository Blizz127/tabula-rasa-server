using System.Collections.Generic;

namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_DisplayWargameMessage(msgId, args = {}), opcode 604, on entity 23.
    /// It hands straight to _DisplayWargameMessage, which posts UI_DISPLAY_PLAYER_MESSAGE with
    /// the SYSTEM_GENERAL filter - one chat line, built from the client's own
    /// playermessagelanguage table, with the %(...)s placeholders filled from args.
    ///
    /// This is the whole of the wargame's text: every refusal, every "you accepted", every
    /// "%(target)s is already in a Wargame" comes down this one channel, which is why the
    /// unimplemented opcode 626 left the player with no feedback of any kind.
    ///
    /// args has a default, but it is always written: nearly every wargame message interpolates a
    /// name, and a missing dictionary would leave the raw %(player)s on screen.
    /// </summary>
    public class DisplayWargameMessagePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DisplayWargameMessage;

        public PlayerMessage MsgId { get; set; }
        public Dictionary<string, string> Args { get; set; }

        public DisplayWargameMessagePacket(PlayerMessage msgId, Dictionary<string, string> args = null)
        {
            MsgId = msgId;
            Args = args ?? new Dictionary<string, string>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)MsgId);
            pw.WriteDictionary(Args.Count);

            foreach (var arg in Args)
            {
                pw.WriteString(arg.Key);
                pw.WriteString(arg.Value);
            }
        }
    }
}
