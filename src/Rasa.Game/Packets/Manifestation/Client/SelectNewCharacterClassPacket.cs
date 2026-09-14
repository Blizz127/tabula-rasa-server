namespace Rasa.Packets.Manifestation.Client
{
    using Data;
    using Memory;

    /// <summary>gameui.OnChooseTierAdvance: SendCallActorMethod('SelectNewCharacterClass', (chosenClassId,)).</summary>
    public class SelectNewCharacterClassPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SelectNewCharacterClass;

        public uint ClassId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ClassId = pr.ReadUInt();
        }
    }
}
