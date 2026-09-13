namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// manifestation.Recv_ForceConverse(convoDataDict, npcNameId): opens the conversation window with the
    /// GREETING entry of the dictionary, titled with creaturenamelanguage[npcNameId]. There is no NPC entity.
    /// </summary>
    public class ForceConversePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ForceConverse;

        public uint GreetingId { get; }
        public uint NpcNameId { get; }

        public ForceConversePacket(uint greetingId, uint npcNameId)
        {
            GreetingId = greetingId;
            NpcNameId = npcNameId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteDictionary(1);
            pw.WriteInt((int)ConversationType.Greeting);
            pw.WriteUInt(GreetingId);
            pw.WriteUInt(NpcNameId);
        }
    }
}
