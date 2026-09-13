using System.IO;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestSwitchToCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestSwitchToCharacterInSlot;

        public byte SlotNum { get; set; }
        public bool SkipBootcamp { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 2)
                throw new InvalidDataException("Character selection requires two fields.");
            SlotNum = checked((byte)pr.ReadInt());
            SkipBootcamp = pr.ReadBool();
        }
    }
}
