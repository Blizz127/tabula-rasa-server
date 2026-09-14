namespace Rasa.Packets.Manifestation.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// manifestation.Recv_CloneCredits(cloneCredits): the first value after login is stored silently; a later
    /// increase posts PM 955 and the cloning tutorial.
    /// </summary>
    public class CloneCreditsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CloneCredits;

        public uint CloneCredits { get; }

        public CloneCreditsPacket(uint cloneCredits)
        {
            CloneCredits = cloneCredits;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(CloneCredits);
        }
    }
}
