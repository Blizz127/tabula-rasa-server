namespace Rasa.Packets.Party.Server
{
    using Data;
    using Memory;

    public class RemovePartyMemberPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RemovePartyMember;

        internal uint UserId { get; set; }
        internal bool WasKicked { get; set; }

        internal RemovePartyMemberPacket(uint userId, bool wasKicked = false)
        {
            UserId = userId;
            WasKicked = wasKicked;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            // The same encoding as the id the client got in the member tuple (PartyMember.Write, WriteUInt). As a long,
            // an id above int.MaxValue (the DIT bots') never matched the client's signed int and the row stayed.
            pw.WriteUInt(UserId);
            pw.WriteBool(WasKicked);
        }
    }
}
