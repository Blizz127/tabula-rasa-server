using System.IO;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class RequestDeleteCharacterInSlotPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.RequestDeleteCharacterInSlot;

        public byte Slot { get; set; }

        public override void Read(PythonReader pr)
        {
            if (pr.ReadTuple() != 1)
                throw new InvalidDataException("Character deletion requires one slot field.");
            Slot = checked((byte)pr.ReadUInt());
        }
    }
}
